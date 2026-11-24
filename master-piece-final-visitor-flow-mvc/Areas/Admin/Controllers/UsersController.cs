using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillsSwap.Models;
using SkillsSwap.Services;

namespace SkillsSwap.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles="Admin")]
public sealed class UsersController(AdminPeopleService people) : Controller
{
    [HttpGet("/Admin/admin-html/manage-users.html")]
    public async Task<IActionResult> Index(string? search,string? filter,int page=1) => View("~/Views/Admin/ManageUsers.cshtml",await people.GetUsersAsync(search,filter,page));

    [HttpGet("/Admin/admin-html/user-details.html")]
    public async Task<IActionResult> Details(Guid id) { var model=await people.GetUserAsync(id); return model is null ? NotFound() : View("~/Views/Admin/UserDetails.cshtml",model); }

    [HttpGet("/Admin/admin-html/edit-user.html")]
    public async Task<IActionResult> Edit(Guid id) { var model=await people.GetStatusFormAsync(id); return model is null ? NotFound() : View("~/Views/Admin/EditUser.cshtml",model); }

    [HttpPost("/Admin/users/status"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(AdminUserStatusFormViewModel model)
    {
        if(!ModelState.IsValid) return View("~/Views/Admin/EditUser.cshtml",model);
        if(!await people.ChangeStatusAsync(model)) return NotFound();
        TempData["AdminMessage"]="Account status updated.";
        return RedirectToAction(nameof(Details),new { id=model.Id });
    }
}
