using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillsSwap.Security;

namespace SkillsSwap.Controllers;

[Authorize(Policy = SecurityPolicies.CompleteProfile)]
public class MembershipController : Controller
{
    [HttpGet("/pricing.html")]
    [AllowAnonymous]
    public IActionResult Pricing() => View();

    [HttpGet("/subscription.html")]
    public IActionResult Subscription() => View();

    [HttpGet("/subscription-checkout.html")]
    public IActionResult SubscriptionCheckout() => View();
}
