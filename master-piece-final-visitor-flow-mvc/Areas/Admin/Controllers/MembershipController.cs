using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using SkillsSwap.Models; using SkillsSwap.Services;
namespace SkillsSwap.Areas.Admin.Controllers;
[Area("Admin"),Authorize(Roles="Admin")]
public sealed class MembershipController(AdminOperationsService operations):Controller
{
    [HttpGet("/Admin/admin-html/plans.html")] public async Task<IActionResult> Plans()=>View("~/Views/Admin/Plans.cshtml",await operations.GetPlansAsync());
    [HttpPost("/Admin/plans/save"),ValidateAntiForgeryToken] public async Task<IActionResult> SavePlan(AdminPlanFormViewModel model){if(!ModelState.IsValid)return RedirectToAction(nameof(Plans));if(!await operations.SavePlanAsync(model))return NotFound();TempData["AdminMessage"]="Plan updated.";return RedirectToAction(nameof(Plans));}
    [HttpGet("/Admin/admin-html/manage-subscriptions.html")] public async Task<IActionResult> Subscriptions()=>View("~/Views/Admin/ManageSubscriptions.cshtml",await operations.GetSubscriptionsAsync());
    [HttpPost("/Admin/subscriptions/cancel"),ValidateAntiForgeryToken] public async Task<IActionResult> Cancel(int id){if(!await operations.CancelSubscriptionAsync(id))return NotFound();TempData["AdminMessage"]="Subscription cancelled; its financial history was preserved.";return RedirectToAction(nameof(Subscriptions));}
    [HttpGet("/Admin/admin-html/transactions.html")] public async Task<IActionResult> Transactions()=>View("~/Views/Admin/Transactions.cshtml",await operations.GetPaymentsAsync());
}
