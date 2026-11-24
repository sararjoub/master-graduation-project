namespace SkillsSwap.Services.Email;

public sealed class DevelopmentAccountEmailSender : IAccountEmailSender
{
    public Task<AccountEmailDeliveryResult> SendConfirmationAsync(string recipient, string confirmationUrl, CancellationToken cancellationToken = default) =>
        Task.FromResult(new AccountEmailDeliveryResult(false, confirmationUrl));

    public Task<AccountEmailDeliveryResult> SendPasswordResetAsync(string recipient, string resetUrl, CancellationToken cancellationToken = default) =>
        Task.FromResult(new AccountEmailDeliveryResult(false, resetUrl));
}
