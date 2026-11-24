namespace SkillsSwap.Services.Email;

public sealed class AccountEmailOptions
{
    public const string SectionName = "AccountEmail";
    public bool EnableDevelopmentPreview { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Skills Swap";
}
