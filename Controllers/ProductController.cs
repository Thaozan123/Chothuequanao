using ChoThueQuanAo.Data;
using ChoThueQuanAo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ChoThueQuanAo.Controllers
{
    public class ProductController : Controller
    {
        private readonly AppDbContext _context;

        public ProductController(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================================
        // XEM + TÌM KIẾM
        // ==========================================================

        [AllowAnonymous]
        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;

            // 🔥 FIX: thêm Supplier
            var products = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier) // 👈 QUAN TRỌNG
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p =>
                    p.Name.Contains(searchString) ||
                    p.ProductCode.Contains(searchString));
            }

            return View(await products.ToListAsync());
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier) // 👈 thêm
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();
            return View(product);
        }

        // ==========================================================
        // ADMIN
        // ==========================================================

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
{
    ViewBag.CategoryId = new SelectList(_context.ProductCategories, "Id", "Name");
    ViewBag.SupplierId = new SelectList(_context.Suppliers, "Id", "Name");
    return View();
}

        [HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> Create(Product product)
{
    // Tự động gán số lượng tồn kho bằng với số lượng đã nhập khi thêm mới
    product.StockQuantity = product.ImportedQuantity;
    ModelState.Remove("StockQuantity"); // Bỏ qua kiểm tra lỗi rỗng nếu có

    if (ModelState.IsValid)
    {
        _context.Add(product);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    ViewBag.CategoryId = new SelectList(_context.ProductCategories, "Id", "Name", product.CategoryId);
    ViewBag.SupplierId = new SelectList(_context.Suppliers, "Id", "Name", product.SupplierId);

    return View(product);
}

        [Authorize(Roles = "Admin")]
      public async Task<IActionResult> Edit(int id)
{
    var product = await _context.Products.FindAsync(id);
    if (product == null) return NotFound();

    ViewBag.CategoryId = new SelectList(_context.ProductCategories, "Id", "Name", product.CategoryId);
    ViewBag.SupplierId = new SelectList(_context.Suppliers, "Id", "Name", product.SupplierId);

    return View(product);
}
       [HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> Edit(int id, Product product)
{
    if (id != product.Id) return NotFound();

    if (ModelState.IsValid)
    {
        try
        {
            _context.Update(product);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Products.Any(e => e.Id == product.Id))
                return NotFound();
            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    ViewBag.CategoryId = new SelectList(_context.ProductCategories, "Id", "Name", product.CategoryId);
    ViewBag.SupplierId = new SelectList(_context.Suppliers, "Id", "Name", product.SupplierId);

    return View(product);
}

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier) // 👈 thêm
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}