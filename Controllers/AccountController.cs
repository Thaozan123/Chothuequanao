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
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Kiểm tra trùng lặp cả Email và Số điện thoại
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
                Phone = model.Phone, // Lưu SĐT để sau này đăng nhập
                Address = model.Address,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = "Customer", // Mặc định là khách hàng
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đăng ký thành công. Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // LOGIC QUAN TRỌNG: Tìm user bằng Email HOẶC Số điện thoại
            var user = await _context.Users
                .FirstOrDefaultAsync(x => (x.Email == model.Email || x.Phone == model.Email) && x.IsActive);

            if (user == null)
            {
                ModelState.AddModelError("", "Tài khoản không tồn tại hoặc đã bị khóa.");
                return View(model);
            }

            // Kiểm tra mật khẩu mã hóa BCrypt
            bool isValidPassword = BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash);

            if (!isValidPassword)
            {
                ModelState.AddModelError("", "Thông tin đăng nhập không chính xác.");
                return View(model);
            }

            // Tạo danh sách Claim (Thông tin định danh)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim("Phone", user.Phone ?? ""),
                new Claim(ClaimTypes.Role, user.Role) // Lưu Role để phân quyền [Authorize(Roles="...")]
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // Đăng nhập vào hệ thống (Lưu Cookie)
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true, // Ghi nhớ đăng nhập
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) // Hết hạn sau 7 ngày
                }
            );

            // Phân hướng trang sau khi đăng nhập thành công
            if (user.Role == "Admin" || user.Role == "Staff")
            {
                // Nếu là Admin/Staff thì vào trang quản lý (tùy bạn đặt tên Controller)
                return RedirectToAction("Index", "Category"); 
            }

            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Cho phép tìm tài khoản để lấy lại MK bằng Email hoặc SĐT
            var user = await _context.Users
                .FirstOrDefaultAsync(x => (x.Email == model.Email || x.Phone == model.Email) && x.IsActive);

            if (user == null)
            {
                ModelState.AddModelError("", "Không tìm thấy tài khoản tương ứng.");
                return View(model);
            }

            // Cập nhật mật khẩu mới
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đổi mật khẩu thành công. Hãy đăng nhập lại.";
            return RedirectToAction("Login");
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}