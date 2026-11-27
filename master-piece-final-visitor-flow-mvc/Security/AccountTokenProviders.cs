using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SkillsSwap.Models;

namespace SkillsSwap.Security;

public sealed class ConfirmationTokenOptions : DataProtectionTokenProviderOptions
{
    public ConfirmationTokenOptions()
    {
        Name = "DataProtectorTokenProvider";
        TokenLifespan = TimeSpan.FromHours(24);
    }
}

public sealed class ResetTokenOptions : DataProtectionTokenProviderOptions
{
    public ResetTokenOptions()
    {
        Name = "SkillsSwap.PasswordReset.v1";
        TokenLifespan = TimeSpan.FromHours(1);
    }
}

public sealed class ConfirmationTokenProvider(IDataProtectionProvider provider,
    IOptions<ConfirmationTokenOptions> options, ILogger<DataProtectorTokenProvider<ApplicationUser>> logger)
    : DataProtectorTokenProvider<ApplicationUser>(provider, options, logger);

public sealed class ResetTokenProvider(IDataProtectionProvider provider,
    IOptions<ResetTokenOptions> options, ILogger<DataProtectorTokenProvider<ApplicationUser>> logger)
    : DataProtectorTokenProvider<ApplicationUser>(provider, options, logger);
