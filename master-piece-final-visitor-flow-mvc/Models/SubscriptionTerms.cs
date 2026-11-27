namespace SkillsSwap.Models;

public enum BillingPeriod { Monthly = 1, Yearly = 2 }
public enum SimulatedOutcome { Success = 1, Failure = 2, Cancel = 3 }

public static class SubscriptionTerms
{
    public const string Plus = "PLUS";
    public const string Currency = "USD";
    public static DateTime End(DateTime start, BillingPeriod period) =>
        period == BillingPeriod.Yearly ? start.AddYears(1) : start.AddMonths(1);

    public static IEnumerable<(DateTime Start, DateTime End)> CreditPeriods(DateTime start, DateTime end, DateTime now)
    {
        for (var month = 0; month < 12; month++)
        {
            var from = start.AddMonths(month);
            if (from >= end || from > now) yield break;
            var to = start.AddMonths(month + 1);
            yield return (from, to < end ? to : end);
        }
    }
}

public sealed class ExpertCreditAllocation
{
    public int SubscriptionId { get; set; }
    public Subscription Subscription { get; set; } = null!;
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public int Allocated { get; set; }
    public int Remaining { get; set; }
}

public sealed record MembershipPage(Plan? Plus, Subscription? Current, Subscription? Latest,
    ExpertCreditAllocation? Credits, IReadOnlyList<Payment> History, BillingPeriod Period, DateTime Now, bool DemoEnabled)
{
    public IReadOnlyList<Subscription> Future { get; init; } = [];
}
public sealed record CheckoutPage(Plan Plan, BillingPeriod Period, DateTime StartsAt, DateTime EndsAt, bool DemoEnabled);
