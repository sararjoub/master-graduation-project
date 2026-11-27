using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using SkillsSwap.Models;
using SkillsSwap.Services;

namespace SkillsSwap.Tests;

public sealed class Phase06SubscriptionTests
{
    [Fact]
    public void Calendar_boundaries_and_monthly_annual_credit_windows()
    {
        var start = new DateTime(2024, 1, 31, 9, 0, 0, DateTimeKind.Utc);
        Assert.Equal(new DateTime(2024, 2, 29, 9, 0, 0, DateTimeKind.Utc), SubscriptionTerms.End(start, BillingPeriod.Monthly));
        Assert.Equal(new DateTime(2025, 2, 28), SubscriptionTerms.End(new DateTime(2024, 2, 29), BillingPeriod.Yearly));
        var end = SubscriptionTerms.End(start, BillingPeriod.Yearly);
        Assert.Single(SubscriptionTerms.CreditPeriods(start, end, start));
        var periods = SubscriptionTerms.CreditPeriods(start, end, end).ToList();
        Assert.Equal(12, periods.Count);
        Assert.Equal(start.AddMonths(2), periods[2].Start);
        Assert.Equal(end, periods[^1].End);
        Assert.All(periods.Zip(periods.Skip(1)), pair => Assert.Equal(pair.First.End, pair.Second.Start));
    }

    [Theory]
    [InlineData("Production", true, false)]
    [InlineData("Staging", true, false)]
    [InlineData("Development", false, false)]
    [InlineData("Development", true, true)]
    [InlineData("Demonstration", true, true)]
    public void Simulation_requires_explicit_configuration_in_an_approved_environment(string environment, bool setting, bool allowed)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { ["Payments:EnableSimulation"] = setting.ToString() }).Build();
        var gateway = new SimulatedPaymentGateway(new TestEnvironment { EnvironmentName = environment }, configuration);
        Assert.Equal(allowed, gateway.Enabled);
        if (!allowed) Assert.Throws<InvalidOperationException>(() => gateway.Process(SimulatedOutcome.Success));
        else Assert.Equal(PaymentStatus.Paid, gateway.Process(SimulatedOutcome.Success));
    }

    [Fact]
    public void Membership_administration_requires_admin()
    {
        var attribute = Assert.Single(typeof(SkillsSwap.Areas.Admin.Controllers.MembershipController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal("Admin", attribute.Roles);
    }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "";
        public string ApplicationName { get; set; } = "";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}

public sealed partial class SqlServerPhase04Tests
{
    [SqlServerPhase04Fact]
    public async Task Phase06_subscription_lifecycle_and_concurrent_idempotency_use_isolated_SQL_Server()
    {
        await using var sql = await OpenAsync();
        await using var db = Context(sql);
        Assert.True(await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20261127150937_Phase06SubscriptionEntitlements'").SingleAsync() == 1);
        var user = Guid.NewGuid();
        var outsider = Guid.NewGuid();
        var clock = new SessionTestClock(new DateTimeOffset(2092, 1, 31, 12, 0, 0, TimeSpan.Zero));
        var gateway = new TestPaymentGateway();
        db.Users.Add(Member(user));
        await db.SaveChangesAsync();
        var plus = await db.Plans.AsNoTracking().SingleAsync(p => p.Code == SubscriptionTerms.Plus);
        try
        {
            var service = new SubscriptionPayments(db, clock, gateway, new PeerSessionService(db, clock));
            var entitlements = new PlusEntitlement(db, clock);
            Assert.False(await entitlements.HasAccessAsync(user));
            Assert.Null(await service.QuoteAsync(user, (BillingPeriod)99, default));

            async Task<bool> Process(string reference, SimulatedOutcome outcome, IPaymentGateway? testGateway = null)
            {
                await using var connection = await OpenAsync();
                await using var context = Context(connection);
                return await new SubscriptionPayments(context, clock, testGateway ?? gateway,
                    new PeerSessionService(context, clock)).ProcessAsync(user, reference, outcome, default);
            }

            var failure = Assert.IsType<Payment>(await service.QuoteAsync(user, BillingPeriod.Monthly, default));
            Assert.Null(await service.FindAsync(outsider, failure.ProviderReference, default));
            Assert.True(await Process(failure.ProviderReference, SimulatedOutcome.Failure));
            Assert.True(await Process(failure.ProviderReference, SimulatedOutcome.Success));
            Assert.Equal(PaymentStatus.Failed, (await service.FindAsync(user, failure.ProviderReference, default))!.Status);
            var cancelled = Assert.IsType<Payment>(await service.QuoteAsync(user, BillingPeriod.Yearly, default));
            Assert.True(await Process(cancelled.ProviderReference, SimulatedOutcome.Cancel));
            Assert.Equal(PaymentStatus.Cancelled, (await service.FindAsync(user, cancelled.ProviderReference, default))!.Status);
            Assert.False(await entitlements.HasAccessAsync(user));

            var annual = Assert.IsType<Payment>(await service.QuoteAsync(user, BillingPeriod.Yearly, default));
            var stale = Assert.IsType<Payment>(await service.QuoteAsync(user, BillingPeriod.Monthly, default));
            Assert.Equal(plus.YearlyPrice, annual.Amount);
            Assert.All(await Task.WhenAll(Process(annual.ProviderReference, SimulatedOutcome.Success),
                Process(annual.ProviderReference, SimulatedOutcome.Success)), x => Assert.True(x));
            Assert.True(await Process(stale.ProviderReference, SimulatedOutcome.Success));
            Assert.Equal(PaymentStatus.Cancelled, (await service.FindAsync(user, stale.ProviderReference, default))!.Status);
            Assert.True(await entitlements.HasAccessAsync(user));
            var firstCredits = db.Set<ExpertCreditAllocation>().AsNoTracking().Where(c => c.SubscriptionId == annual.SubscriptionId);
            Assert.Equal(plus.MonthlyExpertCredits, (await firstCredits.SingleAsync()).Allocated);
            var firstEnd = clock.Now.UtcDateTime.AddYears(1);
            await firstCredits.ExecuteUpdateAsync(set => set.SetProperty(c => c.Remaining, 0));
            Assert.Equal(0, (await service.PageAsync(user, BillingPeriod.Yearly, default)).Credits!.Remaining);

            var renewal = Assert.IsType<Payment>(await service.QuoteAsync(user, BillingPeriod.Monthly, default));
            Assert.Equal(plus.MonthlyPrice, renewal.Amount);
            Assert.Equal(firstEnd, renewal.Subscription.StartsAtUtc);
            Assert.True(await Process(renewal.ProviderReference, SimulatedOutcome.Success));
            Assert.Empty(await db.Set<ExpertCreditAllocation>().Where(c => c.SubscriptionId == renewal.SubscriptionId).ToListAsync());

            clock.Now = clock.Now.AddMonths(3);
            async Task Refresh()
            {
                await using var connection = await OpenAsync();
                await using var context = Context(connection);
                await new SubscriptionPayments(context, clock, gateway, new PeerSessionService(context, clock))
                    .RefreshAsync(user, default);
            }
            await Task.WhenAll(Refresh(), Refresh());
            Assert.Equal(4, await firstCredits.CountAsync());
            Assert.Equal(plus.MonthlyExpertCredits, (await service.PageAsync(user, BillingPeriod.Monthly, default)).Credits!.Remaining);

            clock.Now = new DateTimeOffset(firstEnd, TimeSpan.Zero);
            Assert.True(await entitlements.HasAccessAsync(user));
            await Refresh();
            Assert.Equal(12, await firstCredits.CountAsync());
            Assert.Single(await db.Set<ExpertCreditAllocation>().Where(c => c.SubscriptionId == renewal.SubscriptionId).ToListAsync());
            clock.Now = clock.Now.AddMonths(1);
            Assert.False(await entitlements.HasAccessAsync(user));
            var restart = Assert.IsType<Payment>(await service.QuoteAsync(user, BillingPeriod.Monthly, default));
            Assert.Equal(clock.Now.UtcDateTime, restart.Subscription.StartsAtUtc);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Process(restart.ProviderReference, SimulatedOutcome.Success, new TestPaymentGateway { ThrowOnProcess = true }));
            Assert.Equal(PaymentStatus.Pending, (await service.FindAsync(user, restart.ProviderReference, default))!.Status);
            Assert.False(await entitlements.HasAccessAsync(user));
            var operations = new AdminOperationsService(db, clock, new PeerSessionService(db, clock));
            var settings = new AdminPlanFormViewModel
            {
                Id = plus.Id, Name = plus.Name, IsActive = plus.IsActive,
                MonthlyPrice = plus.MonthlyPrice + 1, YearlyPrice = plus.YearlyPrice,
                MonthlyExpertCredits = plus.MonthlyExpertCredits + 1,
                AdditionalExpertPrice = plus.AdditionalExpertPrice, ExpertSessionMinutes = plus.ExpertSessionMinutes,
                ExpertSharePercent = plus.ExpertSharePercent, PlatformSharePercent = 100
            };
            Assert.False(await operations.SavePlanAsync(settings));
            settings.PlatformSharePercent = 100 - settings.ExpertSharePercent;
            try
            {
                Assert.True(await operations.SavePlanAsync(settings));
                Assert.True(await Process(restart.ProviderReference, SimulatedOutcome.Success));
                Assert.True(await entitlements.HasAccessAsync(user));
                var purchased = (await service.FindAsync(user, restart.ProviderReference, default))!;
                Assert.Equal(plus.MonthlyPrice, purchased.Amount);
                Assert.Equal(plus.MonthlyExpertCredits, purchased.Subscription.PurchasedMonthlyCredits);
                var latestQuote = Assert.IsType<Payment>(await service.QuoteAsync(user, BillingPeriod.Monthly, default));
                Assert.Equal(settings.MonthlyPrice, latestQuote.Amount);
                Assert.Equal(settings.MonthlyExpertCredits, latestQuote.Subscription.PurchasedMonthlyCredits);

                var adminPayments = await operations.GetPaymentsAsync(restart.ProviderReference, PaymentStatus.Paid, null);
                Assert.Equal(purchased.Amount, Assert.Single(adminPayments.Payments).Amount);
                var adminSubs = await operations.GetSubscriptionsAsync(null, SubscriptionStatus.Active, purchased.SubscriptionId);
                Assert.Equal(purchased.SubscriptionId, Assert.Single(adminSubs.Subscriptions).Id);
            }
            finally
            {
                settings.MonthlyPrice = plus.MonthlyPrice;
                settings.MonthlyExpertCredits = plus.MonthlyExpertCredits;
                Assert.True(await operations.SavePlanAsync(settings));
            }
        }
        finally
        {
            await db.Payments.Where(p => p.Subscription.UserId == user).ExecuteDeleteAsync();
            await db.Subscriptions.Where(s => s.UserId == user).ExecuteDeleteAsync();
            await db.Users.Where(u => u.Id == user).ExecuteDeleteAsync();
        }
    }

    private sealed class TestPaymentGateway : IPaymentGateway
    {
        public bool Enabled => true;
        public string Provider => SimulatedPaymentGateway.ProviderName;
        public bool ThrowOnProcess { get; init; }

        public PaymentStatus Process(SimulatedOutcome outcome) => ThrowOnProcess ? throw new InvalidOperationException("Test failure") :
            outcome == SimulatedOutcome.Success ? PaymentStatus.Paid :
            outcome == SimulatedOutcome.Failure ? PaymentStatus.Failed : PaymentStatus.Cancelled;
    }
}
