using ChoThueQuanAo.Data;
using ChoThueQuanAo.Models;
using ChoThueQuanAo.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChoThueQuanAo.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        public IActionResult Register() => View();

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Kiểm tra trùng lặp Email hoặc Số điện thoại
            var existingUser = await _context.Users
                .AnyAsync(x => x.Email == model.Email || x.Phone == model.Phone);

            if (existingUser)
            {
                ModelState.AddModelError("", "Email hoặc Số điện thoại này đã được sử dụng.");
                return View(model);
            }

            var user = new User
            {
                FullName = model.FullName,
                Email = model.Email,
                Phone = model.Phone,
                Address = model.Address,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = "Customer", // Mặc định khi đăng ký luôn là Khách hàng
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đăng ký thành công. Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        [AllowAnonymous]
        public IActionResult Login() => View();

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Tìm user theo Email hoặc Số điện thoại
            var user = await _context.Users
                .FirstOrDefaultAsync(x => (x.Email == model.Email || x.Phone == model.Email) && x.IsActive);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Thông tin đăng nhập không chính xác hoặc tài khoản bị khóa.");
                return View(model);
            }

            // --- THIẾT LẬP PHÂN QUYỀN (CLAIMS) ---
            var claims = new List<Claim>
            {
                // NameIdentifier lưu ID để các Controller khác bốc ra dùng (Sửa lỗi kẹt ID số 1)
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), 
                new Claim(ClaimTypes.Name, user.FullName ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, user.Role) // Chỉ còn Admin hoặc Customer
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
            var principal = new ClaimsPrincipal(identity);

            // Xóa phiên cũ trước khi đăng nhập mới
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true, // Ghi nhớ đăng nhập
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                }
            );

            // --- ĐIỀU HƯỚNG THEO QUYỀN MỚI ---
            if (user.Role == "Admin")
            {
                // Admin nắm toàn quyền, vào thẳng trang quản lý hợp đồng tổng
                return RedirectToAction("Index", "RentalContract");
            }

            // Khách hàng (Customer) về trang chủ mua sắm
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [AllowAnonymous]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _context.Users
                .FirstOrDefaultAsync(x => (x.Email == model.Email || x.Phone == model.Email) && x.IsActive);

            if (user == null)
            {
                ModelState.AddModelError("", "Không tìm thấy tài khoản.");
                return View(model);
            }

            // Cập nhật mật khẩu mới (đã hash)
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đổi mật khẩu thành công. Hãy đăng nhập lại.";
            return RedirectToAction("Login");
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            // Trang hiển thị khi Customer cố tình vào link của Admin
            return View();
        }
    }
}