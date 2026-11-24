using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace SkillsSwap.Services.Email;

public sealed class SmtpAccountEmailSender(IOptions<AccountEmailOptions> options, ILogger<SmtpAccountEmailSender> logger)
    : IAccountEmailSender
{
    private readonly AccountEmailOptions settings = options.Value;

    public Task<AccountEmailDeliveryResult> SendConfirmationAsync(string recipient, string confirmationUrl, CancellationToken cancellationToken = default) =>
        SendAsync(recipient, "Confirm your Skills Swap email", "Confirm your email", confirmationUrl, cancellationToken);

    public Task<AccountEmailDeliveryResult> SendPasswordResetAsync(string recipient, string resetUrl, CancellationToken cancellationToken = default) =>
        SendAsync(recipient, "Reset your Skills Swap password", "Reset your password", resetUrl, cancellationToken);

    private async Task<AccountEmailDeliveryResult> SendAsync(string recipient, string subject, string actionText, string actionUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.Host) || string.IsNullOrWhiteSpace(settings.FromAddress))
            return new AccountEmailDeliveryResult(false);

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(settings.FromAddress, settings.FromName),
                Subject = subject,
                IsBodyHtml = true,
                Body = $"<p>{WebUtility.HtmlEncode(actionText)}:</p><p><a href=\"{WebUtility.HtmlEncode(actionUrl)}\">{WebUtility.HtmlEncode(actionText)}</a></p><p>If you did not request this action, ignore this email.</p>"
            };
            message.To.Add(new MailAddress(recipient));

            using var client = new SmtpClient(settings.Host, settings.Port)
            {
                EnableSsl = settings.UseSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = string.IsNullOrWhiteSpace(settings.UserName)
                    ? CredentialCache.DefaultNetworkCredentials
                    : new NetworkCredential(settings.UserName, settings.Password)
            };

            cancellationToken.ThrowIfCancellationRequested();
            await client.SendMailAsync(message, cancellationToken);
            return new AccountEmailDeliveryResult(true);
        }
        catch (Exception exception) when (exception is SmtpException or InvalidOperationException or FormatException)
        {
            logger.LogWarning("Account email delivery failed with provider error {ErrorType}.", exception.GetType().Name);
            return new AccountEmailDeliveryResult(false);
        }
    }
}
