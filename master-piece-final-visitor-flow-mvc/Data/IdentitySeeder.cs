using Microsoft.AspNetCore.Identity;
using SkillsSwap.Models;

namespace SkillsSwap.Data;

public static class IdentitySeeder
{
    public static async Task SeedAdminAsync(
        IServiceProvider services,
        IConfiguration configuration)
    {
        var userManager =
            services.GetRequiredService<UserManager<ApplicationUser>>();

        var email = configuration["AdminSeed:Email"];
        var password = configuration["AdminSeed:Password"];

        // لا ننشئ حساباً إذا لم توضع الإعدادات السرية.
        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var admin = await userManager.FindByEmailAsync(email);

        if (admin is null)
        {
            admin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                FullName = "Skills Swap Admin",
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                IsProfileComplete = true,
                Status = AccountStatus.Active,
                CreatedAtUtc = DateTime.UtcNow
            };

            var createResult =
                await userManager.CreateAsync(admin, password);

            if (!createResult.Succeeded)
            {
                var errors = string.Join(
                    ", ",
                    createResult.Errors.Select(error => error.Description)
                );

                throw new InvalidOperationException(
                    $"Admin creation failed: {errors}"
                );
            }
        }

        if (!await userManager.IsInRoleAsync(admin, "Admin"))
        {
            var roleResult =
                await userManager.AddToRoleAsync(admin, "Admin");

            if (!roleResult.Succeeded)
            {
                var errors = string.Join(
                    ", ",
                    roleResult.Errors.Select(error => error.Description)
                );

                throw new InvalidOperationException(
                    $"Assigning Admin role failed: {errors}"
                );
            }
        }
    }
}