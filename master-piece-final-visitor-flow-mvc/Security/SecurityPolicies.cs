using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SkillsSwap.Data;
using SkillsSwap.Models;

namespace SkillsSwap.Security;

public static class SecurityPolicies
{
    public const string ActiveMember = nameof(ActiveMember);
    public const string CompleteProfile = nameof(CompleteProfile);
}

public sealed class ActiveMemberRequirement : IAuthorizationRequirement;
public sealed class CompleteProfileRequirement : IAuthorizationRequirement;

public sealed class MemberStateAuthorizationHandler(ApplicationDbContext db)
    : IAuthorizationHandler
{
    public async Task HandleAsync(AuthorizationHandlerContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true ||
            !context.User.IsInRole("Member") ||
            !Guid.TryParse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId))
            return;

        var state = await db.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new { user.Status, user.EmailConfirmed, user.IsProfileComplete })
            .SingleOrDefaultAsync();

        if (state is null || state.Status != AccountStatus.Active || !state.EmailConfirmed)
            return;

        foreach (var requirement in context.PendingRequirements.ToArray())
        {
            if (requirement is ActiveMemberRequirement)
                context.Succeed(requirement);
            else if (requirement is CompleteProfileRequirement && state.IsProfileComplete)
                context.Succeed(requirement);
        }
    }
}
