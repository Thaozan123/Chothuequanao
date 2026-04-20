using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChoThueQuanAo.Data;
using ChoThueQuanAo.Models;
using Microsoft.AspNetCore.Authorization;

namespace ChoThueQuanAo.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SupplierController : Controller
    {
        private readonly AppDbContext _context;

        public SupplierController(AppDbContext context)
        {
            _context = context;
        }

       public async Task<IActionResult> Index(string keyword)
{
    var supplierQuery = _context.Suppliers.AsQueryable();

    if (!string.IsNullOrWhiteSpace(keyword))
    {
        keyword = keyword.Trim();

        supplierQuery = supplierQuery.Where(s =>
            s.Name.Contains(keyword) ||
            (s.Phone != null && s.Phone.Contains(keyword)) ||
            (s.Email != null && s.Email.Contains(keyword)) ||
            (s.Address != null && s.Address.Contains(keyword)));
    }

    var suppliers = await supplierQuery
        .OrderByDescending(s => s.CreatedAt)
        .ToListAsync();

    ViewBag.Keyword = keyword;

    if (!string.IsNullOrWhiteSpace(keyword))
    {
        var selectedSupplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Name.Contains(keyword));

        if (selectedSupplier != null)
        {
            var supplierProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.SupplierId == selectedSupplier.Id)
                .ToListAsync();

            ViewBag.SelectedSupplierName = selectedSupplier.Name;
            ViewBag.SupplierProducts = supplierProducts;

            ViewBag.TotalImportedQuantity = supplierProducts.Sum(p => p.ImportedQuantity);
            ViewBag.TotalStock = supplierProducts.Sum(p => p.StockQuantity);
        }
    }

    return View(suppliers);
}

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                _context.Suppliers.Add(supplier);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(supplier);
        }

        public async Task<IActionResult> Details(int id)
        {
            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == id);

            if (supplier == null)
            {
                return NotFound();
            }

            return View(supplier);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);

            if (supplier == null)
            {
                return NotFound();
            }

            return View(supplier);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Supplier supplier)
        {
            if (id != supplier.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _context.Suppliers.Update(supplier);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(supplier);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);

            if (supplier == null)
            {
                return NotFound();
            }

            return View(supplier);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);

            if (supplier != null)
            {
                _context.Suppliers.Remove(supplier);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}