using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartphoneShop.Core.Entities;
using SmartphoneShop.Core.Interfaces;
using X.PagedList;

namespace SmartphoneShop.Web.Controllers;

[Authorize(Roles = "Admin,ProductAdmin,RepairSpecialist")]
public class AdminSparePartsController : Controller
{
    private readonly ISparePartRepository _sparePartRepo;

    public AdminSparePartsController(ISparePartRepository sparePartRepo)
    {
        _sparePartRepo = sparePartRepo;
    }

    public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 10)
    {
        page = Math.Max(1, page);
        var parts = await _sparePartRepo.SearchAsync(search, page, pageSize);
        ViewBag.Search = search;
        return View(parts);
    }

    [Authorize(Roles = "Admin,ProductAdmin")]
    public IActionResult Create()
    {
        return View(new SparePart());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,ProductAdmin")]
    public async Task<IActionResult> Create(SparePart model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError("", "Заполните название запчасти");
            return View(model);
        }
        if (model.Price <= 0)
        {
            ModelState.AddModelError("", "Цена должна быть больше 0");
            return View(model);
        }
        if (model.Quantity < 0)
        {
            ModelState.AddModelError("", "Количество не может быть отрицательным");
            return View(model);
        }

        model.CreatedAt = DateTime.UtcNow;
        await _sparePartRepo.AddAsync(model);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,ProductAdmin")]
    public async Task<IActionResult> Edit(int id)
    {
        var part = await _sparePartRepo.GetByIdAsync(id);
        if (part == null) return NotFound();
        return View(part);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,ProductAdmin")]
    public async Task<IActionResult> Edit(SparePart model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError("", "Заполните название запчасти");
            return View(model);
        }
        if (model.Price <= 0)
        {
            ModelState.AddModelError("", "Цена должна быть больше 0");
            return View(model);
        }
        if (model.Quantity < 0)
        {
            ModelState.AddModelError("", "Количество не может быть отрицательным");
            return View(model);
        }

        var part = await _sparePartRepo.GetByIdAsync(model.Id);
        if (part == null) return NotFound();

        part.Name = model.Name;
        part.Price = model.Price;
        part.Quantity = model.Quantity;

        await _sparePartRepo.UpdateAsync(part);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,ProductAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _sparePartRepo.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
