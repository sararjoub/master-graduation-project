using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkillsSwap.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SkillsSwap.Controllers;
using SkillsSwap.Models;
using SkillsSwap.Security;
using SkillsSwap.Services.Email;

namespace SkillsSwap.Tests;

public sealed class Phase061AccountEmailTests
{
    [Fact]
    public void Links_use_configured_https_base_and_allowlisted_language()
    {
        var links = new AccountEmailLinks(Options.Create(new AccountEmailOptions
            { PublicBaseUrl = "https://localhost:7186" }), new TestHost("Development"));
        var url = links.Build(false, Guid.Empty, "a?b&c", "ar");
        Assert.StartsWith("https://localhost:7186/account/confirm-email?", url);
        Assert.Contains("code=a%3Fb%26c", url);
        Assert.EndsWith("lang=ar", url);
        Assert.EndsWith("lang=en", links.Build(true, Guid.Empty, "x", "en-US<script>"));
    }

    [Theory]
    [InlineData("http://skillswap.example", false)]
    [InlineData("https://localhost:7186", false)]
    [InlineData("https://user@skillswap.example", false)]
    [InlineData("https://skillswap.example?x=y", false)]
    [InlineData("https://skillswap.example/#section", false)]
    [InlineData("https://skillswap.example", true)]
    public void Public_base_is_validated(string value, bool expected) =>
        Assert.Equal(expected, AccountEmailLinks.TryGetTrustedBase(value, false, out _));

    [Fact]
    public void Password_reset_and_confirmation_have_independent_lifetimes_and_purposes()
    {
        var dataProtection = new EphemeralDataProtectionProvider();
        var confirmation = new ConfirmationTokenOptions();
        var reset = new ResetTokenOptions();
        Assert.Equal(TimeSpan.FromHours(24), confirmation.TokenLifespan);
        Assert.Equal(TimeSpan.FromHours(1), reset.TokenLifespan);
        Assert.Equal("DataProtectorTokenProvider", confirmation.Name);
        Assert.NotEqual(confirmation.Name, reset.Name);
        Assert.NotEqual(dataProtection.CreateProtector(confirmation.Name).Protect("test"),
            dataProtection.CreateProtector(reset.Name).Protect("test"));
    }

    [Theory]
    [InlineData(AccountEmailKind.Verify, "en", "24 hours", "ltr")]
    [InlineData(AccountEmailKind.Verify, "ar", "24 ساعة", "rtl")]
    [InlineData(AccountEmailKind.Resend, "en", "24 hours", "ltr")]
    [InlineData(AccountEmailKind.Resend, "ar", "24 ساعة", "rtl")]
    [InlineData(AccountEmailKind.Reset, "en", "1 hour", "ltr")]
    [InlineData(AccountEmailKind.Reset, "ar", "ساعة واحدة", "rtl")]
    public void Templates_include_encoded_link_and_lifetime(AccountEmailKind kind, string lang, string lifetime, string direction)
    {
        const string link = "https://skillswap.example/action?a=b&code=123";
        var template = AccountEmailTemplate.Create(kind, link, lang,
            kind == AccountEmailKind.Reset ? TimeSpan.FromHours(1) : TimeSpan.FromHours(24));
        Assert.Contains($"dir=\"{direction}\"", template.Html);
        Assert.Contains("a=b&amp;code=123", template.Html);
        Assert.Contains(lifetime, template.Html);
        Assert.Contains(link, template.Text);
        var expectedHref = $"href=\"{System.Net.WebUtility.HtmlEncode(link)}\"";
        Assert.Equal(2, template.Html.Split(expectedHref, StringSplitOptions.None).Length - 1);
        Assert.DoesNotContain(">https://example.invalid", template.Html);
        Assert.Contains($"src=\"cid:{AccountEmailTemplate.LogoContentId}\"", template.Html);
        Assert.DoesNotContain("localhost", template.Html);
        Assert.DoesNotContain("<script", template.Html);
    }

    [Fact]
    public void Mime_message_contains_text_and_html_with_the_official_embedded_png()
    {
        using var message = new System.Net.Mail.MailMessage();
        var template = AccountEmailTemplate.Create(AccountEmailKind.Verify, "https://example.invalid", "en", TimeSpan.FromMinutes(90));
        Assert.Contains("90 minutes", template.Html);
        SmtpAccountEmailSender.AddTemplateViews(message, template);
        Assert.Equal(2, message.AlternateViews.Count);
        Assert.Equal("text/plain", message.AlternateViews[0].ContentType.MediaType);
        Assert.Equal("text/html", message.AlternateViews[1].ContentType.MediaType);
        var logo = Assert.IsType<System.Net.Mail.LinkedResource>(Assert.Single(message.AlternateViews[1].LinkedResources));
        Assert.Equal("image/png", logo.ContentType.MediaType);
        Assert.Equal(AccountEmailTemplate.LogoContentId, logo.ContentId);
        Assert.Empty(message.Attachments);

        using var bytes = new MemoryStream();
        logo.ContentStream.CopyTo(bytes);
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, bytes.ToArray()[..8]);
        Assert.InRange(bytes.Length, 100, 20_000);
    }

    [Fact]
    public async Task Invalid_smtp_settings_fail_without_disclosing_a_preview_token()
    {
        var sender = new SmtpAccountEmailSender(Options.Create(new AccountEmailOptions { UseSsl = false }),
            Options.Create(new ConfirmationTokenOptions()), Options.Create(new ResetTokenOptions()),
            NullLogger<SmtpAccountEmailSender>.Instance);
        var result = await sender.SendAsync(AccountEmailKind.Verify, "test@example.invalid", "https://example.invalid", "en");
        Assert.False(result.Accepted);
        Assert.Null(result.DevelopmentPreviewUrl);
    }

    [Fact]
    public void Resend_cooldown_is_shared_and_does_not_block_the_separate_recovery_flow()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var cooldown = new AccountEmailCooldown(memory);
        Assert.True(cooldown.TryBegin("verification", "USER@EXAMPLE.INVALID"));
        Assert.False(cooldown.TryBegin("verification", "USER@EXAMPLE.INVALID"));
        Assert.True(cooldown.TryBegin("recovery", "USER@EXAMPLE.INVALID"));
    }

    [Theory]
    [InlineData(nameof(AccountController.ResetPassword), typeof(ResetPasswordViewModel))]
    [InlineData(nameof(AccountController.ChangePassword), typeof(ChangePasswordViewModel))]
    public void Password_writes_use_antiforgery_and_rate_limiting(string name, Type model)
    {
        var method = typeof(AccountController).GetMethod(name, [model])!;
        Assert.Single(method.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.ValidateAntiForgeryTokenAttribute), true));
        Assert.Single(method.GetCustomAttributes(typeof(EnableRateLimitingAttribute), true));
    }

    [Theory]
    [InlineData("Production", true, false)]
    [InlineData("Development", false, false)]
    [InlineData("Development", true, true)]
    public void Preview_requires_development_and_an_explicit_flag(string environment, bool enabled, bool expected) =>
        Assert.Equal(expected, AccountEmailOptions.PreviewAllowed(new TestHost(environment), enabled));

    [Fact]
    public async Task Identity_providers_reject_expired_tokens_for_both_purposes()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddIdentityCore<ApplicationUser>().AddEntityFrameworkStores<ApplicationDbContext>();
        await using var service = services.BuildServiceProvider();
        using var scope = service.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dp = new EphemeralDataProtectionProvider();
        var logger = NullLogger<DataProtectorTokenProvider<ApplicationUser>>.Instance;
        var user = new ApplicationUser { Id = Guid.NewGuid(), SecurityStamp = Guid.NewGuid().ToString() };
        var confirm = new ConfirmationTokenProvider(dp, Options.Create(new ConfirmationTokenOptions()), logger);
        var expiredConfirm = new ConfirmationTokenProvider(dp,
            Options.Create(new ConfirmationTokenOptions { TokenLifespan = TimeSpan.FromMinutes(-1) }), logger);
        const string emailPurpose = UserManager<ApplicationUser>.ConfirmEmailTokenPurpose;
        var confirmation = await confirm.GenerateAsync(emailPurpose, manager, user);
        Assert.True(await confirm.ValidateAsync(emailPurpose, confirmation, manager, user));
        Assert.False(await expiredConfirm.ValidateAsync(emailPurpose, confirmation, manager, user));

        var reset = new ResetTokenProvider(dp, Options.Create(new ResetTokenOptions()), logger);
        var expiredReset = new ResetTokenProvider(dp, Options.Create(new ResetTokenOptions
            { TokenLifespan = TimeSpan.FromMinutes(-1) }), logger);
        const string resetPurpose = UserManager<ApplicationUser>.ResetPasswordTokenPurpose;
        var recovery = await reset.GenerateAsync(resetPurpose, manager, user);
        Assert.True(await reset.ValidateAsync(resetPurpose, recovery, manager, user));
        Assert.False(await expiredReset.ValidateAsync(resetPurpose, recovery, manager, user));
        Assert.False(await confirm.ValidateAsync(emailPurpose, recovery, manager, user));
    }

    private sealed class TestHost(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "SkillsSwap";
        public string ContentRootPath { get; set; } = ".";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
