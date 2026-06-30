using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartphoneShop.Core.Entities;
using SmartphoneShop.Infrastructure.Data;
using SmartphoneShop.Web.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using System.IO;
using X.PagedList;

namespace SmartphoneShop.Web.Controllers;

[Authorize(Roles = "Admin,ProductAdmin")]
public class AdminSmartphoneController : Controller
{
    private readonly AppDbContext _context;

    public AdminSmartphoneController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 10)
    {
        page = Math.Max(1, page);
        var query = _context.Smartphones.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lowerSearch = search.ToLower();
            query = query.Where(s => s.ModelName.ToLower().Contains(lowerSearch) ||
                                     s.Brand.ToLower().Contains(lowerSearch));
        }

        var smartphones = await query
            .OrderByDescending(s => s.CreatedAt)
            .ToPagedListAsync(page, pageSize);

        ViewBag.Search = search;
        return View(smartphones);
    }

    public IActionResult Create()
    {
        return View(new SmartphoneFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SmartphoneFormViewModel model)
    {
        var parsedPrice = ParseDecimal(model.Price);
        if (string.IsNullOrWhiteSpace(model.ModelName) || string.IsNullOrWhiteSpace(model.Brand) || parsedPrice == null || parsedPrice <= 0)
        {
            ModelState.AddModelError("", "Заполните обязательные поля: Бренд, Модель, Цена");
            return View(model);
        }

        var smartphone = new Smartphone
        {
            ModelName = model.ModelName,
            Brand = model.Brand,
            Price = parsedPrice.Value,
            OldPrice = ParseDecimal(model.OldPrice),
            RAM = ParseInt(model.RAM),
            Storage = ParseInt(model.Storage),
            ScreenSize = ParseDecimal(model.ScreenSize),
            ScreenResolution = model.ScreenResolution,
            ScreenType = model.ScreenType,
            BatteryCapacity = ParseInt(model.BatteryCapacity),
            Processor = model.Processor,
            MainCamera = model.MainCamera,
            FrontCamera = model.FrontCamera,
            OS = model.OS,
            NFC = model.NFC,
            WirelessCharging = model.WirelessCharging,
            WaterResistance = model.WaterResistance,
            Weight = ParseDecimal(model.Weight),
            Colors = model.Colors,
            ImageUrl = model.ImageUrl,
            ImageUrls = model.ImageUrls,
            Description = model.Description,
            Quantity = model.Quantity,
            IsFeatured = model.IsFeatured,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (model.ImageFiles != null && model.ImageFiles.Length > 0)
        {
            var urls = new List<string>();
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

            foreach (var file in model.ImageFiles)
            {
                if (file.Length > 0)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                    var filePath = Path.Combine(uploadsDir, fileName);
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(stream);
                    urls.Add("/uploads/" + fileName);
                }
            }
            if (urls.Count > 0)
            {
                smartphone.ImageUrls = JsonSerializer.Serialize(urls);
                smartphone.ImageUrl = urls[0];
            }
        }

        _context.Smartphones.Add(smartphone);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var smartphone = await _context.Smartphones.FindAsync(id);
        if (smartphone == null)
            return NotFound();

        var model = new SmartphoneFormViewModel
        {
            Id = smartphone.Id,
            ModelName = smartphone.ModelName,
            Brand = smartphone.Brand,
            Price = smartphone.Price.ToString(System.Globalization.CultureInfo.InvariantCulture),
            OldPrice = smartphone.OldPrice?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            RAM = smartphone.RAM?.ToString(),
            Storage = smartphone.Storage?.ToString(),
            ScreenSize = smartphone.ScreenSize?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ScreenResolution = smartphone.ScreenResolution,
            ScreenType = smartphone.ScreenType,
            BatteryCapacity = smartphone.BatteryCapacity?.ToString(),
            Processor = smartphone.Processor,
            MainCamera = smartphone.MainCamera,
            FrontCamera = smartphone.FrontCamera,
            OS = smartphone.OS,
            NFC = smartphone.NFC,
            WirelessCharging = smartphone.WirelessCharging,
            WaterResistance = smartphone.WaterResistance,
            Weight = smartphone.Weight?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Colors = smartphone.Colors,
            ImageUrl = smartphone.ImageUrl,
            ImageUrls = smartphone.ImageUrls,
            Description = smartphone.Description,
            Quantity = smartphone.Quantity,
            IsFeatured = smartphone.IsFeatured,
            CreatedAt = smartphone.CreatedAt
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SmartphoneFormViewModel model)
    {
        var parsedPrice = ParseDecimal(model.Price);
        if (string.IsNullOrWhiteSpace(model.ModelName) || string.IsNullOrWhiteSpace(model.Brand) || parsedPrice == null || parsedPrice <= 0)
        {
            ModelState.AddModelError("", "Заполните обязательные поля: Бренд, Модель, Цена");
            return View(model);
        }

        var smartphone = await _context.Smartphones.FindAsync(model.Id);
        if (smartphone == null)
            return NotFound();

        smartphone.ModelName = model.ModelName;
        smartphone.Brand = model.Brand;
        smartphone.Price = parsedPrice.Value;
        smartphone.OldPrice = ParseDecimal(model.OldPrice);
        smartphone.RAM = ParseInt(model.RAM);
        smartphone.Storage = ParseInt(model.Storage);
        smartphone.ScreenSize = ParseDecimal(model.ScreenSize);
        smartphone.ScreenResolution = model.ScreenResolution;
        smartphone.ScreenType = model.ScreenType;
        smartphone.BatteryCapacity = ParseInt(model.BatteryCapacity);
        smartphone.Processor = model.Processor;
        smartphone.MainCamera = model.MainCamera;
        smartphone.FrontCamera = model.FrontCamera;
        smartphone.OS = model.OS;
        smartphone.NFC = model.NFC;
        smartphone.WirelessCharging = model.WirelessCharging;
        smartphone.WaterResistance = model.WaterResistance;
        smartphone.Weight = ParseDecimal(model.Weight);
        smartphone.Colors = model.Colors;
        smartphone.ImageUrl = model.ImageUrl;
        smartphone.ImageUrls = model.ImageUrls;
        smartphone.Description = model.Description;
        smartphone.Quantity = model.Quantity;
        smartphone.IsFeatured = model.IsFeatured;
        smartphone.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(model.PhotosToDelete))
        {
            var toDelete = model.PhotosToDelete.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var currentUrls = new List<string>();
            if (!string.IsNullOrEmpty(smartphone.ImageUrls))
            {
                currentUrls = JsonSerializer.Deserialize<List<string>>(smartphone.ImageUrls) ?? new();
            }
            else if (!string.IsNullOrEmpty(smartphone.ImageUrl))
            {
                currentUrls = new List<string> { smartphone.ImageUrl };
            }

            var wwwroot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"));
            foreach (var url in toDelete)
            {
                currentUrls.RemoveAll(u => u == url);
                var filePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", url.TrimStart('/')));
                if (filePath.StartsWith(wwwroot, StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            if (currentUrls.Count > 0)
            {
                smartphone.ImageUrls = JsonSerializer.Serialize(currentUrls);
                smartphone.ImageUrl = currentUrls[0];
            }
            else
            {
                smartphone.ImageUrls = null;
                smartphone.ImageUrl = null;
            }
        }

        if (model.ImageFiles != null && model.ImageFiles.Length > 0)
        {
            var currentUrls = new List<string>();
            if (!string.IsNullOrEmpty(smartphone.ImageUrls))
            {
                currentUrls = JsonSerializer.Deserialize<List<string>>(smartphone.ImageUrls) ?? new();
            }
            else if (!string.IsNullOrEmpty(smartphone.ImageUrl))
            {
                currentUrls.Add(smartphone.ImageUrl);
            }

            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

            foreach (var file in model.ImageFiles)
            {
                if (file.Length > 0)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                    var filePath = Path.Combine(uploadsDir, fileName);
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(stream);
                    currentUrls.Add("/uploads/" + fileName);
                }
            }
            if (currentUrls.Count > 0)
            {
                smartphone.ImageUrls = JsonSerializer.Serialize(currentUrls);
                smartphone.ImageUrl = currentUrls[0];
            }
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var smartphone = await _context.Smartphones.FindAsync(id);
        if (smartphone != null)
        {
            _context.Smartphones.Remove(smartphone);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private static int? ParseInt(string? s)
        => int.TryParse(s, out var v) ? v : null;

    private static decimal? ParseDecimal(string? s)
        => decimal.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;
}
