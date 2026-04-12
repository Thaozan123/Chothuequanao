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

            var model = products.Select(p => new ChoThueQuanAo.ViewModels.CartItemViewModel 
            {
                Product = p,
                Quantity = cartItems.Count(id => id == p.Id)
            }).ToList();

            return View(model);
        }

        // 🟢 Cập nhật số lượng
        [HttpPost]
        public IActionResult UpdateQuantity(int productId, int quantity)
        {
            var cart = HttpContext.Session.GetString("Cart");
            List<int> cartItems = string.IsNullOrEmpty(cart)
                ? new List<int>()
                : cart.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
            
            var product = _context.Products.Find(productId);
            if (product != null && quantity > product.StockQuantity)
            {
                quantity = product.StockQuantity;
            }

            cartItems.RemoveAll(id => id == productId);
            if (quantity > 0)
            {
                for(int i = 0; i < quantity; i++) cartItems.Add(productId);
            }
            HttpContext.Session.SetString("Cart", string.Join(",", cartItems));
            return RedirectToAction("Index");
        }

        // 🔴 Xóa sản phẩm
        public IActionResult RemoveFromCart(int productId)
        {
            var cart = HttpContext.Session.GetString("Cart");
            List<int> cartItems = string.IsNullOrEmpty(cart)
                ? new List<int>()
                : cart.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
            
            cartItems.RemoveAll(id => id == productId);
            HttpContext.Session.SetString("Cart", string.Join(",", cartItems));
            return RedirectToAction("Index");
        }

        // 🔥 3. XỬ LÝ THANH TOÁN (CHECKOUT)
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Checkout(int? promotionId, decimal discountAmount, int numberOfDays = 3)
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
                ExpectedReturnDate = DateTime.Now.AddDays(numberOfDays),
                Status = "PendingDeposit",
                RentalContractDetails = new List<RentalContractDetail>() 
            };

            decimal totalRentalPrice = 0;
            decimal totalDeposit = 0;

            // Bước D.2: Lặp qua từng món trong giỏ để tạo Chi tiết hợp đồng
            foreach (var productId in cartItems.Distinct())
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null) continue;

                int qty = cartItems.Count(id => id == productId);

                var detail = new RentalContractDetail
                {
                    ProductId = productId,
                    Quantity = qty,
                    SelectedSize = product.Size ?? "M",
                    NumberOfDays = numberOfDays,
                    SnapshotUnitPrice = product.RentalPricePerDay,
                    SubTotal = product.RentalPricePerDay * numberOfDays * qty
                };

                contract.RentalContractDetails.Add(detail);
                totalRentalPrice += detail.SubTotal;
                totalDeposit += product.Deposit * qty;
                
                // Trừ tồn kho
                product.StockQuantity -= qty;
                if (product.StockQuantity <= 0) 
                {
                    product.StockQuantity = 0;
                    product.Status = "Rented"; // Hoặc Hết Hàng
                }
                _context.Update(product);
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

            // Bước F: Điều hướng thẳng tới trang quét QR Đặt cọc
            return RedirectToAction("CheckoutQR", "Payment", new { contractId = contract.Id, type = "Deposit" });
        }

        // 🗑️ 4. Xóa sạch giỏ hàng
        public IActionResult ClearCart()
        {
            HttpContext.Session.Remove("Cart");
            return RedirectToAction("Index");
        }
    }
}