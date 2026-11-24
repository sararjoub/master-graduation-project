using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using SkillsSwap.Controllers;
using SkillsSwap.Security;

namespace SkillsSwap.Tests;

public sealed class RouteProtectionTests
{
    [Theory]
    [InlineData(typeof(SkillsController))]
    [InlineData(typeof(SessionsController))]
    [InlineData(typeof(MembershipController))]
    [InlineData(typeof(CareerController))]
    public void Protected_member_controllers_require_complete_profile(Type controllerType)
    {
        var attribute=Assert.Single(controllerType.GetCustomAttributes(typeof(AuthorizeAttribute),true).Cast<AuthorizeAttribute>());
        Assert.Equal(SecurityPolicies.CompleteProfile,attribute.Policy);
    }

    [Fact]
    public void Profile_setup_requires_active_member_but_not_complete_profile()
    {
        var methods=typeof(AccountController).GetMethods().Where(method=>method.Name==nameof(AccountController.ProfileSetup)).ToList();
        Assert.Equal(2,methods.Count);
        Assert.All(methods,method=>Assert.Equal(SecurityPolicies.ActiveMember,Assert.Single(method.GetCustomAttributes(typeof(AuthorizeAttribute),true).Cast<AuthorizeAttribute>()).Policy));
    }

    [Theory]
    [InlineData(nameof(AccountController.Signup),"account-registration")]
    [InlineData(nameof(AccountController.ForgotPassword),"account-sensitive")]
    [InlineData(nameof(AccountController.ResendConfirmation),"account-sensitive")]
    public void Sensitive_post_endpoints_have_rate_limiting(string methodName,string policy)
    {
        var method=typeof(AccountController).GetMethods().Where(item=>item.Name==methodName)
            .Single(item=>item.GetCustomAttributes(inherit:true).Any(attribute=>attribute.GetType().Name=="HttpPostAttribute"));
        var attribute=(EnableRateLimitingAttribute)Assert.Single(method.GetCustomAttributes(typeof(EnableRateLimitingAttribute),true));
        Assert.Equal(policy,attribute.PolicyName);
    }

    [Fact]
    public void Public_catalogue_action_remains_anonymous()
    {
        var method=typeof(SkillsController).GetMethod(nameof(SkillsController.Categories))!;
        Assert.NotEmpty(method.GetCustomAttributes(typeof(AllowAnonymousAttribute),true));
    }
}
