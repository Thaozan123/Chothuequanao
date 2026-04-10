using Microsoft.AspNetCore.Mvc;
using ChoThueQuanAo.Data;
using ChoThueQuanAo.Models;

namespace ChoThueQuanAo.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext _context;

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        // 🛒 Thêm vào giỏ
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

        // 🛒 Xem giỏ (HIỂN THỊ SẢN PHẨM THẬT)
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

        // 🔥 THUÊ TẤT CẢ
        public IActionResult Checkout()
        {
            var cart = HttpContext.Session.GetString("Cart");

            List<int> cartItems = string.IsNullOrEmpty(cart)
                ? new List<int>()
                : cart.Split(',').Select(int.Parse).ToList();

            foreach (var productId in cartItems)
            {
                var contract = new RentalContract
                {
                    ContractCode = "HD" + DateTime.Now.Ticks,
                    CustomerId = 1,
                    CreatedAt = DateTime.Now,
                    Status = "PendingDeposit",
                    StartDate = DateTime.Now,
                    ExpectedReturnDate = DateTime.Now.AddDays(3)
                };

                _context.RentalContracts.Add(contract);
            }

            _context.SaveChanges();

            // 🧹 clear giỏ
            HttpContext.Session.Remove("Cart");

            return RedirectToAction("Index", "RentalContract");
        }
    }
}