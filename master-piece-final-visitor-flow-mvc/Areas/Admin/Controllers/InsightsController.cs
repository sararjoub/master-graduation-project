using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using SkillsSwap.Services;
namespace SkillsSwap.Areas.Admin.Controllers;
[Area("Admin"),Authorize(Roles="Admin")]
public sealed class InsightsController(AdminOperationsService operations):Controller
{
    [HttpGet("/Admin/admin-html/manage-reviews.html")] public async Task<IActionResult> Reviews()=>View("~/Views/Admin/ManageReviews.cshtml",await operations.GetReviewsAsync());
    [HttpGet("/Admin/admin-html/reports.html")] public async Task<IActionResult> Analytics()=>View("~/Views/Admin/Reports.cshtml",await operations.GetAnalyticsAsync());
}
