using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillsSwap.Security;

namespace SkillsSwap.Controllers;

[Authorize(Policy = SecurityPolicies.CompleteProfile)]
public class CareerController : Controller
{
    [HttpGet("/AI.html")]
    public IActionResult Assistant() => View();

    [HttpGet("/career-path.html")]
    public IActionResult CareerPath() => View();

    [HttpGet("/career-path-preview.html")]
    public IActionResult CareerPathPreview() => View();

    [HttpGet("/career-path-setup.html")]
    public IActionResult CareerPathSetup() => View();
}
