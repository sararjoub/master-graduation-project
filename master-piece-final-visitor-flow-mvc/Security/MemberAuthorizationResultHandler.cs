using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.EntityFrameworkCore;
using SkillsSwap.Data;
using SkillsSwap.Models;

namespace SkillsSwap.Security;

public sealed class MemberAuthorizationResultHandler(IServiceScopeFactory scopeFactory)
    : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler fallback = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        var requiresCompleteProfile = context.GetEndpoint()?.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .Any(data => data.Policy == SecurityPolicies.CompleteProfile) == true;

        if (authorizeResult.Forbidden && requiresCompleteProfile && context.User.IsInRole("Member") &&
            Guid.TryParse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var state = await db.Users.AsNoTracking().Where(user => user.Id == userId)
                .Select(user => new { user.Status, user.EmailConfirmed, user.IsProfileComplete }).SingleOrDefaultAsync();

            if (state is { Status: AccountStatus.Active, EmailConfirmed: true, IsProfileComplete: false })
            {
                context.Response.Redirect("/profile-setup.html");
                return;
            }
        }

        await fallback.HandleAsync(next, context, policy, authorizeResult);
    }
}
