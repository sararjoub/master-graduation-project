using Microsoft.EntityFrameworkCore;
using SkillsSwap.Data;
using SkillsSwap.Models;

namespace SkillsSwap.Services;

public sealed class ExpertCreditWorker(IServiceScopeFactory scopes, TimeProvider clock,
    ILogger<ExpertCreditWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1), clock);
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var now = clock.GetUtcNow().UtcDateTime;
                var users = await PlusEntitlement.Verified(db).Where(s => s.BillingPeriod != null &&
                        s.Status == SubscriptionStatus.Active && s.StartsAtUtc <= now &&
                        !s.CreditAllocations.Any(c => c.StartsAtUtc <= now && now < c.EndsAtUtc))
                    .Select(s => s.UserId).Distinct().ToListAsync(stoppingToken);
                foreach (var user in users)
                {
                    await using var memberScope = scopes.CreateAsyncScope();
                    await memberScope.ServiceProvider.GetRequiredService<SubscriptionPayments>().RefreshAsync(user, stoppingToken);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning("Subscription credit processing failed ({ErrorType}).", ex.GetType().Name);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
