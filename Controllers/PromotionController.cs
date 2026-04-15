using ChoThueQuanAo.Data;
using ChoThueQuanAo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChoThueQuanAo.Controllers
{
    public class PromotionController : Controller
    {
        private readonly AppDbContext _context;

        public PromotionController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var now = DateTime.Now;

            if (User.IsInRole("Admin"))
            {
                var allPromos = await _context.Promotions.OrderByDescending(p => p.Id).ToListAsync();
                return View("AdminIndex", allPromos);
            }

            // Cho phép xem voucher mà không cần đăng nhập
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int currentUserId = 0;
            bool isOldCustomer = false;

            if (int.TryParse(userIdClaim, out currentUserId))
            {
                isOldCustomer = await _context.RentalContracts.AnyAsync(c => c.CustomerId == currentUserId);
            }

            var query = _context.Promotions.Where(p => p.IsActive
                                                 && p.StartDate <= now
                                                 && p.EndDate >= now
                                                 && p.UsedCount < p.UsageLimit);

            if (isOldCustomer)
            {
                query = query.Where(p => !p.Code.ToUpper().Contains("WELCOME"));
            }

            var activePromos = await query.OrderByDescending(p => p.DiscountValue).ToListAsync();

            return View(activePromos);
        }

        [Authorize]
        public async Task<IActionResult> Available()
        {
            var now = DateTime.Now;

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out int currentUserId))
            {
                return Unauthorized();
            }

            bool isOldCustomer = await _context.RentalContracts.AnyAsync(c => c.CustomerId == currentUserId);

            var query = _context.Promotions.Where(p => p.IsActive
                                                 && p.StartDate <= now
                                                 && p.EndDate >= now
                                                 && p.UsedCount < p.UsageLimit);

            if (isOldCustomer)
            {
                query = query.Where(p => !p.Code.ToUpper().Contains("WELCOME"));
            }

            var activePromos = await query.OrderByDescending(p => p.DiscountValue).ToListAsync();
            return PartialView("_PromotionList", activePromos);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyVoucher(string code, decimal subTotal)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return Json(new { success = false, message = "Vui lòng nhập mã khuyến mãi." });
            }

            var now = DateTime.Now;
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out int currentUserId))
            {
                return Json(new { success = false, message = "Bạn chưa đăng nhập." });
            }

            bool isOldCustomer = await _context.RentalContracts.AnyAsync(c => c.CustomerId == currentUserId);

            var promo = await _context.Promotions
                .Where(p => p.IsActive
                         && p.Code.ToUpper() == code.Trim().ToUpper()
                         && p.StartDate <= now
                         && p.EndDate >= now
                         && p.UsedCount < p.UsageLimit)
                .FirstOrDefaultAsync();

            if (promo == null || (isOldCustomer && promo.Code.ToUpper().Contains("WELCOME")))
            {
                return Json(new { success = false, message = "Mã không hợp lệ hoặc không áp dụng cho bạn." });
            }

            if (promo.MinOrderAmount > subTotal)
            {
                return Json(new { success = false, message = $"Đơn hàng chưa đủ tối thiểu {promo.MinOrderAmount:N0} đ để áp dụng mã này." });
            }

            return Json(new
            {
                success = true,
                message = "Áp dụng mã thành công.",
                promoId = promo.Id,
                code = promo.Code,
                discountValue = promo.DiscountValue,
                discountType = promo.DiscountType,
                minOrderAmount = promo.MinOrderAmount
            });
        }

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