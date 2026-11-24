namespace SkillsSwap.Security;

public sealed class AccountRateLimitOptions
{
    public const string SectionName = "AccountRateLimits";
    public int SensitivePermitLimit { get; set; } = 5;
    public int RegistrationPermitLimit { get; set; } = 3;
    public int WindowMinutes { get; set; } = 15;
}
