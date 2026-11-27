namespace SkillsSwap.Services.Email;

public sealed class DevelopmentAccountEmailSender : IAccountEmailSender
{
    public Task<AccountEmailDeliveryResult> SendAsync(AccountEmailKind kind, string recipient, string actionUrl,
        string language, CancellationToken cancellationToken = default) =>
        Task.FromResult(new AccountEmailDeliveryResult(false, actionUrl));
}
