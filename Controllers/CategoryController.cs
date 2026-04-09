using ChoThueQuanAo.Data;
using ChoThueQuanAo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChoThueQuanAo.Controllers
{
    public class CategoryController : Controller
    {
        private readonly AppDbContext _context;

        public CategoryController(AppDbContext context)
        {
            _context = context;
        }

        // Danh sách
        public async Task<IActionResult> Index()
        {
            var categories = await _context.ProductCategories.ToListAsync();
            return View(categories);
        }

        // GET: Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Create
        [HttpPost]
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

        // GET: Edit
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.ProductCategories.FindAsync(id);
            if (category == null) return NotFound();
            return View(category);
        }

        // POST: Edit
        [HttpPost]
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

        // GET: Delete
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.ProductCategories.FindAsync(id);
            if (category == null) return NotFound();
            return View(category);
        }

        // POST: Delete
        [HttpPost, ActionName("Delete")]
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