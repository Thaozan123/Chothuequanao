using ChoThueQuanAo.Data;
using ChoThueQuanAo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization; // Thêm thư viện này để dùng Authorize

namespace ChoThueQuanAo.Controllers
{
    // Cấu hình để chỉ Admin và Staff mới có thể vào Controller này
    [Authorize(Roles = "Admin,Staff")] 
    public class CategoryController : Controller
    {
        private readonly AppDbContext _context;

        public CategoryController(AppDbContext context)
        {
            _context = context;
        }

        // 1. Xem danh sách - Cả Admin và Staff đều xem được
        public async Task<IActionResult> Index()
        {
            var categories = await _context.ProductCategories.ToListAsync();
            return View(categories);
        }

        // 2. GET: Create - Cả Admin và Staff
        public IActionResult Create()
        {
            return View();
        }

        // 3. POST: Create - Cả Admin và Staff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCategory category)
        {
            if (ModelState.IsValid)
            {
                _context.ProductCategories.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // 4. GET: Edit - Cả Admin và Staff
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.ProductCategories.FindAsync(id);
            if (category == null) return NotFound();
            return View(category);
        }

        // 5. POST: Edit - Cả Admin và Staff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductCategory category)
        {
            if (ModelState.IsValid)
            {
                _context.ProductCategories.Update(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // 6. GET: Delete 
        // Nếu bạn muốn CHỈ Admin mới được xóa, hãy sửa thành: [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.ProductCategories.FindAsync(id);
            if (category == null) return NotFound();
            return View(category);
        }

        // 7. POST: Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.ProductCategories.FindAsync(id);
            if (category != null)
            {
                _context.ProductCategories.Remove(category);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}