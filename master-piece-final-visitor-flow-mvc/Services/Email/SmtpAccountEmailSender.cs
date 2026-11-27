using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using Microsoft.Extensions.Options;
using SkillsSwap.Security;

namespace SkillsSwap.Services.Email;

public sealed class SmtpAccountEmailSender(IOptions<AccountEmailOptions> options,
    IOptions<ConfirmationTokenOptions> confirmation, IOptions<ResetTokenOptions> reset,
    ILogger<SmtpAccountEmailSender> logger) : IAccountEmailSender
{
    public static void AddTemplateViews(MailMessage message, AccountEmailTemplate template)
    {
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(template.Text, Encoding.UTF8, MediaTypeNames.Text.Plain));
        var html = AlternateView.CreateAlternateViewFromString(template.Html, Encoding.UTF8, MediaTypeNames.Text.Html);
        message.AlternateViews.Add(html);
        var logo = typeof(SmtpAccountEmailSender).Assembly.GetManifestResourceStream("SkillsSwap.Email.BrandMark.png")
            ?? throw new InvalidOperationException("Embedded email brand mark is missing.");
        html.LinkedResources.Add(new LinkedResource(logo, MediaTypeNames.Image.Png)
        {
            ContentId = AccountEmailTemplate.LogoContentId,
            TransferEncoding = TransferEncoding.Base64
        });
    }

    public async Task<AccountEmailDeliveryResult> SendAsync(AccountEmailKind kind, string recipient, string actionUrl,
        string language, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.UseSsl || settings.Port is < 1 or > 65535 ||
            Uri.CheckHostName(settings.Host) == UriHostNameType.Unknown ||
            !MailAddress.TryCreate(settings.FromAddress, out _) ||
            string.IsNullOrWhiteSpace(settings.UserName) || string.IsNullOrWhiteSpace(settings.Password))
        {
            logger.LogWarning("Account email delivery is unavailable because SMTP settings are incomplete or TLS is disabled.");
            return new(false);
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        try
        {
            var lifespan = kind == AccountEmailKind.Reset ? reset.Value.TokenLifespan : confirmation.Value.TokenLifespan;
            var template = AccountEmailTemplate.Create(kind, actionUrl, language, lifespan);
            using var message = new MailMessage
            {
                From = new MailAddress(settings.FromAddress, settings.FromName),
                Subject = template.Subject,
                SubjectEncoding = Encoding.UTF8
            };
            message.To.Add(new MailAddress(recipient));
            AddTemplateViews(message, template);

            using var smtp = new SmtpClient(settings.Host, settings.Port)
            {
                EnableSsl = true, DeliveryMethod = SmtpDeliveryMethod.Network, UseDefaultCredentials = false,
                Credentials = new NetworkCredential(settings.UserName, settings.Password)
            };
            await smtp.SendMailAsync(message, timeout.Token);
            return new(true);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Account email delivery timed out.");
            return new(false);
        }
        catch (Exception ex) when (ex is SmtpException or ArgumentException or FormatException or InvalidOperationException)
        {
            logger.LogWarning("Account email delivery failed ({ErrorType}, SMTP status {SmtpStatus}).",
                ex.GetType().Name, (ex as SmtpException)?.StatusCode);
            return new(false);
        }
    }
}
