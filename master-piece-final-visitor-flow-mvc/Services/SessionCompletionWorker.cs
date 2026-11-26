using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SkillsSwap.Data;
using SkillsSwap.Models;

namespace SkillsSwap.Services;

public sealed class SessionCompletionOptions
{
    public int IntervalSeconds { get; set; } = 30;
}

public sealed class SessionCompletionWorker(IServiceScopeFactory scopes, TimeProvider clock,
    IOptions<SessionCompletionOptions> options, ILogger<SessionCompletionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.IntervalSeconds), clock);
        do
        {
            try
            {
                await SweepAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning("Automatic completion sweep failed ({ErrorType}). Retrying on the next interval.",
                    ex.GetType().Name);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task SweepAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        long cursor = 0;
        while (!ct.IsCancellationRequested)
        {
            await using var queryScope = scopes.CreateAsyncScope();
            var db = queryScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var candidates = await db.LearningSessions.AsNoTracking()
                .Where(s => s.Id > cursor && s.Status == SessionStatus.Scheduled && s.StartsAt <= now &&
                    !s.LearningRequest.IsExpertBooking && s.LearningRequest.Status == RequestStatus.Accepted &&
                    s.LearningRequest.EndedAtUtc == null && s.StartsAt.AddMinutes(s.DurationMinutes) <= now)
                .OrderBy(s => s.Id).Take(100).Select(s => new { s.Id, s.LearningRequestId }).ToListAsync(ct);
            if (candidates.Count == 0) return;
            foreach (var candidate in candidates)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    await using var scope = scopes.CreateAsyncScope();
                    await scope.ServiceProvider.GetRequiredService<PeerSessionService>()
                        .CompleteDueSessionAsync(candidate.LearningRequestId, candidate.Id, ct);
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    logger.LogWarning("Automatic completion failed for session {SessionId} ({ErrorType}).",
                        candidate.Id, ex.GetType().Name);
                }
            }
            cursor = candidates[^1].Id;
        }
    }
}
