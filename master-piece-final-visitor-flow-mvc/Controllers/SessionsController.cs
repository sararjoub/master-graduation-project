using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Security.Claims;
using SkillsSwap.Data;
using SkillsSwap.Models;
using SkillsSwap.Security;
using SkillsSwap.Services;

namespace SkillsSwap.Controllers;

[Authorize(Policy = SecurityPolicies.CompleteProfile)]
public class SessionsController(ApplicationDbContext db, IAuthorizationService authorization,
    PeerSessionService service, TimeProvider clock, ILogger<SessionsController> logger) : Controller
{
    private Guid Actor => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    private IQueryable<LearningRequest> Relationships => db.LearningRequests.AsNoTracking().Where(r =>
        !r.IsExpertBooking && r.Status == RequestStatus.Accepted && (r.LearnerId == Actor || r.TeacherId == Actor));

    private IQueryable<LearningSession> VisibleSessions => db.LearningSessions.AsNoTracking()
        .Where(s => Relationships.Any(r => r.Id == s.LearningRequestId));

    private IQueryable<SessionRow> Project(IQueryable<LearningSession> sessions) => sessions.Select(s => new SessionRow(
        s.Id, s.LearningRequestId, s.Title, s.LearningRequest.Skill.NameEn,
        s.LearningRequest.LearnerId == Actor ? s.LearningRequest.Teacher.FullName : s.LearningRequest.Learner.FullName,
        s.LearningObjectives, s.Notes, s.StartsAt, s.DurationMinutes, s.Status, s.CancellationReason,
        s.ScheduleProposals.OrderByDescending(p => p.Revision).Take(20)
            .Select(p => new ScheduleProposalRow(p.Id, p.Revision, p.StartsAtUtc, p.DurationMinutes,
                p.Status, p.ProposedByUserId == Actor)).ToList(),
        s.RowVersion, s.LearningRequest.TeacherId == Actor, s.LearningRequest.EndedAtUtc == null,
        s.LearnerCompletedAtUtc, s.TeacherCompletedAtUtc, s.CompletedAtUtc,
        db.SessionAbsenceRequests.Where(a => a.LearningSessionId == s.Id).OrderByDescending(a => a.Id).Take(20)
            .Select(a => new AbsenceRow(a.Id, a.Reason, a.CreatedAtUtc, a.RequestedForStartsAtUtc, a.Status)).ToList(),
        db.SessionScheduleHistory.Where(h => h.LearningSessionId == s.Id).OrderByDescending(h => h.ChangedAtUtc).ThenByDescending(h => h.Id).Take(20)
            .Select(h => new ScheduleHistoryRow(h.PreviousStartsAtUtc, h.NewStartsAtUtc, h.PreviousDurationMinutes,
                h.NewDurationMinutes, h.ChangedAtUtc)).ToList(),
        db.Reviews.Where(r => r.LearningSessionId == s.Id && r.ReviewerId == s.LearningRequest.LearnerId &&
                r.ReviewedUserId == s.LearningRequest.TeacherId)
            .Select(r => new SessionReviewRow(r.Rating, r.Comment, r.CreatedAtUtc)).FirstOrDefault()));

    [HttpGet("/chat.html")]
    public IActionResult Chat() => View();

    [HttpGet("/expert-booking.html")]
    public IActionResult ExpertBooking() => View();

    [HttpGet("/expert-workspace.html")]
    public IActionResult ExpertWorkspace() => View();

    [HttpGet("/session-details.html")]
    public async Task<IActionResult> SessionDetails(long id, CancellationToken ct)
    {
        var row = await Project(VisibleSessions.Where(s => s.Id == id)).SingleOrDefaultAsync(ct);
        if (row is null) return NotFound();
        var relationship = await AuthorizedRelationshipAsync(row.RequestId, ct);
        return relationship is null ? NotFound() :
            View(new PeerSessionPage(row.RequestId, relationship.EndedAtUtc == null, 1, false, [row], clock.GetUtcNow()));
    }

    [HttpGet("/session-review.html")]
    public async Task<IActionResult> SessionReview(long id, CancellationToken ct)
    {
        var row = await Project(VisibleSessions.Where(s => s.Id == id)).SingleOrDefaultAsync(ct);
        if (row is null || await AuthorizedRelationshipAsync(row.RequestId, ct) is null) return NotFound();
        if (row.Review is null && (row.IsTeacher || row.Status != SessionStatus.Completed ||
            row.CompletedAt is null || row.LearnerConfirmed is null || row.TeacherConfirmed is null))
            return NotFound();
        return View(new PeerSessionPage(row.RequestId, false, 1, false, [row], clock.GetUtcNow()));
    }

    [HttpGet("/sessions.html")]
    public async Task<IActionResult> Sessions(long? requestId, int page = 1, CancellationToken ct = default)
    {
        var relationship = requestId.HasValue ? await AuthorizedRelationshipAsync(requestId.Value, ct) : null;
        if (requestId.HasValue && relationship is null) return NotFound();
        page = Math.Clamp(page, 1, 10000);
        var query = VisibleSessions.Where(s => !requestId.HasValue || s.LearningRequestId == requestId.Value)
            .OrderByDescending(s => s.StartsAt).ThenByDescending(s => s.Id).Skip((page - 1) * 20).Take(21);
        var rows = await Project(query).ToListAsync(ct);
        var requests = relationship is null ? [] : await db.LearningSessionRequests.AsNoTracking()
            .Where(r => r.LearningRequestId == relationship.Id)
            .OrderByDescending(r => r.Status == LearningSessionRequestStatus.Pending).ThenByDescending(r => r.Id).Take(20)
            .Select(r => new SessionRequestRow(r.Id, r.Status, r.CreatedAtUtc, r.FulfilledSessionId)).ToListAsync(ct);
        return View(new PeerSessionPage(requestId, relationship is { EndedAtUtc: null } && relationship.TeacherId == Actor,
            page, rows.Count > 20, rows.Take(20).ToList(), clock.GetUtcNow())
        {
            RelationshipActive = relationship is { EndedAtUtc: null },
            IsTeacher = relationship?.TeacherId == Actor,
            Requests = requests
        });
    }

    [HttpPost("/sessions/change"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Change(long requestId, long? sessionId, long? itemId,
        string action, SessionScheduleInput input, CancellationToken ct)
    {
        if (!PeerSessionService.IsAction(action))
            return BadRequest();
        if (await AuthorizedRelationshipAsync(requestId, ct) is null) return NotFound();
        if (action is "reschedule" or "cancel" or "absence" or "withdraw-absence" or "complete" or "review" &&
            !await VisibleSessions.AnyAsync(s => s.Id == sessionId && s.LearningRequestId == requestId, ct))
            return NotFound();
        string? error;
        try
        {
            error = ModelState.IsValid
                ? await service.ChangeAsync(requestId, sessionId, itemId, Actor, action, input, ct)
                : "Enter valid session details.";
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            logger.LogWarning("Peer session operation {Action} failed with {ExceptionType}.", action, ex.GetType().Name);
            TempData["PeerSessionFeedback"] = "The change could not be saved. Check the current status and try again.";
            return sessionId.HasValue ? RedirectToAction(nameof(SessionDetails), new { id = sessionId })
                : RedirectToAction(nameof(Sessions), new { requestId });
        }
        if (error is not null)
        {
            var result = action == "review" && sessionId.HasValue ? await SessionReview(sessionId.Value, ct)
                : sessionId.HasValue ? await SessionDetails(sessionId.Value, ct) : await Sessions(requestId, 1, ct);
            if (result is ViewResult view && view.Model is PeerSessionPage model)
            {
                view.ViewName = action == "review" ? nameof(SessionReview) :
                    sessionId.HasValue ? nameof(SessionDetails) : nameof(Sessions);
                view.ViewData.Model = model with { FormError = error, PreviousInput = input,
                    PreviousAction = action, PreviousItemId = itemId };
                return view;
            }
            TempData["PeerSessionFeedback"] = error;
        }
        else TempData["PeerSessionFeedback"] = "Session changes saved.";
        return sessionId.HasValue ? RedirectToAction(nameof(SessionDetails), new { id = sessionId })
            : RedirectToAction(nameof(Sessions), new { requestId });
    }

    private static bool IsDatabaseFailure(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
            if (current is DbException or DbUpdateException) return true;
        return false;
    }

    private async Task<LearningRequest?> AuthorizedRelationshipAsync(long requestId, CancellationToken ct)
    {
        var request = await Relationships.SingleOrDefaultAsync(r => r.Id == requestId, ct);
        return request is not null && (await authorization.AuthorizeAsync(User, request, RequestOperation.View)).Succeeded
            ? request : null;
    }

    [HttpGet("/zoom-meeting.html")]
    public IActionResult ZoomMeeting() => View();
}
