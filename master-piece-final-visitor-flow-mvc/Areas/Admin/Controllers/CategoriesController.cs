using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillsSwap.Models;
using SkillsSwap.Services;

namespace SkillsSwap.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class CategoriesController(CatalogueService catalogue) : Controller
{
    [HttpGet("/Admin/admin-html/manage-categories.html")]
    public async Task<IActionResult> Index() => View("~/Views/Admin/ManageCategories.cshtml", await catalogue.GetCategoriesAsync());

    [HttpPost("/Admin/categories/save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(CategoryFormViewModel model)
    {
        if (!ModelState.IsValid) return View("~/Views/Admin/ManageCategories.cshtml", await catalogue.GetCategoriesAsync());
        if (!await catalogue.SaveCategoryAsync(model)) return NotFound();
        TempData["AdminMessage"] = model.Id == 0 ? "Category created." : "Category updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/Admin/categories/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        if (!await catalogue.ToggleCategoryAsync(id)) return NotFound();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/Admin/subcategories/save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSubCategory(SubCategoryFormViewModel model)
    {
        if (!ModelState.IsValid) return View("~/Views/Admin/ManageCategories.cshtml", await catalogue.GetCategoriesAsync());
        if (!await catalogue.SaveSubCategoryAsync(model)) return NotFound();
        TempData["AdminMessage"] = model.Id == 0 ? "Subcategory created." : "Subcategory updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/Admin/subcategories/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSubCategory(int id)
    {
        if (!await catalogue.ToggleSubCategoryAsync(id)) return NotFound();
        return RedirectToAction(nameof(Index));
    }
}
