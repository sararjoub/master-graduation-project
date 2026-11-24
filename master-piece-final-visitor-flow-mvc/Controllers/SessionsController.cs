using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillsSwap.Security;

namespace SkillsSwap.Controllers;

[Authorize(Policy = SecurityPolicies.CompleteProfile)]
public class SessionsController : Controller
{
    [HttpGet("/chat.html")]
    public IActionResult Chat() => View();

    [HttpGet("/expert-booking.html")]
    public IActionResult ExpertBooking() => View();

    [HttpGet("/expert-workspace.html")]
    public IActionResult ExpertWorkspace() => View();

    [HttpGet("/session-details.html")]
    public IActionResult SessionDetails() => View();

    [HttpGet("/session-review.html")]
    public IActionResult SessionReview() => View();

    [HttpGet("/sessions.html")]
    public IActionResult Sessions() => View();

    [HttpGet("/zoom-meeting.html")]
    public IActionResult ZoomMeeting() => View();
}
