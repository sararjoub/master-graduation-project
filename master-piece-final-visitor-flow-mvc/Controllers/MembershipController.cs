using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SkillsSwap.Models;
using SkillsSwap.Security;
using SkillsSwap.Services;

namespace SkillsSwap.Controllers;

[Authorize(Policy = SecurityPolicies.CompleteProfile)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class MembershipController(SubscriptionPayments payments, IAuthorizationService authorization, TimeProvider clock) : Controller
{
    private Guid MemberId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("/pricing.html"), AllowAnonymous]
    public async Task<IActionResult> Pricing(BillingPeriod period = BillingPeriod.Monthly, CancellationToken ct = default)
    {
        if (!ModelState.IsValid || !Enum.IsDefined(period)) return BadRequest();
        var eligible = User.IsInRole("Member") && !User.IsInRole("Admin") &&
            (await authorization.AuthorizeAsync(User, SecurityPolicies.CompleteProfile)).Succeeded;
        ViewData["EligibleMember"] = eligible;
        return View(await payments.PageAsync(eligible ? MemberId : null, period, ct));
    }

    [HttpGet("/subscription.html")]
    public async Task<IActionResult> Subscription(CancellationToken ct) => User.IsInRole("Admin") ? Forbid() :
        View(await payments.PageAsync(MemberId, BillingPeriod.Monthly, ct));

    [HttpGet("/subscription-checkout.html")]
    public async Task<IActionResult> SubscriptionCheckout(BillingPeriod period = BillingPeriod.Monthly, CancellationToken ct = default)
    {
        if (User.IsInRole("Admin")) return Forbid();
        if (!ModelState.IsValid) return BadRequest();
        var checkout = await payments.CheckoutAsync(MemberId, period, ct);
        return checkout is null ? NotFound() : View(checkout);
    }

    [HttpPost("/subscription-checkout/quote"), ValidateAntiForgeryToken, EnableRateLimiting("subscription-payment")]
    public async Task<IActionResult> Quote(BillingPeriod period, CancellationToken ct)
    {
        if (User.IsInRole("Admin")) return Forbid();
        if (!ModelState.IsValid) return BadRequest();
        var payment = await payments.QuoteAsync(MemberId, period, ct);
        return payment is null ? BadRequest() : RedirectToAction(nameof(Payment), new { reference = payment.ProviderReference });
    }

    [HttpGet("/subscription-payment.html")]
    public async Task<IActionResult> Payment(string reference, CancellationToken ct)
    {
        if (User.IsInRole("Admin")) return Forbid();
        var payment = await payments.FindAsync(MemberId, reference, ct);
        ViewData["DemoEnabled"] = payments.Enabled;
        ViewData["StartsLater"] = payment?.Subscription.StartsAtUtc > clock.GetUtcNow().UtcDateTime;
        return payment is null ? NotFound() : View(payment);
    }

    [HttpPost("/subscription-payment/process"), ValidateAntiForgeryToken, EnableRateLimiting("subscription-payment")]
    public async Task<IActionResult> Process(string reference, SimulatedOutcome outcome, CancellationToken ct)
    {
        if (User.IsInRole("Admin")) return Forbid();
        if (!ModelState.IsValid || !await payments.ProcessAsync(MemberId, reference, outcome, ct)) return BadRequest();
        return RedirectToAction(nameof(Payment), new { reference });
    }
}
