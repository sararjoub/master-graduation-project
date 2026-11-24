using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using SkillsSwap.Services;
namespace SkillsSwap.Areas.Admin.Controllers;
[Area("Admin"),Authorize(Roles="Admin")]
public sealed class DashboardController(AdminOperationsService operations):Controller
{
    [HttpGet("/Admin/admin-html/dashboard.html")] public async Task<IActionResult> Index()=>View("~/Views/Admin/Dashboard.cshtml",await operations.GetDashboardAsync());
}
