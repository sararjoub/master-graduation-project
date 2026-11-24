using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using SkillsSwap.Models;

namespace SkillsSwap.Security;

public sealed record RequestOperation(string Name) : IAuthorizationRequirement
{
    public static readonly RequestOperation View = new("View");
    public static readonly RequestOperation Respond = new("Respond");
    public static readonly RequestOperation Cancel = new("Cancel");
}

public sealed class RequestAuthorization : AuthorizationHandler<RequestOperation, LearningRequest>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
        RequestOperation requirement, LearningRequest resource)
    {
        if (context.User.Identity?.IsAuthenticated == true && context.User.IsInRole("Member") &&
            Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) &&
            !resource.IsExpertBooking &&
            (requirement == RequestOperation.View && (id == resource.LearnerId || id == resource.TeacherId) ||
             requirement == RequestOperation.Respond && id == resource.TeacherId ||
             requirement == RequestOperation.Cancel && id == resource.LearnerId))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
