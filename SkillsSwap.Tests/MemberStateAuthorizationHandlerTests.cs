using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SkillsSwap.Data;
using SkillsSwap.Models;
using SkillsSwap.Security;

namespace SkillsSwap.Tests;

public sealed class MemberStateAuthorizationHandlerTests
{
    [Theory]
    [InlineData(AccountStatus.Suspended, true, true)]
    [InlineData(AccountStatus.Closed, true, true)]
    [InlineData(AccountStatus.Active, false, true)]
    public async Task ActiveMember_rejects_inactive_or_unverified_member(AccountStatus status, bool confirmed, bool complete)
    {
        var (handler,user) = await CreateHandlerAsync(status, confirmed, complete);
        var requirement = new ActiveMemberRequirement();
        var context = new AuthorizationHandlerContext([requirement], Principal(user.Id,"Member"), null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task ActiveMember_accepts_active_verified_member_during_onboarding()
    {
        var (handler,user) = await CreateHandlerAsync(AccountStatus.Active, true, false);
        var requirement = new ActiveMemberRequirement();
        var context = new AuthorizationHandlerContext([requirement], Principal(user.Id,"Member"), null);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task CompleteProfile_rejects_incomplete_member()
    {
        var (handler,user) = await CreateHandlerAsync(AccountStatus.Active, true, false);
        var requirement = new CompleteProfileRequirement();
        var context = new AuthorizationHandlerContext([requirement], Principal(user.Id,"Member"), null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task CompleteProfile_accepts_active_verified_complete_member()
    {
        var (handler,user) = await CreateHandlerAsync(AccountStatus.Active, true, true);
        var requirement = new CompleteProfileRequirement();
        var context = new AuthorizationHandlerContext([requirement], Principal(user.Id,"Member"), null);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Member_policy_does_not_treat_admin_as_member()
    {
        var (handler,user) = await CreateHandlerAsync(AccountStatus.Active, true, true);
        var requirement = new ActiveMemberRequirement();
        var context = new AuthorizationHandlerContext([requirement], Principal(user.Id,"Admin"), null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    private static ClaimsPrincipal Principal(Guid id,string role) => new(new ClaimsIdentity([
        new Claim(ClaimTypes.NameIdentifier,id.ToString()), new Claim(ClaimTypes.Role,role)
    ],"Test"));

    private static async Task<(MemberStateAuthorizationHandler Handler,ApplicationUser User)> CreateHandlerAsync(AccountStatus status,bool confirmed,bool complete)
    {
        var options=new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db=new ApplicationDbContext(options);
        var user=new ApplicationUser { Id=Guid.NewGuid(),UserName="member@example.test",Email="member@example.test",FullName="Test Member",Status=status,EmailConfirmed=confirmed,IsProfileComplete=complete };
        db.Users.Add(user); await db.SaveChangesAsync();
        return (new MemberStateAuthorizationHandler(db),user);
    }
}
