using System.ComponentModel.DataAnnotations;

namespace SkillsSwap.Models;

public sealed class SignupViewModel
{
    [Required, StringLength(120, MinimumLength = 2)] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)] public string Password { get; set; } = string.Empty;
    [Required, DataType(DataType.Password), Compare(nameof(Password))] public string ConfirmPassword { get; set; } = string.Empty;
    [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the terms and privacy policy.")]
    public bool AcceptTerms { get; set; }
}

public sealed class LoginViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

public sealed class ForgotPasswordViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? Message { get; set; }
    public string? DevelopmentResetUrl { get; set; }
}

public sealed class ResetPasswordViewModel
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string Code { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class ProfileSetupViewModel
{
    [Required, StringLength(120, MinimumLength = 2)] public string FullName { get; set; } = string.Empty;
    [StringLength(120)] public string? Location { get; set; }
    [StringLength(1000)] public string? Bio { get; set; }
    public List<int> TeachSkillIds { get; set; } = [];
    [MinLength(1, ErrorMessage = "Choose at least one skill you want to learn.")]
    public List<int> LearnSkillIds { get; set; } = [];
    public IReadOnlyList<SkillOptionViewModel> Skills { get; set; } = [];
}

public sealed record SkillOptionViewModel(int Id, string Name, string Category, AccessTier AccessTier);

public sealed class VerifyEmailViewModel
{
    public string Email { get; set; } = string.Empty;
    public string? DevelopmentConfirmationUrl { get; set; }
    public string? Message { get; set; }
}
