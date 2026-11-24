using Microsoft.AspNetCore.Mvc;

namespace SkillsSwap.Controllers;

public class StatusController : Controller
{
    [HttpGet("/404.html")]
    public IActionResult NotFoundPage() => View();

    [HttpGet("/500.html")]
    public IActionResult ServerError() => View();

    [HttpGet("/access-denied.html")]
    public IActionResult AccessDenied() => View();

    [HttpGet("/session-expired.html")]
    public IActionResult SessionExpired() => View();
}
