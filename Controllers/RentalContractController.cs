using ChoThueQuanAo.Data;
using ChoThueQuanAo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChoThueQuanAo.Controllers
{
    public class RentalContractController : Controller
    {
        private readonly AppDbContext _context;

        public RentalContractController(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================================
        // 1. DÀNH CHO ADMIN: Xem toàn bộ hợp đồng hệ thống
        // ==========================================================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var contracts = await _context.RentalContracts
                .Include(r => r.Customer)
                .Include(r => r.Promotion)
                // BỔ SUNG: Include sản phẩm để trang Index cũng có thể hiện tên đồ nếu cần
                .Include(r => r.RentalContractDetails!)
                    .ThenInclude(d => d.Product)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(contracts);
        }

        // ==========================================================
        // 2. DÀNH CHO KHÁCH HÀNG: Xem hợp đồng của cá nhân
        // ==========================================================
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> MyContracts()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            int userId = int.Parse(userIdClaim);

            var contracts = await _context.RentalContracts
                .Include(r => r.Promotion)
                .Include(r => r.RentalContractDetails!) // BỔ SUNG: Nạp chi tiết sản phẩm
                    .ThenInclude(d => d.Product)       // BỔ SUNG: Nạp thông tin sản phẩm
                .Where(r => r.CustomerId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View("Index", contracts);
        }

        // ==========================================================
        // 3. CHI TIẾT HỢP ĐỒNG (Fix lỗi không hiện tên sản phẩm)
        // ==========================================================
        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            var contract = await _context.RentalContracts
                .Include(r => r.Customer)
                .Include(r => r.Promotion)
                .Include(r => r.RentalContractDetails!)
                    .ThenInclude(d => d.Product) // Đã có: Đảm bảo hiện tên sản phẩm
                .FirstOrDefaultAsync(m => m.Id == id);

            if (contract == null) return NotFound();

            if (!User.IsInRole("Admin"))
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || contract.CustomerId != int.Parse(userIdClaim)) 
                    return Forbid();
            }

            return View(contract);
        }

        // ==========================================================
        // 4. LOGIC TÍNH TOÁN (Khuyến mãi & Ngày trả)
        // ==========================================================
        
        [HttpPost]
        public async Task<IActionResult> ApplyDiscount(string promoCode, decimal subTotal)
        {
            var promo = await _context.Promotions
                .FirstOrDefaultAsync(p => p.Code.ToLower() == promoCode.ToLower() && p.IsActive);

            if (promo == null) return Json(new { success = false, message = "Mã không tồn tại hoặc đã hết hạn." });
            
            // Tính số tiền giảm dựa trên loại giảm giá (Phần trăm hoặc Tiền mặt)
            decimal discount = 0;
            if (promo.DiscountType == "Percent")
            {
                discount = subTotal * (promo.DiscountValue / 100);
            }
            else
            {
                discount = promo.DiscountValue;
            }
            
            return Json(new { success = true, discountAmount = discount, promoId = promo.Id });
        }

        // Hàm hỗ trợ tính ngày trả dự kiến (Dùng để tư vấn cho khách)
        public IActionResult GetExpectedReturnDate(int days = 3)
        {
            var returnDate = DateTime.Now.AddDays(days);
            return Json(new { returnDate = returnDate.ToString("dd/MM/yyyy") });
        }
    }
}