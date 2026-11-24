using Microsoft.EntityFrameworkCore;
using SkillsSwap.Data;
using SkillsSwap.Models;

namespace SkillsSwap.Services;

public sealed class CatalogueService(ApplicationDbContext db)
{
    public async Task<ManageCategoriesViewModel> GetCategoriesAsync() => new()
    {
        TotalSkills = await db.Skills.CountAsync(),
        Categories = await db.Categories.AsNoTracking().OrderByDescending(x => x.Id)
            .Select(x => new AdminCategoryViewModel
            {
                Id = x.Id, NameEn = x.NameEn, NameAr = x.NameAr, Slug = x.Slug,
                Description = x.Description, IsActive = x.IsActive,
                SubCategories = x.SubCategories.OrderByDescending(s => s.Id)
                    .Select(s => new AdminSubCategoryViewModel(s.Id, s.NameEn, s.NameAr, s.Slug,
                        s.IsActive, db.Skills.Count(skill => skill.SubCategoryId == s.Id))).ToList()
            }).ToListAsync()
    };

    public async Task<bool> SaveCategoryAsync(CategoryFormViewModel form)
    {
        var item = form.Id == 0 ? new Category() : await db.Categories.FindAsync(form.Id);
        if (item is null) return false;
        item.NameEn = form.NameEn.Trim();
        if (form.Id == 0) { item.NameAr = item.NameEn; item.Slug = await UniqueCategorySlugAsync(item.NameEn); db.Categories.Add(item); }
        item.Description = form.Description?.Trim();
        await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> ToggleCategoryAsync(int id)
    {
        var item = await db.Categories.FindAsync(id); if (item is null) return false;
        item.IsActive = !item.IsActive; await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> SaveSubCategoryAsync(SubCategoryFormViewModel form)
    {
        if (!await db.Categories.AnyAsync(x => x.Id == form.CategoryId)) return false;
        var item = form.Id == 0 ? new SubCategory() : await db.SubCategories.FindAsync(form.Id);
        if (item is null) return false;
        item.CategoryId = form.CategoryId; item.NameEn = form.NameEn.Trim();
        if (form.Id == 0) { item.NameAr = item.NameEn; item.Slug = await UniqueSubCategorySlugAsync(form.CategoryId, item.NameEn); db.SubCategories.Add(item); }
        await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> ToggleSubCategoryAsync(int id)
    {
        var item = await db.SubCategories.FindAsync(id); if (item is null) return false;
        item.IsActive = !item.IsActive; await db.SaveChangesAsync(); return true;
    }

    public async Task<ManageSkillsViewModel> GetSkillsAsync() => new()
    {
        Skills = await db.Skills.AsNoTracking().OrderByDescending(x => x.Id)
            .Select(x => new AdminSkillRowViewModel(x.Id, x.NameEn, x.Description, x.Slug,
                x.SubCategory.Category.NameEn, x.SubCategory.NameEn, x.Level, x.AccessTier,
                x.Status, db.UserSkills.Count(u => u.SkillId == x.Id))).ToListAsync()
    };

    public async Task<SkillFormViewModel?> GetSkillFormAsync(int? id)
    {
        var model = id.HasValue
            ? await db.Skills.AsNoTracking().Where(x => x.Id == id).Select(x => new SkillFormViewModel
                { Id=x.Id, NameEn=x.NameEn, Slug=x.Slug, SubCategoryId=x.SubCategoryId,
                  Description=x.Description, Level=x.Level, AccessTier=x.AccessTier, Status=x.Status }).SingleOrDefaultAsync()
            : new SkillFormViewModel();
        if (model is not null) model.SubCategories = await GetSubCategoryOptionsAsync();
        return model;
    }

    public async Task FillSkillOptionsAsync(SkillFormViewModel model) =>
        model.SubCategories = await GetSubCategoryOptionsAsync();

    public async Task<string> SaveSkillAsync(SkillFormViewModel model)
    {
        if (!await db.SubCategories.AnyAsync(x => x.Id == model.SubCategoryId && x.IsActive && x.Category.IsActive)) return "InvalidSubCategory";
        var item = model.Id == 0 ? new Skill() : await db.Skills.FindAsync(model.Id);
        if (item is null) return "NotFound";
        item.NameEn=model.NameEn.Trim(); item.SubCategoryId=model.SubCategoryId; item.Description=model.Description.Trim();
        item.Level=model.Level; item.AccessTier=model.AccessTier; item.Status=model.Status;
        if(model.Id==0) { item.NameAr=item.NameEn; item.Slug=await UniqueSkillSlugAsync(item.NameEn); db.Skills.Add(item); }
        await db.SaveChangesAsync(); return "Success";
    }

    public async Task<bool> ToggleSkillArchiveAsync(int id)
    {
        var item=await db.Skills.FindAsync(id); if(item is null) return false;
        item.Status=item.Status==PublicationStatus.Archived ? PublicationStatus.Published : PublicationStatus.Archived;
        await db.SaveChangesAsync(); return true;
    }

    private async Task<IReadOnlyList<SkillSubCategoryOptionViewModel>> GetSubCategoryOptionsAsync() =>
        await db.SubCategories.AsNoTracking().Where(x=>x.IsActive && x.Category.IsActive)
            .OrderBy(x=>x.Category.NameEn).ThenBy(x=>x.NameEn)
            .Select(x=>new SkillSubCategoryOptionViewModel(x.Id,x.Category.NameEn,x.NameEn)).ToListAsync();

    private Task<string> UniqueCategorySlugAsync(string name) => UniqueSlugAsync(name, slug => db.Categories.AnyAsync(x => x.Slug == slug));
    private Task<string> UniqueSubCategorySlugAsync(int categoryId, string name) => UniqueSlugAsync(name, slug => db.SubCategories.AnyAsync(x => x.CategoryId == categoryId && x.Slug == slug));
    private Task<string> UniqueSkillSlugAsync(string name) => UniqueSlugAsync(name, slug => db.Skills.AnyAsync(x => x.Slug == slug));
    private static async Task<string> UniqueSlugAsync(string name, Func<string, Task<bool>> exists)
    {
        var root=System.Text.RegularExpressions.Regex.Replace(name.Trim().ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        if(string.IsNullOrWhiteSpace(root)) root="item"; var value=root; var suffix=2;
        while(await exists(value)) value=$"{root}-{suffix++}"; return value;
    }
}
