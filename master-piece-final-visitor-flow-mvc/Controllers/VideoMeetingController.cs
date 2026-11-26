using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SkillsSwap.Data;
using SkillsSwap.Models;
using SkillsSwap.Security;
using SkillsSwap.Services;

namespace SkillsSwap.Controllers;

[Authorize(Policy = SecurityPolicies.CompleteProfile)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class VideoMeetingController(ApplicationDbContext db, PeerSessionService sessions,
    IAuthorizationService authorization, IOptions<VideoMeetingOptions> options, TimeProvider clock) : Controller
{
    [HttpPost("/meetings/{id:long}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Open(long id, CancellationToken ct)
    {
        var requestId = await db.LearningSessions.Where(s => s.Id == id)
            .Select(s => (long?)s.LearningRequestId).SingleOrDefaultAsync(ct);
        if (requestId is null) return NotFound();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (!await sessions.LockAsync($"SkillSwap:Request:{requestId}", ct)) return StatusCode(503);
        var session = await FindAsync(id, ct);
        if (session is null) return NotFound();
        if (!CanOpen(session)) return RedirectToAction("SessionDetails", "Sessions", new { id });

        if (session.MeetingProviderId is null)
        {
            session.MeetingProviderId = "skillswap-jitsi-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
            await db.SaveChangesAsync(ct);
        }
        await transaction.CommitAsync(ct);
        return RedirectToAction(nameof(Index), new { id });
    }

    [HttpGet("/meetings/{id:long}")]
    public async Task<IActionResult> Index(long id, CancellationToken ct)
    {
        var session = await FindAsync(id, ct);
        if (session is null) return NotFound();
        var request = session.LearningRequest;
        var name = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var displayName = Guid.TryParse(name, out var actor) && request.TeacherId == actor
            ? request.Teacher.FullName : request.Learner.FullName;
        var availability = Availability(session);
        return View("~/Views/Sessions/ZoomMeeting.cshtml", new VideoMeetingPage(session.Id, session.Title ?? request.Skill.NameEn, request.Skill.NameEn,
            request.Teacher.FullName, request.Learner.FullName, session.StartsAt, session.DurationMinutes,
            displayName, CanOpen(session) ? session.MeetingProviderId : null, options.Value.Domain, availability));
    }

    private async Task<LearningSession?> FindAsync(long id, CancellationToken ct)
    {
        var session = await db.LearningSessions.Include(s => s.LearningRequest).ThenInclude(r => r.Teacher)
            .Include(s => s.LearningRequest).ThenInclude(r => r.Learner)
            .Include(s => s.LearningRequest).ThenInclude(r => r.Skill)
            .SingleOrDefaultAsync(s => s.Id == id && s.LearningRequest.Status == RequestStatus.Accepted, ct);
        if (session is null || !(await authorization.AuthorizeAsync(User, session.LearningRequest, RequestOperation.View)).Succeeded)
            return null;
        var actor = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var eligible = await sessions.EligibleParticipants(session.LearningRequest).ContainsAsync(actor, ct);
        return eligible ? session : null;
    }

    private bool CanOpen(LearningSession session) => Availability(session) == "Meeting is available.";

    private string Availability(LearningSession session) =>
        options.Value.Availability(session.Status, session.LearningRequest.EndedAtUtc is null,
            session.StartsAt, session.DurationMinutes, clock.GetUtcNow(), session.MeetingProviderId, session.MeetingJoinUrl);
}
