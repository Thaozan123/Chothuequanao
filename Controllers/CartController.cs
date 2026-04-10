using Microsoft.AspNetCore.Mvc;
using ChoThueQuanAo.Data;
using ChoThueQuanAo.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace ChoThueQuanAo.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext _context;

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        // 🛒 1. Thêm sản phẩm vào giỏ hàng
        public IActionResult AddToCart(int productId)
        {
            var cart = HttpContext.Session.GetString("Cart");
            List<int> cartItems = string.IsNullOrEmpty(cart)
                ? new List<int>()
                : cart.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();

            cartItems.Add(productId);
            HttpContext.Session.SetString("Cart", string.Join(",", cartItems));

            return RedirectToAction("Index", "Product");
        }

        // 🛒 2. Xem danh sách giỏ hàng
        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetString("Cart");
            List<int> cartItems = string.IsNullOrEmpty(cart)
                ? new List<int>()
                : cart.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();

            var products = _context.Products
                .Where(p => cartItems.Contains(p.Id))
                .ToList();

            return View(products);
        }

        // 🔥 3. XỬ LÝ THANH TOÁN (CHECKOUT)
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Checkout(int? promotionId, decimal discountAmount)
        {
            // Bước A: Lấy giỏ hàng từ Session
            var cart = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(cart))
            {
                return RedirectToAction("Index", "Product");
            }

            // Bước B: Lấy ID người dùng thực tế
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int currentUserId = int.Parse(userIdClaim ?? "0");

            // --- KIỂM TRA MÃ GIẢM GIÁ (DÀNH CHO KHÁCH MỚI) ---
            if (promotionId.HasValue)
            {
                var promo = await _context.Promotions.FindAsync(promotionId.Value);
                if (promo != null && promo.Code.ToUpper().Contains("WELCOME"))
                {
                    // Kiểm tra xem khách đã từng thuê món nào chưa
                    bool hasOrderedBefore = await _context.RentalContracts.AnyAsync(c => c.CustomerId == currentUserId);
                    if (hasOrderedBefore)
                    {
                        TempData["Error"] = "Mã WELCOME chỉ dành cho khách hàng mới lần đầu sử dụng!";
                        return RedirectToAction("Index");
                    }
                }
            }

            // Bước C: Chuyển chuỗi ID thành List
            List<int> cartItems = cart.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();

            // Bước D.1: Tạo đối tượng Hợp đồng tổng
            var contract = new RentalContract
            {
                ContractCode = "HD" + DateTime.Now.Ticks,
                CustomerId = currentUserId,
                PromotionId = promotionId,
                DiscountAmount = discountAmount,
                CreatedAt = DateTime.Now,
                StartDate = DateTime.Now,
                ExpectedReturnDate = DateTime.Now.AddDays(3),
                Status = "PendingDeposit",
                RentalContractDetails = new List<RentalContractDetail>() 
            };

            decimal totalRentalPrice = 0;
            decimal totalDeposit = 0;

            // Bước D.2: Lặp qua từng món trong giỏ để tạo Chi tiết hợp đồng
            foreach (var productId in cartItems)
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null) continue;

                var detail = new RentalContractDetail
                {
                    ProductId = productId,
                    Quantity = 1,
                    SelectedSize = product.Size ?? "M",
                    NumberOfDays = 3,
                    SnapshotUnitPrice = product.RentalPricePerDay,
                    SubTotal = product.RentalPricePerDay * 3
                };

                contract.RentalContractDetails.Add(detail);
                totalRentalPrice += detail.SubTotal;
                totalDeposit += product.Deposit;
            }

            contract.SubTotal = totalRentalPrice;
            contract.TotalAmount = totalRentalPrice - discountAmount;
            contract.DepositRequired = totalDeposit;

            // Lưu hợp đồng chính
            _context.RentalContracts.Add(contract);

            // Bước D.3: Cập nhật lượt dùng mã giảm giá
            if (promotionId.HasValue)
            {
                var promo = await _context.Promotions.FindAsync(promotionId.Value);
                if (promo != null)
                {
                    promo.UsedCount += 1;
                    _context.Update(promo);
                }
            }

            await _context.SaveChangesAsync();

            // 🧹 Bước E: Xóa giỏ hàng
            HttpContext.Session.Remove("Cart");

            return RedirectToAction("MyContracts", "RentalContract");
        }

        // 🗑️ 4. Xóa sạch giỏ hàng
        public IActionResult ClearCart()
        {
            HttpContext.Session.Remove("Cart");
            return RedirectToAction("Index");
        }
    }
}