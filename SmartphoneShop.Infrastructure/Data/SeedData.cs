using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartphoneShop.Core.Entities;

namespace SmartphoneShop.Infrastructure.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<AppDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.MigrateAsync();

        string[] roles = { "Admin", "User", "Expert", "ProductAdmin", "RepairSpecialist", "Народный эксперт" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var usersToSeed = new (string email, string fullName, string role)[]
        {
            ("admin@smartshop.com", "Администратор", "Admin"),
            ("testuser@test.com", "Тестовый пользователь", "User"),
            ("prodadmin2@test.com", "Продуктовый админ 2", "ProductAdmin"),
            ("repair@test.com", "Ремонтник тестовый", "RepairSpecialist"),
            ("repair@smartshop.com", "Ремонтник смартшоп", "RepairSpecialist"),
            ("folk@smartshop.com", "Народный эксперт смартшоп", "Народный эксперт"),
            ("folk@test.com", "Народный эксперт тестовый", "Народный эксперт"),
            ("expert@smartshop.com", "Эксперт смартшоп", "Expert"),
            ("expert@test.com", "Эксперт тестовый", "Expert"),
            ("n.smantser555@gmail.com", "Николай Сманцер", "User"),
            ("regtest1@test.com", "Тестовый пользователь 1", "User"),
            ("regtest2@test.com", "Тестовый пользователь 2", "User"),
        };

        foreach (var (email, fullName, role) in usersToSeed)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new AppUser
                {
                    UserName = email,
                    Email = email,
                    FullName = fullName,
                    EmailConfirmed = true
                };
                var createResult = await userManager.CreateAsync(user, "bmw850850");
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role);
                }
            }
            else
            {
                user.PasswordHash = userManager.PasswordHasher.HashPassword(user, "bmw850850");
                await userManager.UpdateAsync(user);
                var currentRoles = await userManager.GetRolesAsync(user);
                if (!currentRoles.Contains(role) || currentRoles.Count > 1)
                {
                    await context.Database.ExecuteSqlRawAsync(
                        "DELETE FROM AspNetUserRoles WHERE UserId = {0}", user.Id);
                    var targetRole = await roleManager.FindByNameAsync(role);
                    if (targetRole != null)
                    {
                        await context.Database.ExecuteSqlRawAsync(
                            "INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES ({0}, {1})",
                            user.Id, targetRole.Id);
                    }
                }
            }
        }

    }
}