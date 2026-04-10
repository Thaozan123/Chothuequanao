using ChoThueQuanAo.Data;
using ChoThueQuanAo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChoThueQuanAo.Controllers
{
    // Không để Authorize ở đây để Khách hàng có thể truy cập vào trang Index xem mã
    public class PromotionController : Controller
    {
        private readonly AppDbContext _context;

        public PromotionController(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================================
        // 1. TRANG DÀNH CHO KHÁCH HÀNG & ADMIN (Index chung)
        // ==========================================================
        public async Task<IActionResult> Index()
        {
            var now = DateTime.Now;

            // Nếu là Admin: Hiện toàn bộ mã để quản lý
            if (User.IsInRole("Admin"))
            {
                var allPromos = await _context.Promotions.OrderByDescending(p => p.Id).ToListAsync();
                return View("AdminIndex", allPromos); // Trỏ về View dành riêng cho Admin
            }

            // Nếu là Khách hàng: Chỉ hiện các mã đang còn hiệu lực và còn lượt dùng
            var activePromos = await _context.Promotions
                .Where(p => p.IsActive 
                         && p.StartDate <= now 
                         && p.EndDate >= now 
                         && p.UsedCount < p.UsageLimit)
                .OrderByDescending(p => p.DiscountValue)
                .ToListAsync();

            return View(activePromos); // Trỏ về View Card xinh xắn cho khách
        }

        // ==========================================================
        // 2. CÁC CHỨC NĂNG QUẢN LÝ (CHỈ ADMIN MỚI ĐƯỢC VÀO)
        // ==========================================================

        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View();

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Promotion promo)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Promotions.AnyAsync(x => x.Code == promo.Code))
                {
                    ModelState.AddModelError("Code", "Mã khuyến mãi này đã tồn tại.");
                    return View(promo);
                }

                _context.Add(promo);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(promo);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var promo = await _context.Promotions.FindAsync(id);
            if (promo == null) return NotFound();
            return View(promo);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Promotion promo)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(promo);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PromotionExists(promo.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(promo);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var promo = await _context.Promotions.FindAsync(id);
            if (promo != null)
            {
                _context.Promotions.Remove(promo);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool PromotionExists(int id)
        {
            return _context.Promotions.Any(e => e.Id == id);
        }
    }
}