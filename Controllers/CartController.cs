using Microsoft.AspNetCore.Mvc;
using ChoThueQuanAo.Data;
using ChoThueQuanAo.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ChoThueQuanAo.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext _context;

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        // 🛒 1. Thêm sản phẩm vào giỏ hàng (Lưu trong Session)
        public IActionResult AddToCart(int productId)
        {
            var cart = HttpContext.Session.GetString("Cart");

            List<int> cartItems = string.IsNullOrEmpty(cart)
                ? new List<int>()
                : cart.Split(',').Select(int.Parse).ToList();

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
                : cart.Split(',').Select(int.Parse).ToList();

            var products = _context.Products
                .Where(p => cartItems.Contains(p.Id))
                .ToList();

            return View(products);
        }

        // 🔥 3. XỬ LÝ THANH TOÁN (CHECKOUT) - ĐÃ SỬA LỖI ID
        [Authorize] // Bắt buộc khách phải đăng nhập mới được thuê đồ
        public async Task<IActionResult> Checkout()
        {
            // Bước A: Lấy giỏ hàng từ Session
            var cart = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(cart))
            {
                return RedirectToAction("Index", "Product");
            }

            // Bước B: Lấy ID của người dùng đang đăng nhập thực tế
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }
            int currentUserId = int.Parse(userIdClaim);

            // Bước C: Chuyển chuỗi ID từ Session thành List
            List<int> cartItems = cart.Split(',').Select(int.Parse).ToList();

            // Bước D: Tạo hợp đồng dựa trên ID người dùng thật
            foreach (var productId in cartItems)
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null) continue;

                var contract = new RentalContract
                {
                    ContractCode = "HD" + DateTime.Now.Ticks, // Mã hóa đơn duy nhất
                    CustomerId = currentUserId,               // SỬA TỪ 1 THÀNH ID THẬT NÈ VÂN
                    CreatedAt = DateTime.Now,
                    Status = "PendingDeposit",                // Trạng thái chờ đặt cọc
                    StartDate = DateTime.Now,
                    ExpectedReturnDate = DateTime.Now.AddDays(3),
                    TotalAmount = product.RentalPricePerDay * 3, // Giả sử mặc định thuê 3 ngày
                    DepositRequired = product.Deposit
                };

                _context.RentalContracts.Add(contract);
            }

            await _context.SaveChangesAsync();

            // 🧹 Bước E: Xóa giỏ hàng sau khi đặt thành công
            HttpContext.Session.Remove("Cart");

            // Chuyển khách về trang "Hóa đơn của tôi" để họ xem đơn vừa đặt
            return RedirectToAction("MyContracts", "RentalContract");
        }

        // 🗑️ 4. Xóa giỏ hàng (Nếu cần)
        public IActionResult ClearCart()
        {
            HttpContext.Session.Remove("Cart");
            return RedirectToAction("Index");
        }
    }
}