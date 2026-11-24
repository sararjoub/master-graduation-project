using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SkillsSwap.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    [AllowAnonymous]
    [HttpGet("/Admin/admin-html/admin-login.html")]
    public IActionResult AdminLogin()
    {
        return Redirect("/login.html");
    }

    [HttpGet("/Admin/admin-html/admin-profile.html")]
    public IActionResult AdminProfile() => View();

    [HttpGet("/Admin/admin-html/skill-saved.html")]
    public IActionResult SkillSaved() => View();

}
