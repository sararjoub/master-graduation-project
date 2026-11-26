using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SkillsSwap.Data;
using SkillsSwap.Models;

namespace SkillsSwap.Services;

public sealed class PeerSessionService(ApplicationDbContext db, TimeProvider clock)
{
    public static bool Participant(LearningRequest request, Guid actor) =>
        actor != Guid.Empty && (request.LearnerId == actor || request.TeacherId == actor);

    public static bool TrySchedule(string? local, string? zone, int duration, DateTimeOffset now, out DateTimeOffset utc)
    {
        utc = default;
        if (duration is not (30 or 45 or 60 or 90) || string.IsNullOrWhiteSpace(zone) ||
            !DateTime.TryParseExact(local, ["yyyy-MM-ddTHH:mm", "yyyy-MM-ddTHH:mm:ss"],
                System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var date))
            return false;
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(zone);
            if (tz.IsAmbiguousTime(date) || tz.IsInvalidTime(date)) return false;
            utc = TimeZoneInfo.ConvertTimeToUtc(date, tz);
            return utc > now && utc <= DateTimeOffset.MaxValue.AddMinutes(-duration);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException or ArgumentException)
        {
            return false;
        }
    }

    public static bool IsAction(string action) => action is
        "request" or "withdraw-request" or "schedule" or "reschedule" or "cancel" or
        "absence" or "withdraw-absence" or "review" or "end";

    public async Task<string?> ChangeAsync(long requestId, long? sessionId, long? itemId, Guid actor,
        string action, SessionScheduleInput input, CancellationToken ct)
    {
        if (!IsAction(action)) return "This action is unavailable.";
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            if (!await LockAsync($"SkillSwap:Request:{requestId}", ct)) return "Scheduling is busy. Try again.";
            var request = await db.LearningRequests.SingleOrDefaultAsync(r =>
                r.Id == requestId && !r.IsExpertBooking && r.Status == RequestStatus.Accepted, ct);
            if (request is null || !Participant(request, actor)) return "The relationship is unavailable.";
            var teacher = request.TeacherId == actor;
            if ((action is "schedule" or "reschedule" or "cancel") && !teacher ||
                (action is "request" or "withdraw-request" or "absence" or "withdraw-absence" or "review") && teacher)
                return "This action is unavailable for your role in this relationship.";
            if (request.EndedAtUtc.HasValue && action != "review") return "This relationship has ended.";

            if (action is "schedule" or "reschedule" or "cancel")
                if (!await LockParticipantsAsync(request, ct)) return "Scheduling is busy. Try again.";

            var now = clock.GetUtcNow();
            var eligible = await EligibleParticipants(request).ToListAsync(ct);
            if (!eligible.Contains(actor) || (eligible.Count != 2 && action is "request" or "schedule" or "reschedule"))
                return "The required participant accounts are unavailable.";

            var session = sessionId is long id ? await db.LearningSessions
                .SingleOrDefaultAsync(s => s.Id == id && s.LearningRequestId == requestId, ct) : null;
            if (action is "reschedule" or "cancel" or "absence" or "withdraw-absence" or "review")
            {
                if (session is null) return "The session is unavailable.";
                if (action is "reschedule" or "cancel" &&
                    (session.RowVersion is null || input.Version != Convert.ToBase64String(session.RowVersion)))
                    return "The session changed. Reload its details before trying again.";
            }

            var error = action switch
            {
                "request" or "withdraw-request" => await ChangeRequestAsync(request, actor, action, itemId, now, ct),
                "schedule" or "reschedule" => await ScheduleAsync(request, session, actor, action, itemId, input, now, ct),
                "end" => await EndAsync(request, actor, now, ct),
                _ => await ChangeSessionAsync(request, session!, actor, action, itemId, input, now, ct)
            };
            if (error is not null) return error;

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return null;
        }
        catch (Exception ex) when (ex is DbUpdateConcurrencyException || PeerRequestService.HasSqlError(ex, 1205, 2601, 2627))
        {
            return "The change could not be confirmed. Reload before trying again.";
        }
    }

    private async Task<string?> ChangeRequestAsync(LearningRequest request, Guid actor, string action,
        long? itemId, DateTimeOffset now, CancellationToken ct)
    {
        var pending = await PendingRequestAsync(request.Id, ct);
        if (action == "request")
        {
            if (pending is not null) return "A session request is already awaiting scheduling.";
            db.LearningSessionRequests.Add(new LearningSessionRequest
            {
                LearningRequestId = request.Id, RequestedByUserId = actor, CreatedAtUtc = now
            });
        }
        else
        {
            if (pending is null || pending.Id != itemId || pending.RequestedByUserId != actor)
                return "This request is no longer pending.";
            Resolve(pending, LearningSessionRequestStatus.Withdrawn, actor, now);
        }
        return null;
    }

    private async Task<string?> ScheduleAsync(LearningRequest request, LearningSession? session, Guid actor,
        string action, long? itemId, SessionScheduleInput input, DateTimeOffset now, CancellationToken ct)
    {
        if (!TrySchedule(input.LocalStart, input.TimeZoneId, input.DurationMinutes, now, out var start))
            return "Enter a valid future start, time zone and duration.";
        if (action == "reschedule" && (session!.Status != SessionStatus.Scheduled || session.StartsAt <= now))
            return "Only a future scheduled session can be rescheduled.";
        if (action == "reschedule" && session!.StartsAt == start && session.DurationMinutes == input.DurationMinutes)
            return "Choose a different time or duration.";

        LearningSessionRequest? pending = null;
        if (action == "schedule" && itemId.HasValue)
        {
            pending = await PendingRequestAsync(request.Id, ct);
            if (pending is null || pending.Id != itemId || pending.RequestedByUserId != request.LearnerId)
                return "This session request is no longer pending.";
        }

        var excluded = action == "reschedule" ? session!.Id : 0;
        var end = start.AddMinutes(input.DurationMinutes);
        if (await db.LearningSessions.AnyAsync(s => s.Id != excluded &&
            s.Status == SessionStatus.Scheduled && s.StartsAt < end && start < s.StartsAt.AddMinutes(s.DurationMinutes) &&
            (s.LearningRequest.LearnerId == request.LearnerId || s.LearningRequest.TeacherId == request.LearnerId ||
             s.LearningRequest.LearnerId == request.TeacherId || s.LearningRequest.TeacherId == request.TeacherId), ct))
            return "That time overlaps a scheduled session.";

        if (action == "schedule")
        {
            var title = input.Title?.Trim();
            var objectives = input.Objectives?.Trim();
            var notes = input.Notes?.Trim();
            if (string.IsNullOrWhiteSpace(title) || title.Length > 120 ||
                string.IsNullOrWhiteSpace(objectives) || objectives.Length > 1500 || notes?.Length > 1500)
                return "Enter a valid title, learning objectives and notes.";
            session = new LearningSession
            {
                LearningRequestId = request.Id, Title = title, LearningObjectives = objectives, Notes = notes,
                CreatedAtUtc = now, StartsAt = start, DurationMinutes = input.DurationMinutes, Status = SessionStatus.Scheduled
            };
            db.LearningSessions.Add(session);
            if (pending is not null)
            {
                // Materialize the key within the same transaction; fulfillment commits with the session.
                await db.SaveChangesAsync(ct);
                pending.FulfilledSessionId = session.Id;
                Resolve(pending, LearningSessionRequestStatus.Fulfilled, actor, now);
            }
        }
        else
        {
            db.SessionScheduleHistory.Add(new SessionScheduleHistory
            {
                LearningSessionId = session!.Id, ChangedByUserId = actor, ChangedAtUtc = now,
                PreviousStartsAtUtc = session.StartsAt, NewStartsAtUtc = start,
                PreviousDurationMinutes = session.DurationMinutes, NewDurationMinutes = input.DurationMinutes
            });
            if (session.StartsAt != start)
                await ResolveAbsenceAsync(session.Id, SessionAbsenceRequestStatus.ResolvedByReschedule, actor, now, ct);
            session.StartsAt = start;
            session.DurationMinutes = input.DurationMinutes;
        }
        return null;
    }

    private async Task<string?> ChangeSessionAsync(LearningRequest request, LearningSession session, Guid actor,
        string action, long? itemId, SessionScheduleInput input, DateTimeOffset now, CancellationToken ct)
    {
        switch (action)
        {
            case "cancel":
                var reason = input.Reason?.Trim();
                if (session.Status != SessionStatus.Scheduled || session.StartsAt <= now ||
                    string.IsNullOrWhiteSpace(reason) || reason.Length > 500)
                    return "Only a future scheduled session can be cancelled with a valid reason.";
                session.Status = SessionStatus.Cancelled;
                session.CancelledByUserId = actor;
                session.CancelledAtUtc = now;
                session.CancellationReason = reason;
                await ResolveAbsenceAsync(session.Id, SessionAbsenceRequestStatus.ResolvedByCancellation, actor, now, ct);
                return null;
            case "absence":
                var absenceReason = input.Reason?.Trim();
                if (session.Status != SessionStatus.Scheduled || session.StartsAt <= now ||
                    string.IsNullOrWhiteSpace(absenceReason) || absenceReason.Length > 500)
                    return "Enter a reason for a future scheduled session.";
                if (await PendingAbsenceAsync(session.Id, ct) is not null)
                    return "An absence request is already pending.";
                db.SessionAbsenceRequests.Add(new SessionAbsenceRequest
                {
                    LearningSessionId = session.Id, RequestedByUserId = actor, Reason = absenceReason,
                    CreatedAtUtc = now, RequestedForStartsAtUtc = session.StartsAt
                });
                return null;
            case "withdraw-absence":
                var pending = await PendingAbsenceAsync(session.Id, ct);
                if (pending is null || pending.Id != itemId || pending.RequestedByUserId != actor ||
                    now >= pending.RequestedForStartsAtUtc || session.Status != SessionStatus.Scheduled || session.StartsAt <= now)
                    return "This absence request cannot be withdrawn.";
                pending.Status = SessionAbsenceRequestStatus.Withdrawn;
                pending.ResolvedByUserId = actor;
                pending.ResolvedAtUtc = now;
                return null;
            case "review":
                if (session.Status != SessionStatus.Completed || session.CompletedAtUtc is null)
                    return "Review is available after the session completes.";
                if (input.Rating is not (>= 1 and <= 5) || input.Comment?.Length > 1500)
                    return "Enter a rating from 1 to 5 and a comment of up to 1,500 characters.";
                if (await db.Reviews.AnyAsync(r => r.LearningSessionId == session.Id, ct))
                    return "A review has already been submitted.";
                db.Reviews.Add(new Review
                {
                    LearningSessionId = session.Id, ReviewerId = actor, ReviewedUserId = request.TeacherId,
                    Rating = (byte)input.Rating, Comment = input.Comment?.Trim(), CreatedAtUtc = now.UtcDateTime
                });
                return null;
            default:
                return "This action is unavailable.";
        }
    }

    public async Task<bool> CompleteDueSessionAsync(long requestId, long sessionId, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (!await LockAsync($"SkillSwap:Request:{requestId}", ct)) return false;

        var session = await db.LearningSessions.SingleOrDefaultAsync(s =>
            s.Id == sessionId && s.LearningRequestId == requestId && s.Status == SessionStatus.Scheduled &&
            !s.LearningRequest.IsExpertBooking && s.LearningRequest.Status == RequestStatus.Accepted &&
            s.LearningRequest.EndedAtUtc == null, ct);
        var now = clock.GetUtcNow();
        if (session is null || now - session.StartsAt < TimeSpan.FromMinutes(session.DurationMinutes))
            return false;

        session.Status = SessionStatus.Completed;
        session.CompletedAtUtc = now;
        await ResolveAbsenceAsync(session.Id, SessionAbsenceRequestStatus.ExpiredUnresolved, null, now, ct);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }

    private async Task<string?> EndAsync(LearningRequest request, Guid actor, DateTimeOffset now, CancellationToken ct)
    {
        if (await db.LearningSessions.AnyAsync(s => s.LearningRequestId == request.Id &&
            (s.Status == SessionStatus.Scheduled || s.Status == SessionStatus.PendingConfirmation ||
                s.ScheduleProposals.Any(p => p.Status == ScheduleProposalStatus.Pending)), ct))
            return "Resolve scheduled sessions and pending commitments before ending this relationship.";
        request.EndedAtUtc = now;
        request.EndedByUserId = actor;
        var pending = await PendingRequestAsync(request.Id, ct);
        if (pending is not null) Resolve(pending, LearningSessionRequestStatus.ClosedWithRelationship, actor, now);
        return null;
    }

    private Task<LearningSessionRequest?> PendingRequestAsync(long requestId, CancellationToken ct) =>
        db.LearningSessionRequests.SingleOrDefaultAsync(r => r.LearningRequestId == requestId &&
            r.Status == LearningSessionRequestStatus.Pending, ct);

    private Task<SessionAbsenceRequest?> PendingAbsenceAsync(long sessionId, CancellationToken ct) =>
        db.SessionAbsenceRequests.SingleOrDefaultAsync(r => r.LearningSessionId == sessionId &&
            r.Status == SessionAbsenceRequestStatus.Pending, ct);

    private async Task ResolveAbsenceAsync(long sessionId, SessionAbsenceRequestStatus status, Guid? actor,
        DateTimeOffset now, CancellationToken ct)
    {
        var pending = await PendingAbsenceAsync(sessionId, ct);
        if (pending is null) return;
        var expired = now >= pending.RequestedForStartsAtUtc;
        if (!expired && status == SessionAbsenceRequestStatus.ExpiredUnresolved) return;
        pending.Status = expired ? SessionAbsenceRequestStatus.ExpiredUnresolved : status;
        pending.ResolvedByUserId = expired ? null : actor;
        pending.ResolvedAtUtc = now;
    }

    private static void Resolve(LearningSessionRequest request, LearningSessionRequestStatus status, Guid actor, DateTimeOffset now)
    {
        request.Status = status;
        request.ResolvedByUserId = actor;
        request.ResolvedAtUtc = now;
    }

    private async Task<bool> LockParticipantsAsync(LearningRequest request, CancellationToken ct)
    {
        foreach (var participant in new[] { request.LearnerId, request.TeacherId }.Order())
            if (!await LockAsync($"SkillSwap:Schedule:{participant:D}", ct)) return false;
        return true;
    }

    internal IQueryable<Guid> EligibleParticipants(LearningRequest request) =>
        db.Users.Where(u => (u.Id == request.LearnerId || u.Id == request.TeacherId) &&
            u.Status == AccountStatus.Active && u.EmailConfirmed && u.IsProfileComplete &&
            db.UserRoles.Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.NormalizedName == "MEMBER")) &&
            !db.UserRoles.Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.NormalizedName == "ADMIN")))
            .Select(u => u.Id);

    internal async Task<bool> LockAsync(string resource, CancellationToken ct)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = db.Database.CurrentTransaction!.GetDbTransaction();
        command.CommandText = "DECLARE @rc int; EXEC @rc=sys.sp_getapplock @Resource=@resource, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=2000; SELECT @rc;";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@resource";
        parameter.Value = resource;
        command.Parameters.Add(parameter);
        return await command.ExecuteScalarAsync(ct) is int result && result >= 0;
    }
}
