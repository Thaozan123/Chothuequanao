using ChoThueQuanAo.Data;
using ChoThueQuanAo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims; // Cần thêm dòng này để lấy User ID

namespace ChoThueQuanAo.Controllers
{
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
                return View("AdminIndex", allPromos);
            }

            // --- LOGIC LỌC KHÁCH HÀNG MỚI CHO VÂN ---
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int currentUserId = int.Parse(userIdClaim ?? "0");

            // Kiểm tra khách đã có hợp đồng nào chưa
            bool isOldCustomer = await _context.RentalContracts.AnyAsync(c => c.CustomerId == currentUserId);

            // Bắt đầu truy vấn lọc mã
            var query = _context.Promotions.Where(p => p.IsActive 
                                                 && p.StartDate <= now 
                                                 && p.EndDate >= now 
                                                 && p.UsedCount < p.UsageLimit);

            // Nếu là khách cũ (đã từng thuê), lọc bỏ các mã có chữ "WELCOME"
            if (isOldCustomer)
            {
                query = query.Where(p => !p.Code.ToUpper().Contains("WELCOME"));
            }

            var activePromos = await query.OrderByDescending(p => p.DiscountValue).ToListAsync();

            return View(activePromos);
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
                // Kiểm tra mã trùng
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