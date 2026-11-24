using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillsSwap.Models;
using SkillsSwap.Services;

namespace SkillsSwap.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles="Admin")]
public sealed class ExpertApplicationsController(AdminPeopleService people) : Controller
{
    [HttpGet("/Admin/admin-html/expert-applications.html")]
    public async Task<IActionResult> Index(string? status) => View("~/Views/Admin/ExpertApplications.cshtml",await people.GetExpertApplicationsAsync(status));

    [HttpPost("/Admin/expert-applications/decision"),ValidateAntiForgeryToken]
    public async Task<IActionResult> Decide(ExpertDecisionViewModel model)
    {
        if(!ModelState.IsValid) { TempData["AdminError"]="A decision and a reason of at least 10 characters are required."; return RedirectToAction(nameof(Index)); }
        var result=await people.DecideExpertApplicationAsync(model,Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));
        if(result=="NotFound") return NotFound();
        if(result!="Success") return BadRequest();
        TempData["AdminMessage"]="Expert application decision saved.";
        return RedirectToAction(nameof(Index));
    }
}
