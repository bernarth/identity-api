using Identity.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Extensions;

public static class SeedDataExtensions
{
    /// <summary>
    /// Seeds the roles and the first admin, but only while the database has no users
    /// once real accounts exist this is permanently inert
    /// </summary>
    public static async Task SeedIdentityDataAsync(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();

        UserManager<ApplicationUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (await userManager.Users.AnyAsync())
        {
            return;
        }

        RoleManager<IdentityRole> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (string role in (string[])["Admin", "User"])
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        string? adminEmail = app.Configuration["Seed:AdminEmail"];
        string? adminPassword = app.Configuration["Seed:AdminPassword"];

        if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
        {
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "System",
            LastName = "Admin",
            CreatedAt = DateTime.UtcNow,
        };

        IdentityResult created = await userManager.CreateAsync(admin, adminPassword);

        if (!created.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to seed admin user: {string.Join("; ", created.Errors.Select(e => e.Description))}");
        }

        await userManager.AddToRoleAsync(admin, "Admin");
    }
}
