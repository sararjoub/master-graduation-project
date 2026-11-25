using System.Data.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillsSwap.Models;
using SkillsSwap.Security;
using SkillsSwap.Services;

namespace SkillsSwap.Controllers;

[Authorize(Policy = SecurityPolicies.CompleteProfile)]
public sealed class PeerRequestsController(PeerRequestService requests, UserManager<ApplicationUser> users,
    IAuthorizationService authorization, ILogger<PeerRequestsController> logger) : Controller
{
    private Guid Actor => Guid.TryParse(users.GetUserId(User), out var id) ? id : Guid.Empty;

    [HttpGet("/exchange-request.html")]
    public async Task<IActionResult> Compose(Guid teacherId, int skillId, CancellationToken ct) =>
        await RenderForm(new() { TeacherId = teacherId, SkillId = skillId }, ct);

    private async Task<IActionResult> RenderForm(PeerRequestInput input, CancellationToken ct)
    {
        var target = await requests.TargetAsync(Actor, input.TeacherId, input.SkillId, ct);
        return target is null ? NotFound("This teaching partner or skill is unavailable.") :
            View("~/Views/Skills/ExchangeRequest.cshtml", new PeerRequestForm(input, target,
                await requests.FindActiveIdAsync(Actor, input.TeacherId, input.SkillId, ct)));
    }

    [HttpPost("/exchange-request.html"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PeerRequestInput input, CancellationToken ct)
    {
        if (!ModelState.IsValid) return await RenderForm(input, ct);
        try
        {
            var result = await requests.CreateAsync(Actor, input, ct);
            if (result.Id is long id)
            {
                TempData["PeerRequestSubmitted"] = id.ToString();
                return RedirectToAction(nameof(Detail), new { id });
            }
            ModelState.AddModelError("", result.Error!);
            return await RenderForm(input, ct);
        }
        catch (Exception ex) when (ex is DbException or DbUpdateException || PeerRequestService.HasSqlError(ex, 1205, 2601, 2627))
        {
            logger.LogWarning("Peer request creation failed ({ExceptionType}).", ex.GetType().Name);
            Response.StatusCode = 503;
            return Content("The request could not be confirmed. Check your outgoing requests before trying again.");
        }
    }

    [HttpGet("/swap-requests.html")]
    public async Task<IActionResult> List(string? tab, int page = 1, string? search = null,
        RequestStatus? status = null, CancellationToken ct = default) =>
        View("~/Views/Skills/SwapRequests.cshtml", await requests.ListAsync(Actor,
            tab is "outgoing" or "completed" ? tab : "incoming", page, ct, search, status));

    [HttpGet("/request-sent.html")]
    public async Task<IActionResult> Detail(long id, CancellationToken ct)
    {
        var resource = await requests.FindAsync(id, ct);
        if (resource is null || !(await authorization.AuthorizeAsync(User, resource, RequestOperation.View)).Succeeded)
            return NotFound();
        var detail = await requests.DetailAsync(id, Actor, ct);
        return detail is null ? NotFound() : View("~/Views/Skills/RequestSent.cshtml", detail);
    }

    [HttpPost("/swap-requests.html"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(long id, string? decision, CancellationToken ct)
    {
        var next = decision switch
        {
            "accept" => RequestStatus.Accepted,
            "reject" => RequestStatus.Declined,
            "cancel" => RequestStatus.Cancelled,
            _ => RequestStatus.Pending
        };
        if (next == RequestStatus.Pending) return BadRequest();
        var resource = await requests.FindAsync(id, ct);
        var operation = next == RequestStatus.Cancelled ? RequestOperation.Cancel : RequestOperation.Respond;
        if (resource is null || !(await authorization.AuthorizeAsync(User, resource, operation)).Succeeded)
            return NotFound();
        try
        {
            var error = await requests.TransitionAsync(resource, Actor, next, ct);
            TempData["PeerRequestResult"] = error ?? (next == RequestStatus.Declined ? "Request rejected." : $"Request {next.ToString().ToLowerInvariant()}.");
        }
        catch (Exception ex) when (ex is DbException or DbUpdateException)
        {
            logger.LogWarning("Peer request update failed ({ExceptionType}).", ex.GetType().Name);
            TempData["PeerRequestResult"] = "The update could not be confirmed. Check the current status.";
        }
        return RedirectToAction(nameof(List), new { tab = next == RequestStatus.Cancelled ? "outgoing" : "incoming" });
    }
}
