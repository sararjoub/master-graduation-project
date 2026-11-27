using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillsSwap.Data;
using SkillsSwap.Models;
using SkillsSwap.Security;
using SkillsSwap.Services;

namespace SkillsSwap.Controllers;

[Authorize(Policy = SecurityPolicies.CompleteProfile)]
public class SkillsController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    SkillDiscoveryService discoveryService, PlusEntitlement entitlement) : Controller
{
    [HttpGet("/categories.html")]
    [AllowAnonymous]
    public async Task<IActionResult> Categories()
    {
        ApplicationUser? currentUser = null;
        if (User.Identity?.IsAuthenticated == true)
            currentUser = await userManager.GetUserAsync(User);

        var categories = await db.Categories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.NameEn)
            .Select(category => new CategoryCardViewModel(
                category.Id,
                category.NameEn,
                category.Slug,
                category.Description,
                category.SubCategories.Count(subCategory => subCategory.IsActive),
                db.Skills.Count(skill =>
                    skill.SubCategory.CategoryId == category.Id &&
                    skill.SubCategory.IsActive &&
                    skill.Status == PublicationStatus.Published)))
            .ToListAsync();

        return View(new CategoriesPageViewModel
        {
            IsAuthenticated = currentUser is not null,
            CurrentUserFullName = currentUser?.FullName,
            CurrentUserEmail = currentUser?.Email,
            CurrentUserInitials = NameFormatting.Initials(currentUser?.FullName),
            Categories = categories
        });
    }

    [HttpGet("/category-overview.html")]
    [AllowAnonymous]
    public async Task<IActionResult> CategoryOverview(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return NotFound();

        var model = await db.Categories.AsNoTracking()
            .Where(category => category.IsActive && category.Slug == slug)
            .Select(category => new CategoryOverviewViewModel
            {
                Name = category.NameEn,
                Slug = category.Slug,
                Description = category.Description,
                SubCategories = category.SubCategories
                    .Where(subCategory => subCategory.IsActive)
                    .OrderBy(subCategory => subCategory.NameEn)
                    .Select(subCategory => new SubCategoryCardViewModel(
                        subCategory.Id,
                        subCategory.NameEn,
                        subCategory.Slug,
                        db.Skills.Count(skill => skill.SubCategoryId == subCategory.Id &&
                            skill.Status == PublicationStatus.Published)))
                    .ToList()
            })
            .SingleOrDefaultAsync();

        return model is null ? NotFound() : View(model);
    }

    [HttpGet("/technology-category.html")]
    [AllowAnonymous]
    public IActionResult TechnologyCategory() => View();

    [HttpGet("/sub-category.html")]
    [AllowAnonymous]
    public async Task<IActionResult> SubCategory(string? slug, string? cat)
    {
        slug = NormalizeLegacySubCategorySlug(slug ?? cat);
        if (string.IsNullOrWhiteSpace(slug)) return NotFound();

        var model = await db.SubCategories.AsNoTracking()
            .Where(subCategory => subCategory.IsActive && subCategory.Category.IsActive && subCategory.Slug == slug)
            .Select(subCategory => new SubCategoryPageViewModel
            {
                CategoryName = subCategory.Category.NameEn,
                CategorySlug = subCategory.Category.Slug,
                Name = subCategory.NameEn,
                Slug = subCategory.Slug,
                Skills = db.Skills
                    .Where(skill => skill.SubCategoryId == subCategory.Id && skill.Status == PublicationStatus.Published)
                    .OrderBy(skill => skill.NameEn)
                    .Select(skill => new SkillCardViewModel(
                        skill.Id, skill.NameEn, skill.Slug, skill.Description, skill.Level, skill.AccessTier))
                    .ToList()
            })
            .SingleOrDefaultAsync();

        return model is null ? NotFound() : View(model);
    }

    private static string? NormalizeLegacySubCategorySlug(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var slug = value.Trim().ToLowerInvariant();
        return slug switch
        {
            "mobile-development" or "cybersecurity" => "mobile-cloud-security",
            "mathematics" or "physics" or "chemistry" => "academic-skills",
            "english" or "arabic" => "languages",
            "painting" or "knitting" => "visual-arts",
            "crafts" or "diy" => "handmade",
            "video-editing" or "photography" => "photo-video",
            "marketing" or "sales" => "marketing-sales",
            "management" or "entrepreneurship" => "management-finance",
            "yoga" or "personal-training" => "fitness",
            "nutrition" => "wellness",
            _ => slug
        };
    }

    [HttpGet("/skill-details.html")]
    [AllowAnonymous]
    public async Task<IActionResult> SkillDetails(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return NotFound();

        var model = await db.Skills.AsNoTracking()
            .Where(skill => skill.Status == PublicationStatus.Published &&
                skill.SubCategory.IsActive && skill.SubCategory.Category.IsActive && skill.Slug == slug)
            .Select(skill => new SkillDetailsViewModel
            {
                Id = skill.Id,
                Name = skill.NameEn,
                Slug = skill.Slug,
                Description = skill.Description,
                Level = skill.Level,
                AccessTier = skill.AccessTier,
                CategoryName = skill.SubCategory.Category.NameEn,
                CategorySlug = skill.SubCategory.Category.Slug,
                SubCategoryName = skill.SubCategory.NameEn,
                SubCategorySlug = skill.SubCategory.Slug,
                RelatedSkills = db.Skills
                    .Where(related => related.SubCategoryId == skill.SubCategoryId &&
                        related.Id != skill.Id && related.Status == PublicationStatus.Published)
                    .OrderBy(related => related.NameEn)
                    .Take(5)
                    .Select(related => new RelatedSkillViewModel(related.NameEn, related.Slug))
                    .ToList()
            })
            .SingleOrDefaultAsync();

        if (model is null) return NotFound();
        model.IsAuthenticated = User.Identity?.IsAuthenticated == true;
        if (model.IsAuthenticated && User.IsInRole("Member") && !User.IsInRole("Admin") &&
            Guid.TryParse(userManager.GetUserId(User), out var member))
            model.HasPlus = await entitlement.HasAccessAsync(member, HttpContext.RequestAborted);
        return View(model);
    }

    [HttpGet("/skill-partners.html")]
    public async Task<IActionResult> SkillPartners(string? skillSlug, [FromQuery] SkillDiscoveryQuery query,
        CancellationToken cancellationToken)
    {
        if (User.IsInRole("Admin")) return Forbid();
        if (string.IsNullOrWhiteSpace(skillSlug)) return NotFound();
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var model = await discoveryService.DiscoverAsync(skillSlug.Trim(), user.Id, query, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpGet("/my-skills.html")]
    public IActionResult MySkills() => View();

    [HttpGet("/partner-profile.html")]
    public IActionResult PartnerProfile() => View();

}
