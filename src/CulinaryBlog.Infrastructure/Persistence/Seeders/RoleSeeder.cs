namespace CulinaryBlog.Infrastructure.Persistence.Seeders;

using CulinaryBlog.Application.Auth.Common;
using CulinaryBlog.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public sealed class RoleSeeder(
    RoleManager<IdentityRole> roleManager,
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration,
    ILogger<RoleSeeder> logger)
{
    private static readonly Action<ILogger, string, Exception?> LogRoleSeeded =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(6001, "RoleSeeded"), "Seeded role: {Role}");

    private static readonly Action<ILogger, string, Exception?> LogAdminSeeded =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(6002, "AdminSeeded"), "Seeded default admin user: {Email}");

    private static readonly Action<ILogger, string, Exception?> LogAdminSeedFailed =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(6003, "AdminSeedFailed"), "Failed to seed admin user: {Errors}");

    private static readonly Action<ILogger, Exception?> LogAdminCredentialsMissing =
        LoggerMessage.Define(LogLevel.Information, new EventId(6004, "AdminCredentialsMissing"), "Admin credentials not configured in settings/environment; skipping admin user seeding.");

    public async Task SeedAsync()
    {
        string[] roles = [Roles.Author, Roles.Admin];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                LogRoleSeeded(logger, role, null);
            }
        }

        var adminEmail = configuration["Admin:Email"];
        var adminPassword = configuration["Admin:Password"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            LogAdminCredentialsMissing(logger, null);
            return;
        }

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                DisplayName = "System Administrator",
                EmailConfirmed = true,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRolesAsync(adminUser, [Roles.Admin, Roles.Author]);
                LogAdminSeeded(logger, adminEmail, null);
            }
            else
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                LogAdminSeedFailed(logger, errors, null);
            }
        }
    }
}
