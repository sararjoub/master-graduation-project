using Microsoft.EntityFrameworkCore;
using SkillsSwap.Data;
using SkillsSwap.Models;

namespace SkillsSwap.Services;

public sealed class PlusEntitlement(ApplicationDbContext db, TimeProvider clock)
{
    public static IQueryable<Subscription> Verified(ApplicationDbContext db) =>
        db.Subscriptions.Where(s => s.Plan.Code == SubscriptionTerms.Plus &&
            (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Expired) &&
            s.StartsAtUtc < s.EndsAtUtc && db.Payments.Any(p => p.SubscriptionId == s.Id &&
                p.Status == PaymentStatus.Paid && p.Amount > 0));

    public static IQueryable<Subscription> Active(ApplicationDbContext db, DateTime now) =>
        Verified(db).Where(s => s.Status == SubscriptionStatus.Active && s.StartsAtUtc <= now && now < s.EndsAtUtc);

    public Task<bool> HasAccessAsync(Guid member, CancellationToken ct = default) =>
        Active(db, clock.GetUtcNow().UtcDateTime).AnyAsync(s => s.UserId == member, ct);
}
