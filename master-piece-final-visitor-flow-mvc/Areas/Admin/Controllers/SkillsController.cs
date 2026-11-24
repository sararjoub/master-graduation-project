using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillsSwap.Models;
using SkillsSwap.Services;

namespace SkillsSwap.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class SkillsController(CatalogueService catalogue) : Controller
{
    [HttpGet("/Admin/admin-html/manage-skills.html")]
    public async Task<IActionResult> Index() => View("~/Views/Admin/ManageSkills.cshtml", await catalogue.GetSkillsAsync());

    [HttpGet("/Admin/admin-html/add-edit-skill.html")]
    public async Task<IActionResult> Edit(int? id)
    {
        var model=await catalogue.GetSkillFormAsync(id);
        return model is null ? NotFound() : View("~/Views/Admin/AddEditSkill.cshtml", model);
    }

    [HttpPost("/Admin/skills/save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(SkillFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await catalogue.FillSkillOptionsAsync(model);
            return View("~/Views/Admin/AddEditSkill.cshtml", model);
        }
        var result=await catalogue.SaveSkillAsync(model);
        if(result=="NotFound") return NotFound();
        if(result=="InvalidSubCategory")
        {
            ModelState.AddModelError(nameof(model.SubCategoryId), "Choose an active subcategory.");
            await catalogue.FillSkillOptionsAsync(model);
            return View("~/Views/Admin/AddEditSkill.cshtml", model);
        }
        TempData["AdminMessage"] = model.Id==0 ? "Skill created." : "Skill updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/Admin/skills/toggle-archive")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleArchive(int id)
    {
        if(!await catalogue.ToggleSkillArchiveAsync(id)) return NotFound();
        return RedirectToAction(nameof(Index));
    }
}
