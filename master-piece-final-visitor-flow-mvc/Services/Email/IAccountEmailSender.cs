namespace SkillsSwap.Services.Email;

public enum AccountEmailKind { Verify, Resend, Reset }

public sealed record AccountEmailDeliveryResult(bool Accepted, string? DevelopmentPreviewUrl = null);

public interface IAccountEmailSender
{
    Task<AccountEmailDeliveryResult> SendAsync(AccountEmailKind kind, string recipient, string actionUrl,
        string language, CancellationToken cancellationToken = default);
}
