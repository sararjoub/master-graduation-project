namespace SkillsSwap.Services.Email;

public sealed record AccountEmailDeliveryResult(bool Accepted, string? DevelopmentPreviewUrl = null);

public interface IAccountEmailSender
{
    Task<AccountEmailDeliveryResult> SendConfirmationAsync(string recipient, string confirmationUrl, CancellationToken cancellationToken = default);
    Task<AccountEmailDeliveryResult> SendPasswordResetAsync(string recipient, string resetUrl, CancellationToken cancellationToken = default);
}
