using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SkillsSwap.Data;
using SkillsSwap.Models;
using SkillsSwap.Security;
using SkillsSwap.Services;

namespace SkillsSwap.Controllers;

[Authorize(Policy = SecurityPolicies.CompleteProfile)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class LearningChatController(ApplicationDbContext db, PeerSessionService sessions,
    IAuthorizationService authorization, TimeProvider clock) : Controller
{
    private const int PageSize = 40;
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private IQueryable<LearningRequest> Conversations =>
        db.LearningRequests.AsNoTracking().Where(r => !r.IsExpertBooking && r.Status == RequestStatus.Accepted &&
            (r.TeacherId == Actor || r.LearnerId == Actor));

    [HttpGet("/chat.html")]
    public async Task<IActionResult> Index(long? requestId, CancellationToken ct)
    {
        var selected = requestId.HasValue ? await FindAsync(requestId.Value, ct) : null;
        if (requestId.HasValue && selected is null) return NotFound();
        var rows = await Conversations.Select(r => new
            {
                r.Id,
                Skill = r.Skill.NameEn,
                Partner = Actor == r.TeacherId ? r.Learner.FullName : r.Teacher.FullName,
                ProfileImage = Actor == r.TeacherId ? r.Learner.ProfileImageUrl : r.Teacher.ProfileImageUrl,
                LastBody = db.Messages.Where(m => m.LearningRequestId == r.Id)
                    .OrderByDescending(m => m.SentAtUtc).ThenByDescending(m => m.Id)
                    .Select(m => m.Body).FirstOrDefault(),
                LastAt = db.Messages.Where(m => m.LearningRequestId == r.Id).Max(m => (DateTime?)m.SentAtUtc),
                Unread = db.Messages.Count(m => m.LearningRequestId == r.Id && m.SenderId != Actor && m.ReadAtUtc == null)
            })
            .OrderByDescending(r => r.LastAt).ThenByDescending(r => r.Id)
            .Select(r => new ChatConversation(r.Id, r.Skill, r.Partner, r.ProfileImage, r.LastBody, r.LastAt, r.Unread))
            .ToListAsync(ct);
        var canSend = selected is { EndedAtUtc: null } && await sessions.EligibleParticipants(selected).CountAsync(ct) == 2;
        var conversations = rows.Select(row => row with
        {
            ProfileImage = SkillDiscoveryService.SafePublicImageUrl(row.ProfileImage)
        }).ToList();
        return View("~/Views/Sessions/Chat.cshtml", new LearningChatPage(conversations, requestId, canSend));
    }

    [HttpGet("/chat/{id:long}/messages")]
    public async Task<IActionResult> Messages(long id, long? after, long? before, CancellationToken ct)
    {
        var request = await FindAsync(id, ct);
        if (request is null) return NotFound();
        if (after.HasValue && before.HasValue) return BadRequest();
        var messages = db.Messages.AsNoTracking().Where(m => m.LearningRequestId == id);
        if ((after ?? before) is long anchor)
        {
            var pivot = await messages.Where(m => m.Id == anchor).Select(m => new { m.SentAtUtc }).SingleOrDefaultAsync(ct);
            if (pivot is null) return BadRequest();
            messages = after.HasValue
                ? messages.Where(m => m.SentAtUtc > pivot.SentAtUtc || m.SentAtUtc == pivot.SentAtUtc && m.Id > anchor)
                : messages.Where(m => m.SentAtUtc < pivot.SentAtUtc || m.SentAtUtc == pivot.SentAtUtc && m.Id < anchor);
        }
        var rows = await (after.HasValue ? messages.OrderBy(m => m.SentAtUtc).ThenBy(m => m.Id)
                : messages.OrderByDescending(m => m.SentAtUtc).ThenByDescending(m => m.Id))
            .Take(PageSize + 1).Select(m => new ChatMessage(m.Id, m.Body, m.SentAtUtc, m.SenderId == Actor,
                m.SenderId != Actor && m.ReadAtUtc == null)).ToListAsync(ct);
        var more = rows.Count > PageSize;
        if (more) rows.RemoveAt(PageSize);
        if (!after.HasValue) rows.Reverse();
        var canSend = request.EndedAtUtc is null && await sessions.EligibleParticipants(request).CountAsync(ct) == 2;
        return Json(new { messages = rows, more, canSend, archived = request.EndedAtUtc.HasValue });
    }

    [HttpPost("/chat/{id:long}/messages"), ValidateAntiForgeryToken, EnableRateLimiting("learning-chat-send")]
    public async Task<IActionResult> Send(long id, string? body, CancellationToken ct)
    {
        if (await FindAsync(id, ct) is null) return NotFound();
        if (!ValidBody(body)) return BadRequest("Enter a message of 1 to 4000 characters.");
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (!await sessions.LockAsync($"SkillSwap:Request:{id}", ct)) return StatusCode(503);
        var request = await FindAsync(id, ct);
        if (request is null) return NotFound();
        if (request.EndedAtUtc.HasValue || await sessions.EligibleParticipants(request).CountAsync(ct) != 2)
            return Conflict("This conversation is read-only.");
        var last = await db.Messages.Where(m => m.LearningRequestId == id).MaxAsync(m => (DateTime?)m.SentAtUtc, ct);
        var now = clock.GetUtcNow().UtcDateTime;
        var message = new Message { LearningRequestId = id, SenderId = Actor, Body = body!.Trim(),
            SentAtUtc = last.HasValue && last.Value > now ? DateTime.SpecifyKind(last.Value, DateTimeKind.Utc) : now };
        db.Messages.Add(message);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Json(new ChatMessage(message.Id, message.Body, message.SentAtUtc, true, false));
    }

    [HttpPost("/chat/{id:long}/read"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Read(long id, [FromForm] long[] ids, CancellationToken ct)
    {
        if (await FindAsync(id, ct) is null) return NotFound();
        if (ids.Length is < 1 or > PageSize) return BadRequest();
        await db.Messages.Where(m => m.LearningRequestId == id && m.SenderId != Actor &&
                m.ReadAtUtc == null && ids.Contains(m.Id))
            .ExecuteUpdateAsync(set => set.SetProperty(m => m.ReadAtUtc, clock.GetUtcNow().UtcDateTime), ct);
        return NoContent();
    }

    [HttpGet("/chat/unread")]
    public async Task<IActionResult> Unread(CancellationToken ct) => Json(await db.Messages.CountAsync(m =>
        !m.LearningRequest.IsExpertBooking && m.LearningRequest.Status == RequestStatus.Accepted &&
        (m.LearningRequest.LearnerId == Actor || m.LearningRequest.TeacherId == Actor) &&
        m.SenderId != Actor && m.ReadAtUtc == null, ct));

    public static bool ValidBody(string? body) => body is { Length: <= 4000 } && !string.IsNullOrWhiteSpace(body);

    private async Task<LearningRequest?> FindAsync(long id, CancellationToken ct)
    {
        var request = await Conversations.SingleOrDefaultAsync(r => r.Id == id, ct);
        return request is not null && (await authorization.AuthorizeAsync(User, request, RequestOperation.View)).Succeeded &&
            await sessions.EligibleParticipants(request).ContainsAsync(Actor, ct) ? request : null;
    }
}
