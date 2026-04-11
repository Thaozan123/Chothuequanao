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
        // KHÁCH HÀNG: ĐẶT THUÊ (CHECKOUT)
        // ==========================================================
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null || product.Status != "Available") 
            {
                TempData["Error"] = "Sản phẩm không tồn tại hoặc tạm hết hàng.";
                return RedirectToAction("Index", "Product");
            }

            var model = new ChoThueQuanAo.ViewModels.CheckoutViewModel
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductImageUrl = product.ImageUrl,
                RentalPricePerDay = product.RentalPricePerDay,
                Deposit = product.Deposit,
                NumberOfDays = 3 // Mặc định 3 ngày
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ChoThueQuanAo.ViewModels.CheckoutViewModel model)
        {
            var product = await _context.Products.FindAsync(model.ProductId);
            if (product == null) return NotFound();

            if (!ModelState.IsValid)
            {
                // Nạp lại thông tin hiển thị nếu lỗi
                model.ProductName = product.Name;
                model.ProductImageUrl = product.ImageUrl;
                model.RentalPricePerDay = product.RentalPricePerDay;
                model.Deposit = product.Deposit;
                return View(model);
            }

            // BẮT ĐẦU TRANSACTION
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Lấy mã User
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int userId = int.Parse(userIdStr);

                // 2. Tính toán tiền nong (Subtotal, Cọc) chuẩn từ DB (Không tin tưởng phía Client JS)
                decimal calculatedSubTotal = product.RentalPricePerDay * model.NumberOfDays;
                decimal calculatedDeposit = product.Deposit;
                decimal discountAmount = 0;
                int? promotionId = null;

                // 3. Áp dụng Khuyến mãi (nếu khách nhập)
                if (!string.IsNullOrEmpty(model.PromoCode))
                {
                    var promo = await _context.Promotions
                        .FirstOrDefaultAsync(p => p.Code.ToLower() == model.PromoCode.ToLower() && p.IsActive && p.EndDate >= DateTime.Now);
                    
                    if (promo != null && calculatedSubTotal >= promo.MinOrderAmount)
                    {
                        promotionId = promo.Id;
                        if (promo.DiscountType == "Percent")
                            discountAmount = calculatedSubTotal * (promo.DiscountValue / 100);
                        else
                            discountAmount = promo.DiscountValue;
                    }
                }

                decimal totalAmount = calculatedSubTotal - discountAmount;
                if (totalAmount < 0) totalAmount = 0; // Chống lỗi âm tiền

                // 4. Tạo Hợp Đồng Mới
                var contract = new RentalContract
                {
                    ContractCode = "RC" + DateTime.Now.ToString("yyMMddHHmmss"),
                    CustomerId = userId,
                    StartDate = DateTime.Now,
                    ExpectedReturnDate = DateTime.Now.AddDays(model.NumberOfDays),
                    Status = "PendingDeposit", // Chờ cọc
                    PromotionId = promotionId,
                    SubTotal = calculatedSubTotal,
                    DiscountAmount = discountAmount,
                    TotalAmount = totalAmount,
                    DepositRequired = calculatedDeposit,
                    DepositPaid = 0,
                    Notes = model.Note,
                    CreatedAt = DateTime.Now
                };

                _context.RentalContracts.Add(contract);
                await _context.SaveChangesAsync(); // Lưu để lấy contract.Id

                // 5. Tạo Chi tiết hợp đồng (Sản phẩm khách chọn)
                var contractDetail = new RentalContractDetail
                {
                    RentalContractId = contract.Id,
                    ProductId = product.Id,
                    SelectedSize = model.SelectedSize,
                    Quantity = 1,
                    NumberOfDays = model.NumberOfDays,
                    SnapshotUnitPrice = product.RentalPricePerDay,
                    SnapshotDeposit = product.Deposit,
                    SubTotal = calculatedSubTotal
                };

                _context.RentalContractDetails.Add(contractDetail);

                // 6. Trừ tồn kho (StockQuantity)
                product.StockQuantity -= 1;
                if (product.StockQuantity <= 0) product.Status = "Rented";
                _context.Products.Update(product);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // 7. Chuyển hướng thành công (Ví dụ: Chuyển qua trang hướng dẫn đặt cọc)
                TempData["Success"] = "Đặt hàng thành công! Vui lòng chuyển khoản tiền cọc để nhận hàng.";
                return RedirectToAction("CheckoutQR", "Payment", new { contractId = contract.Id, type = "Deposit" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Đã xảy ra lỗi hệ thống: " + ex.Message);

                // Nạp lại thông tin
                model.ProductName = product.Name;
                model.ProductImageUrl = product.ImageUrl;
                model.RentalPricePerDay = product.RentalPricePerDay;
                model.Deposit = product.Deposit;
                return View(model);
            }
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