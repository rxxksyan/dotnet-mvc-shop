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

        if (!context.Smartphones.Any())
        {
            var smartphones = new List<Smartphone>
            {
                new Smartphone
                {
                    ModelName = "Galaxy S24 Ultra",
                    Brand = "Samsung",
                    Price = 129990,
                    OldPrice = 149990,
                    ScreenSize = 6.8m,
                    ScreenResolution = "3120x1440",
                    ScreenType = "Dynamic AMOLED 2X",
                    BatteryCapacity = 5000,
                    RAM = 12,
                    Storage = 256,
                    Processor = "Snapdragon 8 Gen 3",
                    MainCamera = "200MP",
                    FrontCamera = "12MP",
                    OS = "Android 14",
                    NFC = true,
                    WirelessCharging = true,
                    WaterResistance = "IP68",
                    Weight = 232,
                    Colors = "[\"Черный\", \"Серый\", \"Фиолетовый\"]",
                    ImageUrl = "https://images.pexels.com/photos/404280/pexels-photo-404280.jpeg?auto=compress&cs=tinysrgb&w=400",
                    Description = "Флагманский смартфон Samsung с S Pen и мощной камерой 200MP",
                    IsFeatured = true,
                    IsInStock = true
                },
                new Smartphone
                {
                    ModelName = "iPhone 15 Pro Max",
                    Brand = "Apple",
                    Price = 149990,
                    ScreenSize = 6.7m,
                    ScreenResolution = "2796x1290",
                    ScreenType = "Super Retina XDR",
                    BatteryCapacity = 4441,
                    RAM = 8,
                    Storage = 256,
                    Processor = "A17 Pro",
                    MainCamera = "48MP",
                    FrontCamera = "12MP",
                    OS = "iOS 17",
                    NFC = true,
                    WirelessCharging = true,
                    WaterResistance = "IP68",
                    Weight = 221,
                    Colors = "[\"Титановый\", \"Черный\", \"Белый\", \"Синий\"]",
                    ImageUrl = "https://images.pexels.com/photos/788946/pexels-photo-788946.jpeg?auto=compress&cs=tinysrgb&w=400",
                    Description = "Самый мощный iPhone с титановым корпусом и камерой Pro",
                    IsFeatured = true,
                    IsInStock = true
                },
                new Smartphone
                {
                    ModelName = "Xiaomi 14 Ultra",
                    Brand = "Xiaomi",
                    Price = 89990,
                    OldPrice = 99990,
                    ScreenSize = 6.73m,
                    ScreenResolution = "3200x1440",
                    ScreenType = "LTPO AMOLED",
                    BatteryCapacity = 5000,
                    RAM = 16,
                    Storage = 512,
                    Processor = "Snapdragon 8 Gen 3",
                    MainCamera = "50MP (Leica)",
                    FrontCamera = "32MP",
                    OS = "Android 14",
                    NFC = true,
                    WirelessCharging = true,
                    WaterResistance = "IP68",
                    Weight = 219,
                    Colors = "[\"Черный\", \"Белый\"]",
                    ImageUrl = "https://images.pexels.com/photos/8372149/pexels-photo-8372149.jpeg?auto=compress&cs=tinysrgb&w=400",
                    Description = "Камерофон с оптикой Leica и мощным процессором",
                    IsFeatured = true,
                    IsInStock = true
                },
                new Smartphone
                {
                    ModelName = "Redmi Note 13 Pro",
                    Brand = "Xiaomi",
                    Price = 24990,
                    ScreenSize = 6.67m,
                    ScreenResolution = "2712x1220",
                    ScreenType = "AMOLED",
                    BatteryCapacity = 5100,
                    RAM = 8,
                    Storage = 256,
                    Processor = "Snapdragon 7s Gen 2",
                    MainCamera = "200MP",
                    FrontCamera = "16MP",
                    OS = "Android 13",
                    NFC = true,
                    WirelessCharging = false,
                    WaterResistance = "IP54",
                    Weight = 187,
                    Colors = "[\"Черный\", \"Белый\", \"Зеленый\"]",
                    ImageUrl = "https://images.pexels.com/photos/5731871/pexels-photo-5731871.jpeg?auto=compress&cs=tinysrgb&w=400",
                    Description = "Средний класс с отличной камерой 200MP",
                    IsInStock = true
                },
                new Smartphone
                {
                    ModelName = "Galaxy A54",
                    Brand = "Samsung",
                    Price = 34990,
                    OldPrice = 39990,
                    ScreenSize = 6.4m,
                    ScreenResolution = "2340x1080",
                    ScreenType = "Super AMOLED",
                    BatteryCapacity = 5000,
                    RAM = 8,
                    Storage = 256,
                    Processor = "Exynos 1380",
                    MainCamera = "50MP",
                    FrontCamera = "32MP",
                    OS = "Android 13",
                    NFC = true,
                    WirelessCharging = false,
                    WaterResistance = "IP67",
                    Weight = 202,
                    Colors = "[\"Черный\", \"Белый\", \"Фиолетовый\"]",
                    ImageUrl = "https://images.pexels.com/photos/1092644/pexels-photo-1092644.jpeg?auto=compress&cs=tinysrgb&w=400",
                    Description = "Надежный средний класс от Samsung",
                    IsInStock = true
                },
                new Smartphone
                {
                    ModelName = "Pixel 8 Pro",
                    Brand = "Google",
                    Price = 79990,
                    ScreenSize = 6.7m,
                    ScreenResolution = "2992x1344",
                    ScreenType = "LTPO OLED",
                    BatteryCapacity = 5050,
                    RAM = 12,
                    Storage = 256,
                    Processor = "Google Tensor G3",
                    MainCamera = "50MP",
                    FrontCamera = "10.5MP",
                    OS = "Android 14",
                    NFC = true,
                    WirelessCharging = true,
                    WaterResistance = "IP68",
                    Weight = 213,
                    Colors = "[\"Черный\", \"Синий\", \"Бежевый\"]",
                    ImageUrl = "https://images.pexels.com/photos/607812/pexels-photo-607812.jpeg?auto=compress&cs=tinysrgb&w=400",
                    Description = "Смартфон Google с лучшим ИИ для камеры",
                    IsFeatured = true,
                    IsInStock = true
                }
            };

            context.Smartphones.AddRange(smartphones);
            await context.SaveChangesAsync();
        }
    }
}