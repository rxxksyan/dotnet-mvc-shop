using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using SmartphoneShop.Core.Entities;
using SmartphoneShop.Core.Interfaces;
using SmartphoneShop.Infrastructure.Data;
using SmartphoneShop.Infrastructure.Repositories;
using SmartphoneShop.Web.Services;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithExceptionDetails()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProcessId()
    .Enrich.WithProcessName()
    .Enrich.WithThreadId()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
        theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code)
    .WriteTo.File(
        path: "logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        fileSizeLimitBytes: 10 * 1024 * 1024,
        retainedFileCountLimit: 30,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting web application");
    
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog(); // Замена стандартного логгера на Serilog

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.MaxAge = TimeSpan.FromDays(30);
});

builder.Services.AddScoped<ISmartphoneRepository, SmartphoneRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IRepairRequestRepository, RepairRequestRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IComparisonRepository, ComparisonRepository>();
builder.Services.AddScoped<IFavoriteRepository, FavoriteRepository>();
builder.Services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
builder.Services.AddScoped<ReportGenerator>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await SeedData.InitializeAsync(scope.ServiceProvider);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();

// Принудительно инициализируем сессию ДО маршрутизации
// Это решает проблему когда Session.Id меняется между запросами для гостей
app.Use(async (context, next) =>
{
    // Доступ к Session.Id принудительно инициализирует сессию
    var sessionId = context.Session.Id;
    // Отправляем куку немедленно, а не только при изменении данных
    context.Session.SetString("__init", "1");
    await next();
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Middleware логирования запросов
app.Use(async (context, next) =>
{
    var start = Stopwatch.GetTimestamp();
    try
    {
        await next();
        var elapsed = Stopwatch.GetElapsedTime(start);
        
        var statusCode = context.Response.StatusCode;
        var level = statusCode >= 500 ? LogEventLevel.Error : 
                    statusCode >= 400 ? LogEventLevel.Warning : LogEventLevel.Information;
        
        Log.Write(level, "HTTP {Method} {Path} responded {StatusCode} in {Elapsed:0.0000}ms | Session: {SessionId} | User: {UserId}",
            context.Request.Method,
            context.Request.Path,
            statusCode,
            elapsed.TotalMilliseconds,
            context.Session.Id,
            context.User.Identity?.IsAuthenticated == true ? context.User.Identity.Name : "Anonymous");
    }
    catch (Exception ex)
    {
        var elapsed = Stopwatch.GetElapsedTime(start);
        Log.Error(ex, "HTTP {Method} {Path} failed after {Elapsed:0.0000}ms | Session: {SessionId} | User: {UserId}",
            context.Request.Method,
            context.Request.Path,
            elapsed.TotalMilliseconds,
            context.Session.Id,
            context.User.Identity?.IsAuthenticated == true ? context.User.Identity.Name : "Anonymous");
        throw;
    }
});

app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}