using Microsoft.AspNetCore.Mvc;

namespace SkillsSwap.Controllers;

public class HomeController : Controller
{
    [HttpGet("/")]
    public IActionResult Start() => RedirectToAction(nameof(Index));

    [HttpGet("/home.html")]
    public IActionResult Index() => View();

    [HttpGet("/help.html")]
    public IActionResult Help() => View();

    [HttpGet("/privacy.html")]
    public IActionResult Privacy() => View();

    [HttpGet("/terms.html")]
    public IActionResult Terms() => View();
}
