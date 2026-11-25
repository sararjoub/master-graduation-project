using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SkillsSwap.Data;
using SkillsSwap.Models;

namespace SkillsSwap.Services;

public sealed class PeerRequestService(ApplicationDbContext db)
{
    private const int PageSize = 12;
    private const string ExistingRequest = "You already have an active learning request for this teacher and skill.";
    private const string RetryRequest = "Another request operation was in progress. Please try again.";

    private IQueryable<ApplicationUser> Members => db.Users.Where(u =>
        u.Status == AccountStatus.Active && u.EmailConfirmed && u.IsProfileComplete &&
        db.UserRoles.Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.NormalizedName == "MEMBER")) &&
        !db.UserRoles.Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.NormalizedName == "ADMIN")));

    private IQueryable<LearningRequest> PeerRequests => db.LearningRequests.AsNoTracking().Where(r => !r.IsExpertBooking);

    public Task<long?> FindActiveIdAsync(Guid learner, Guid teacher, int skill, CancellationToken ct) =>
        PeerRequests.Where(r => r.LearnerId == learner && r.TeacherId == teacher && r.SkillId == skill &&
                r.EndedAtUtc == null && (r.Status == RequestStatus.Pending || r.Status == RequestStatus.Accepted))
            .OrderBy(r => r.Id).Select(r => (long?)r.Id).FirstOrDefaultAsync(ct);

    public async Task<PeerRequestTarget?> TargetAsync(Guid learner, Guid teacher, int skill, CancellationToken ct)
    {
        if (learner == teacher || teacher == Guid.Empty || skill <= 0) return null;
        var now = DateTime.UtcNow;
        return await db.UserSkills.AsNoTracking().Where(x =>
            x.UserId == teacher && x.SkillId == skill && x.Intent == SkillIntent.Teach &&
            Members.Any(u => u.Id == teacher) && Members.Any(u => u.Id == learner) &&
            x.Skill.Status == PublicationStatus.Published && x.Skill.SubCategory.IsActive &&
            x.Skill.SubCategory.Category.IsActive &&
            (x.Skill.AccessTier == AccessTier.Free || db.Subscriptions.Any(s =>
                s.UserId == learner && s.Plan.Code == "PLUS" && s.Plan.IsActive &&
                s.Status == SubscriptionStatus.Active && s.StartsAtUtc <= now && s.EndsAtUtc > now)))
            .Select(x => new PeerRequestTarget(x.User.FullName, x.Skill.NameEn, x.Skill.Slug))
            .SingleOrDefaultAsync(ct);
    }

    public async Task<(long? Id, string? Error)> CreateAsync(Guid learner, PeerRequestInput input, CancellationToken ct)
    {
        var message = input.Message?.Trim();
        if (string.IsNullOrEmpty(message) || message.Length > 1500)
            return (null, "Enter a learning goal up to 1,500 characters.");
        LearningRequest? request = null;
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            if (await TargetAsync(learner, input.TeacherId, input.SkillId, ct) is null)
                return (null, "This teaching partner or skill is unavailable for your account.");
            if (await FindActiveIdAsync(learner, input.TeacherId, input.SkillId, ct) is not null)
                return (null, ExistingRequest);
            request = new LearningRequest
            {
                LearnerId = learner, TeacherId = input.TeacherId, SkillId = input.SkillId, Message = message,
                CreatedAtUtc = DateTime.UtcNow, Status = RequestStatus.Pending, IsExpertBooking = false
            };
            db.LearningRequests.Add(request);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return (request.Id, null);
        }
        catch (Exception ex) when (HasSqlError(ex, 1205, 2601, 2627))
        {
            // The transaction has been disposed. Remove only this attempt's tracked entity.
            if (request is not null)
                db.Entry(request).State = EntityState.Detached;

            if (HasSqlError(ex, 1205))
                return (null, RetryRequest);

            if (request is not null && await FindActiveIdAsync(learner, input.TeacherId, input.SkillId, ct) is not null)
                return (null, ExistingRequest);

            throw;
        }
    }

    internal static bool HasSqlError(Exception exception, params int[] numbers)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
            if (current is SqlException sql && sql.Errors.Cast<SqlError>().Any(error => numbers.Contains(error.Number)))
                return true;
        return false;
    }

    public Task<LearningRequest?> FindAsync(long id, CancellationToken ct) =>
        PeerRequests.SingleOrDefaultAsync(r => r.Id == id, ct);

    private static IQueryable<PeerRequestRow> Rows(IQueryable<LearningRequest> query, Guid actor) =>
        query.Select(r => new PeerRequestRow(r.Id, r.TeacherId == actor ? r.Learner.FullName : r.Teacher.FullName,
            r.Skill.NameEn, r.Message, r.CreatedAtUtc, r.RespondedAtUtc, r.Status, r.TeacherId == actor,
            r.TeacherId == actor ? r.Learner.ProfileImageUrl : r.Teacher.ProfileImageUrl));

    public Task<PeerRequestRow?> DetailAsync(long id, Guid actor, CancellationToken ct) =>
        Rows(PeerRequests.Where(r => r.Id == id && (r.LearnerId == actor || r.TeacherId == actor)), actor).SingleOrDefaultAsync(ct);

    public async Task<PeerRequestList> ListAsync(Guid actor, string tab, int page, CancellationToken ct,
        string? search = null, RequestStatus? status = null)
    {
        page = Math.Clamp(page, 1, int.MaxValue / PageSize);
        var incoming = PeerRequests.Where(r => r.TeacherId == actor);
        var outgoing = PeerRequests.Where(r => r.LearnerId == actor);
        var selected = tab switch
        {
            "outgoing" => outgoing,
            "completed" => PeerRequests.Where(r => (r.TeacherId == actor || r.LearnerId == actor) && r.Status != RequestStatus.Pending),
            _ => incoming
        };
        search = search?.Trim() ?? "";
        search = search[..Math.Min(search.Length, 120)];
        if (status.HasValue)
            selected = selected.Where(r => r.Status == status);
        if (search.Length > 0)
            selected = selected.Where(r => r.Skill.NameEn.Contains(search) ||
                (r.TeacherId == actor ? r.Learner.FullName : r.Teacher.FullName).Contains(search));
        var rows = await Rows(selected.OrderByDescending(r => r.CreatedAtUtc).ThenByDescending(r => r.Id)
            .Skip((page - 1) * PageSize).Take(PageSize + 1), actor).ToListAsync(ct);
        return new(rows.Take(PageSize).ToArray(), tab, page, rows.Count > PageSize,
            await incoming.CountAsync(r => r.Status == RequestStatus.Pending, ct),
            await outgoing.CountAsync(r => r.Status == RequestStatus.Pending, ct), search, status);
    }

    public async Task<string?> TransitionAsync(LearningRequest request, Guid actor, RequestStatus next, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (!await Members.AnyAsync(u => u.Id == actor, ct))
            return "Your account is no longer eligible.";
        if (next == RequestStatus.Accepted && await TargetAsync(request.LearnerId, request.TeacherId, request.SkillId, ct) is null)
            return "The learner, teacher, or skill is no longer eligible.";
        var now = DateTime.UtcNow;
        var updated = await PeerRequests.Where(r => r.Id == request.Id && r.Status == RequestStatus.Pending &&
            (next == RequestStatus.Cancelled && r.LearnerId == actor ||
            (next == RequestStatus.Accepted || next == RequestStatus.Declined) && r.TeacherId == actor))
            .ExecuteUpdateAsync(set => set.SetProperty(r => r.Status, next).SetProperty(r => r.RespondedAtUtc, (DateTime?)now), ct);
        await transaction.CommitAsync(ct);
        return updated == 1 ? null : "This request has already been processed.";
    }
}
