using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace SkillsSwap.Services.Email;

public sealed class AccountEmailLinks(IOptions<AccountEmailOptions> configuration, IHostEnvironment environment)
{
    public string? Build(bool passwordReset, Guid userId, string code, string language)
    {
        if (!TryGetTrustedBase(configuration.Value.PublicBaseUrl, environment.IsDevelopment(), out var origin))
            return null;
        var path = passwordReset ? "/reset-password.html" : "/account/confirm-email";
        return QueryHelpers.AddQueryString(origin.AbsoluteUri.TrimEnd('/') + path, new Dictionary<string, string?>
        {
            ["userId"] = userId.ToString(), ["code"] = code, ["lang"] = Language(language)
        });
    }

    public static string Language(string? language) => string.Equals(language, "ar", StringComparison.OrdinalIgnoreCase) ? "ar" : "en";

    public static bool TryGetTrustedBase(string? setting, bool development, out Uri origin)
    {
        origin = null!;
        if (setting is null || !Uri.TryCreate(setting, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) ||
            uri.Host.Length == 0 || (uri.IsLoopback && !development))
            return false;

        origin = uri;
        return true;
    }
}
