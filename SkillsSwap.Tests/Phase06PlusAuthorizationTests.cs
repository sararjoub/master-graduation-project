using Microsoft.EntityFrameworkCore;
using SkillsSwap.Models;
using SkillsSwap.Services;

namespace SkillsSwap.Tests;

public sealed partial class SqlServerPhase04Tests
{
    [SqlServerPhase04Fact]
    public async Task Premium_discovery_and_new_requests_require_current_paid_learner_entitlement()
    {
        await using var sql = await OpenAsync();
        await using var db = Context(sql);
        var learner = Guid.NewGuid();
        var teacher = Guid.NewGuid();
        var now = new SessionTestClock(new DateTimeOffset(2092, 2, 1, 0, 0, 0, TimeSpan.Zero));
        var role = await db.Roles.SingleAsync(r => r.NormalizedName == "MEMBER");
        var catalogue = db.Skills.Where(s => s.Status == PublicationStatus.Published &&
            s.SubCategory.IsActive && s.SubCategory.Category.IsActive);
        var free = await catalogue.FirstAsync(s => s.AccessTier == AccessTier.Free);
        var premium = await catalogue.FirstAsync(s => s.AccessTier == AccessTier.Plus);
        var otherPremium = await catalogue.FirstAsync(s => s.AccessTier == AccessTier.Plus && s.Id != premium.Id);
        var plan = await db.Plans.SingleAsync(p => p.Code == SubscriptionTerms.Plus);
        db.Users.AddRange(Member(learner), Member(teacher));
        db.UserRoles.AddRange(new() { UserId = learner, RoleId = role.Id }, new() { UserId = teacher, RoleId = role.Id });
        db.UserSkills.AddRange(new() { UserId = teacher, SkillId = free.Id, Intent = SkillIntent.Teach },
                              new() { UserId = teacher, SkillId = premium.Id, Intent = SkillIntent.Teach },
                              new() { UserId = teacher, SkillId = otherPremium.Id, Intent = SkillIntent.Teach });
        await db.SaveChangesAsync();
        try
        {
            var discovery = new SkillDiscoveryService(db, now);
            var requests = new PeerRequestService(db, now);
            var input = new PeerRequestInput { SkillId = premium.Id, TeacherId = teacher, Message = "Learn this skill" };
            var sub = new Subscription
            {
                PlanId = plan.Id, UserId = learner, Status = SubscriptionStatus.Active,
                StartsAtUtc = now.Now.UtcDateTime, EndsAtUtc = now.Now.UtcDateTime.AddMonths(1)
            };
            var payment = new Payment
            {
                Subscription = sub, Provider = SimulatedPaymentGateway.ProviderName, ProviderReference = Guid.NewGuid().ToString(),
                Amount = plan.MonthlyPrice, Status = PaymentStatus.Pending
            };

            Assert.NotNull(await discovery.DiscoverAsync(free.Slug, learner, new()));
            Assert.NotNull(await requests.TargetAsync(learner, teacher, free.Id, default));
            async Task AssertRestricted()
            {
                Assert.Null(await discovery.DiscoverAsync(premium.Slug, learner, new()));
                Assert.Null((await requests.CreateAsync(learner, input, default)).Id);
            }
            await AssertRestricted();
            db.Payments.Add(payment);
            foreach (var status in new[] { PaymentStatus.Pending, PaymentStatus.Failed, PaymentStatus.Cancelled })
            {
                payment.Status = status;
                await db.SaveChangesAsync();
                await AssertRestricted();
            }

            payment.Status = PaymentStatus.Paid;
            sub.StartsAtUtc = now.Now.UtcDateTime.AddMinutes(1);
            await db.SaveChangesAsync();
            await AssertRestricted();
            now.Now = now.Now.AddMinutes(1);
            var result = Assert.IsType<SkillDiscoveryViewModel>(await discovery.DiscoverAsync(premium.Slug, learner, new()));
            Assert.Contains(result.Partners, p => p.UserId == teacher);
            Assert.Null(await discovery.DiscoverAsync(premium.Slug, teacher, new()));
            var acceptedId = Assert.IsType<long>((await requests.CreateAsync(learner, input, default)).Id);
            var existing = await db.LearningRequests.AsNoTracking().SingleAsync(r => r.Id == acceptedId);
            Assert.Null(await requests.TransitionAsync(existing, teacher, RequestStatus.Accepted, default));

            var pending = new LearningRequest
            {
                LearnerId = learner, TeacherId = teacher, SkillId = otherPremium.Id,
                Status = RequestStatus.Pending, Message = "Pending premium request"
            };
            db.LearningRequests.Add(pending);
            await db.SaveChangesAsync();
            Assert.NotNull(await requests.TargetAsync(learner, teacher, otherPremium.Id, default));
            now.Now = new(sub.EndsAtUtc, TimeSpan.Zero);
            await AssertRestricted();
            Assert.NotNull(await requests.TransitionAsync(pending, teacher, RequestStatus.Accepted, default));
            Assert.Equal(RequestStatus.Accepted, (await db.LearningRequests.AsNoTracking().SingleAsync(r => r.Id == acceptedId)).Status);
        }
        finally
        {
            await db.Payments.Where(p => p.Subscription.UserId == learner).ExecuteDeleteAsync();
            await db.Subscriptions.Where(s => s.UserId == learner).ExecuteDeleteAsync();
            await db.LearningRequests.Where(r => r.LearnerId == learner || r.LearnerId == teacher).ExecuteDeleteAsync();
            await db.UserSkills.Where(s => s.UserId == teacher).ExecuteDeleteAsync();
            await db.UserRoles.Where(r => r.UserId == learner || r.UserId == teacher).ExecuteDeleteAsync();
            await db.Users.Where(u => u.Id == learner || u.Id == teacher).ExecuteDeleteAsync();
        }
    }
}
