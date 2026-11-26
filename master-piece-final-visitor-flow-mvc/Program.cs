using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using SkillsSwap.Data;
using SkillsSwap.Models;
using SkillsSwap.Security;
using SkillsSwap.Services;
using SkillsSwap.Services.Email;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<CatalogueService>();
builder.Services.AddScoped<SkillDiscoveryService>();
builder.Services.AddScoped<PeerRequestService>();
builder.Services.AddScoped<PeerSessionService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddOptions<SessionCompletionOptions>().BindConfiguration("SessionCompletion")
    .Validate(o => o.IntervalSeconds is >= 1 and <= 3600, "Invalid session completion interval.")
    .ValidateOnStart();
builder.Services.AddHostedService<SessionCompletionWorker>();
builder.Services.AddSingleton<IAuthorizationHandler, RequestAuthorization>();
builder.Services.AddScoped<AdminPeopleService>();
builder.Services.AddScoped<AdminOperationsService>();
builder.Services.Configure<AccountEmailOptions>(builder.Configuration.GetSection(AccountEmailOptions.SectionName));
builder.Services.Configure<AccountRateLimitOptions>(builder.Configuration.GetSection(AccountRateLimitOptions.SectionName));
if (builder.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>($"{AccountEmailOptions.SectionName}:EnableDevelopmentPreview"))
    builder.Services.AddScoped<IAccountEmailSender, DevelopmentAccountEmailSender>();
else
    builder.Services.AddScoped<IAccountEmailSender, SmtpAccountEmailSender>();
builder.Services.AddScoped<IAuthorizationHandler, MemberStateAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, MemberAuthorizationResultHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(SecurityPolicies.ActiveMember, policy =>
        policy.RequireAuthenticatedUser().AddRequirements(new ActiveMemberRequirement()));
    options.AddPolicy(SecurityPolicies.CompleteProfile, policy =>
        policy.RequireAuthenticatedUser().AddRequirements(new CompleteProfileRequirement()));
});
builder.Services.AddOptions<SkillsSwap.Models.VideoMeetingOptions>().BindConfiguration("VideoMeeting")
    .Validate(o => o.Domain == "meet.jit.si" && o.OpeningMinutes is >= 0 and <= 120,
        "Invalid video meeting domain or opening margin.")
    .ValidateOnStart();
var rateLimits = builder.Configuration.GetSection(AccountRateLimitOptions.SectionName).Get<AccountRateLimitOptions>() ?? new();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await context.HttpContext.Response.WriteAsync("Too many requests. Please wait and try again.", cancellationToken);
    };
    options.AddPolicy("learning-chat-send", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("account-sensitive", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = Math.Max(1, rateLimits.SensitivePermitLimit), Window = TimeSpan.FromMinutes(Math.Max(1, rateLimits.WindowMinutes)), QueueLimit = 0 }));
    options.AddPolicy("account-registration", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = Math.Max(1, rateLimits.RegistrationPermitLimit), Window = TimeSpan.FromMinutes(Math.Max(1, rateLimits.WindowMinutes)), QueueLimit = 0 }));
});
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
    {
        options.SignIn.RequireConfirmedEmail = true;
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login.html";
    options.AccessDeniedPath = "/access-denied.html";
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    foreach (var roleName in new[] { "Member", "Admin" })
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(
                new IdentityRole<Guid>(roleName)
            );
        }
    }

    await IdentitySeeder.SeedAdminAsync(
        scope.ServiceProvider,
        builder.Configuration
    );
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllerRoute("areas", "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
app.MapControllerRoute("default", "{controller}/{action}/{id?}");

app.Run();
