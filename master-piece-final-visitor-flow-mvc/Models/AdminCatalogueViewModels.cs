using System.ComponentModel.DataAnnotations;

namespace SkillsSwap.Models;

public sealed class ManageCategoriesViewModel
{
    public IReadOnlyList<AdminCategoryViewModel> Categories { get; set; } = [];
    public int TotalSkills { get; set; }
    public int ActiveCategories => Categories.Count(x => x.IsActive);
    public int InactiveCategories => Categories.Count - ActiveCategories;
    public int TotalSubCategories => Categories.Sum(x => x.SubCategories.Count);
}

public sealed class AdminCategoryViewModel
{
    public int Id { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public IReadOnlyList<AdminSubCategoryViewModel> SubCategories { get; set; } = [];
}

public sealed record AdminSubCategoryViewModel(int Id, string NameEn, string NameAr, string Slug, bool IsActive, int SkillCount);

public sealed class CategoryFormViewModel
{
    public int Id { get; set; }
    [Required, StringLength(80)] public string NameEn { get; set; } = string.Empty;
    [StringLength(80)] public string? NameAr { get; set; }
    public string? Slug { get; set; }
    [StringLength(500)] public string? Description { get; set; }
}

public sealed class SubCategoryFormViewModel
{
    public int Id { get; set; }
    [Range(1, int.MaxValue)] public int CategoryId { get; set; }
    [Required, StringLength(80)] public string NameEn { get; set; } = string.Empty;
    [StringLength(80)] public string? NameAr { get; set; }
    public string? Slug { get; set; }
}

public sealed class ManageSkillsViewModel
{
    public IReadOnlyList<AdminSkillRowViewModel> Skills { get; set; } = [];
    public int Total => Skills.Count;
    public int Active => Skills.Count(x => x.Status == PublicationStatus.Published);
    public int Archived => Skills.Count(x => x.Status == PublicationStatus.Archived);
    public int Categories => Skills.Select(x => x.CategoryName).Distinct().Count();
}

public sealed record AdminSkillRowViewModel(int Id, string Name, string Description, string Slug,
    string CategoryName, string SubCategoryName, SkillLevel Level, AccessTier AccessTier,
    PublicationStatus Status, int UserCount);

public sealed class SkillFormViewModel
{
    public int Id { get; set; }
    [Required, StringLength(120)] public string NameEn { get; set; } = string.Empty;
    public string? Slug { get; set; }
    [Range(1, int.MaxValue)] public int SubCategoryId { get; set; }
    [Required, StringLength(1500, MinimumLength = 10)] public string Description { get; set; } = string.Empty;
    public SkillLevel Level { get; set; }
    public AccessTier AccessTier { get; set; }
    public PublicationStatus Status { get; set; } = PublicationStatus.Published;
    public IReadOnlyList<SkillSubCategoryOptionViewModel> SubCategories { get; set; } = [];
}

public sealed record SkillSubCategoryOptionViewModel(int Id, string CategoryName, string Name);
