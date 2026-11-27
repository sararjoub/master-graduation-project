using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SkillsSwap.Data;
using SkillsSwap.Models;

namespace SkillsSwap.Services;

public interface IPaymentGateway
{
    bool Enabled { get; }
    string Provider { get; }
    PaymentStatus Process(SimulatedOutcome outcome);
}

public sealed class SimulatedPaymentGateway(IHostEnvironment environment, IConfiguration configuration) : IPaymentGateway
{
    public bool Enabled => (environment.IsDevelopment() || environment.IsEnvironment("Demonstration")) &&
        configuration.GetValue<bool>("Payments:EnableSimulation");
    public const string ProviderName = "SkillSwap Simulation";
    public string Provider => ProviderName;
    public PaymentStatus Process(SimulatedOutcome outcome)
    {
        if (!Enabled) throw new InvalidOperationException("Simulated payments are disabled.");
        return outcome switch
        {
            SimulatedOutcome.Success => PaymentStatus.Paid,
            SimulatedOutcome.Failure => PaymentStatus.Failed,
            SimulatedOutcome.Cancel => PaymentStatus.Cancelled,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome))
        };
    }
}

public sealed class SubscriptionPayments(ApplicationDbContext db, TimeProvider clock,
    IPaymentGateway gateway, PeerSessionService locks)
{
    public bool Enabled => gateway.Enabled;
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    public Task<Plan?> PlusAsync(CancellationToken ct) => db.Plans.AsNoTracking()
        .SingleOrDefaultAsync(p => p.Code == SubscriptionTerms.Plus && p.IsActive, ct);

    private Task<DateTime?> TailAsync(Guid user, DateTime now, CancellationToken ct) =>
        PlusEntitlement.Verified(db).Where(s => s.UserId == user && s.Status == SubscriptionStatus.Active && s.EndsAtUtc > now)
            .MaxAsync(s => (DateTime?)s.EndsAtUtc, ct);

    public async Task<CheckoutPage?> CheckoutAsync(Guid user, BillingPeriod period, CancellationToken ct)
    {
        if (!Enum.IsDefined(period)) return null;
        var plus = await PlusAsync(ct);
        if (plus is null) return null;
        var now = Now;
        var start = await TailAsync(user, now, ct) ?? now;
        return new(plus, period, start, SubscriptionTerms.End(start, period), Enabled);
    }

    public async Task<Payment?> QuoteAsync(Guid user, BillingPeriod period, CancellationToken ct)
    {
        if (!Enabled) return null;
        var checkout = await CheckoutAsync(user, period, ct);
        if (checkout is null) return null;
        var plan = checkout.Plan;
        var payment = new Payment
        {
            Subscription = new Subscription
            {
                UserId = user, PlanId = plan.Id, Status = SubscriptionStatus.Pending,
                StartsAtUtc = checkout.StartsAt, EndsAtUtc = checkout.EndsAt, BillingPeriod = period,
                PurchasedMonthlyCredits = plan.MonthlyExpertCredits
            },
            Provider = gateway.Provider,
            ProviderReference = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)),
            Amount = period == BillingPeriod.Yearly ? plan.YearlyPrice : plan.MonthlyPrice,
            Currency = SubscriptionTerms.Currency, Status = PaymentStatus.Pending, CreatedAtUtc = Now
        };
        if (payment.Amount <= 0) return null;
        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);
        return payment;
    }

    public Task<Payment?> FindAsync(Guid user, string reference, CancellationToken ct) =>
        db.Payments.AsNoTracking().Include(p => p.Subscription).ThenInclude(s => s.Plan)
            .SingleOrDefaultAsync(p => p.Subscription.UserId == user && p.Provider == gateway.Provider &&
                p.ProviderReference == reference, ct);

    public async Task<bool> ProcessAsync(Guid user, string reference, SimulatedOutcome choice, CancellationToken ct)
    {
        if (!Enabled || !Enum.IsDefined(choice)) return false;
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (!await locks.LockAsync($"SkillSwap:Subscription:{user:D}", ct)) return false;
        var payment = await db.Payments.Include(p => p.Subscription)
            .SingleOrDefaultAsync(p => p.Subscription.UserId == user && p.Provider == gateway.Provider &&
                p.ProviderReference == reference, ct);
        if (payment is null) return false;
        if (payment.Status != PaymentStatus.Pending) return true;
        var now = Now;
        var sub = payment.Subscription;
        if (sub.Status != SubscriptionStatus.Pending || sub.BillingPeriod is null ||
            !Enum.IsDefined(sub.BillingPeriod.Value) || payment.Amount <= 0 ||
            payment.Currency != SubscriptionTerms.Currency) return false;
        var tail = await TailAsync(user, now, ct);
        var stale = now >= payment.CreatedAtUtc.AddMinutes(15) ||
            (tail is { } end && end != sub.StartsAtUtc) ||
            (tail is null && sub.StartsAtUtc > now);
        payment.Status = stale ? PaymentStatus.Cancelled : gateway.Process(choice);
        if (payment.Status == PaymentStatus.Paid)
        {
            sub.StartsAtUtc = tail ?? now;
            sub.EndsAtUtc = SubscriptionTerms.End(sub.StartsAtUtc, sub.BillingPeriod!.Value);
            sub.Status = SubscriptionStatus.Active;
            Allocate(sub, now);
        }
        else sub.Status = SubscriptionStatus.Cancelled;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }

    private void Allocate(Subscription sub, DateTime now)
    {
        var loaded = sub.CreditAllocations.Select(c => c.StartsAtUtc).ToHashSet();
        foreach (var (start, end) in SubscriptionTerms.CreditPeriods(sub.StartsAtUtc, sub.EndsAtUtc, now))
        {
            if (!loaded.Add(start)) continue;
            sub.CreditAllocations.Add(new ExpertCreditAllocation
            {
                StartsAtUtc = start, EndsAtUtc = end, Allocated = sub.PurchasedMonthlyCredits,
                Remaining = sub.PurchasedMonthlyCredits
            });
        }
    }

    public async Task RefreshAsync(Guid user, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (!await locks.LockAsync($"SkillSwap:Subscription:{user:D}", ct)) return;
        var now = Now;
        var terms = await PlusEntitlement.Verified(db).Where(s => s.UserId == user && s.BillingPeriod != null &&
                s.StartsAtUtc <= now && s.Status == SubscriptionStatus.Active)
            .Include(s => s.CreditAllocations).ToListAsync(ct);
        foreach (var sub in terms)
        {
            Allocate(sub, now);
            if (sub.EndsAtUtc <= now) sub.Status = SubscriptionStatus.Expired;
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task<MembershipPage> PageAsync(Guid? user, BillingPeriod period, CancellationToken ct)
    {
        var plus = await PlusAsync(ct);
        var now = Now;
        if (user is null) return new(plus, null, null, null, [], period, now, Enabled);
        var current = await PlusEntitlement.Active(db, now).AsNoTracking().Where(s => s.UserId == user)
            .OrderByDescending(s => s.EndsAtUtc).FirstOrDefaultAsync(ct);
        var latest = await PlusEntitlement.Verified(db).AsNoTracking().Where(s => s.UserId == user)
            .OrderByDescending(s => s.EndsAtUtc).FirstOrDefaultAsync(ct);
        var credit = current is null ? null : await db.Set<ExpertCreditAllocation>().AsNoTracking()
            .SingleOrDefaultAsync(c => c.SubscriptionId == current.Id && c.StartsAtUtc <= now && now < c.EndsAtUtc, ct);
        var recent = await db.Payments.AsNoTracking().Include(p => p.Subscription).Where(p => p.Subscription.UserId == user)
            .OrderByDescending(p => p.Id).Take(50).ToListAsync(ct);
        var future = await PlusEntitlement.Verified(db).AsNoTracking()
            .Where(s => s.UserId == user && s.Status == SubscriptionStatus.Active && s.StartsAtUtc > now)
            .OrderBy(s => s.StartsAtUtc).ToListAsync(ct);
        return new(plus, current, latest, credit, recent, period, now, Enabled) { Future = future };
    }
}
