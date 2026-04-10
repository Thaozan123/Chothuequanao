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

            // BẮT ĐẦU TRANSACTION ĐỂ GIỮ DỮ LIỆU AN TOÀN
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
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
                    Role = "Customer",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                
                // COMMIT: Xác nhận ghi vào database vĩnh viễn
                await transaction.CommitAsync();

                TempData["Success"] = "Đăng ký thành công. Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            catch (Exception)
            {
                // ROLLBACK: Nếu lỗi (mất điện, sập mạng) thì hủy bỏ, không tạo user rác
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Đã xảy ra lỗi hệ thống khi đăng ký.");
                return View(model);
            }
        }

        [AllowAnonymous]
        public IActionResult Login() => View();

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _context.Users
                .FirstOrDefaultAsync(x => (x.Email == model.Email || x.Phone == model.Email) && x.IsActive);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Thông tin đăng nhập không chính xác hoặc tài khoản bị khóa.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), 
                new Claim(ClaimTypes.Name, user.FullName ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true, 
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                }
            );

            if (user.Role == "Admin")
            {
                return RedirectToAction("Index", "RentalContract");
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
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // TRANSACTION CHO VIỆC ĐỔI MẬT KHẨU
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(x => (x.Email == model.Email || x.Phone == model.Email) && x.IsActive);

                if (user == null)
                {
                    ModelState.AddModelError("", "Không tìm thấy tài khoản.");
                    return View(model);
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                
                // XÁC NHẬN THAY ĐỔI
                await transaction.CommitAsync();

                TempData["Success"] = "Đổi mật khẩu thành công. Hãy đăng nhập lại.";
                return RedirectToAction("Login");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Lỗi khi cập nhật mật khẩu.");
                return View(model);
            }
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}