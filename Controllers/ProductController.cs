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
        // DÀNH CHO TẤT CẢ MỌI NGƯỜI (Xem hàng & Tìm kiếm)
        // ==========================================================

        [AllowAnonymous]
        public async Task<IActionResult> Index(string searchString)
        {
            // Lưu lại từ khóa tìm kiếm để hiển thị lại trên thanh tìm kiếm ở View
            ViewData["CurrentFilter"] = searchString;

            // Lấy danh sách sản phẩm kèm theo thông tin Danh mục
            var products = from p in _context.Products.Include(p => p.Category)
                           select p;

            // Thực hiện lọc nếu người dùng có nhập từ khóa
            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.Name.Contains(searchString) 
                                            || p.ProductCode.Contains(searchString));
            }

            return View(await products.ToListAsync());
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();
            return View(product);
        }

        // ==========================================================
        // CÁC CHỨC NĂNG QUẢN TRỊ - CHỈ DÀNH CHO ADMIN
        // ==========================================================

        // 1. GET: Tạo sản phẩm mới (Đã nạp danh mục để click chọn)
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            // Load danh sách danh mục vào ViewBag để dropdown ở View hiển thị được
            ViewBag.CategoryId = new SelectList(_context.ProductCategories, "Id", "Name");
            return View();
        }

        // 2. POST: Lưu sản phẩm mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Product product)
        {
            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            // Nếu có lỗi, nạp lại danh mục để dropdown không bị trống
            ViewBag.CategoryId = new SelectList(_context.ProductCategories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // 3. GET: Chỉnh sửa sản phẩm (Đã nạp danh mục để chọn lại)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            // Cực kỳ quan trọng: Nạp danh mục để dropdown "Sửa sản phẩm" hiển thị dữ liệu
            ViewBag.CategoryId = new SelectList(_context.ProductCategories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // 4. POST: Cập nhật sản phẩm
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
                    if (!_context.Products.Any(e => e.Id == product.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            // Nạp lại danh mục nếu lưu thất bại để tránh lỗi dropdown trống
            ViewBag.CategoryId = new SelectList(_context.ProductCategories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // 5. GET: Xóa sản phẩm
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();
            return View(product);
        }

        // 6. POST: Xác nhận xóa sản phẩm
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