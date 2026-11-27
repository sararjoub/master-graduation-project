using System.Threading.Channels;

namespace SkillsSwap.Services.Email;

public sealed class QueuedAccountEmailSender(IServiceScopeFactory scopes, ILogger<QueuedAccountEmailSender> logger)
    : BackgroundService, IAccountEmailSender
{
    private sealed record Delivery(AccountEmailKind Kind, string Recipient, string Url, string Language);
    private readonly Channel<Delivery> pending = Channel.CreateBounded<Delivery>(32);

    public Task<AccountEmailDeliveryResult> SendAsync(AccountEmailKind kind, string recipient, string actionUrl,
        string language, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!pending.Writer.TryWrite(new(kind, recipient, actionUrl, language)))
            logger.LogWarning("Account email queue is full. Request another email after a short delay.");
        // A queued message has not yet been accepted by the SMTP server.
        return Task.FromResult(new AccountEmailDeliveryResult(false));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var delivery in pending.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<SmtpAccountEmailSender>()
                    .SendAsync(delivery.Kind, delivery.Recipient, delivery.Url, delivery.Language, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning("Account email worker could not send a queued message ({ErrorType}).", ex.GetType().Name);
            }
        }
    }
}
