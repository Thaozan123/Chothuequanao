using ChoThueQuanAo.Data;
using ChoThueQuanAo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

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
        // QUYỀN ADMIN: QUẢN LÝ TOÀN BỘ HỆ THỐNG
        // ==========================================================

        // Xem tất cả hóa đơn của mọi khách hàng
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var contracts = await _context.RentalContracts
                .Include(r => r.Customer)
                .Include(r => r.RentalContractDetails!)
                    .ThenInclude(d => d.Product)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(contracts);
        }

        // Xem chi tiết bất kỳ hóa đơn nào
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int id)
        {
            var contract = await _context.RentalContracts
                .Include(r => r.Customer)
                .Include(r => r.RentalContractDetails!)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (contract == null) return NotFound();

            return View(contract);
        }

        // Xóa hóa đơn (Chỉ Admin mới có quyền này)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var contract = await _context.RentalContracts
                .Include(r => r.Customer)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (contract == null) return NotFound();

            return View(contract);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var contract = await _context.RentalContracts.FindAsync(id);
            if (contract != null)
            {
                _context.RentalContracts.Remove(contract);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // ==========================================================
        // QUYỀN CUSTOMER: CHỈ XEM VÀ QUẢN LÝ CỦA CÁ NHÂN
        // ==========================================================

        // Xem danh sách hóa đơn của chính mình (ĐÃ SỬA LỖI ID)
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> MyContracts()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            int currentUserId = int.Parse(userIdClaim);

            var contracts = await _context.RentalContracts
                .Include(r => r.RentalContractDetails!)
                    .ThenInclude(d => d.Product)
                .Where(r => r.CustomerId == currentUserId) // Lọc đúng ID người dùng đang đăng nhập
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(contracts);
        }

        // Xem chi tiết hóa đơn cá nhân (Bảo mật: Không xem được đơn của người khác)
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> MyDetails(int id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            int currentUserId = int.Parse(userIdClaim);

            var contract = await _context.RentalContracts
                .Include(r => r.RentalContractDetails!)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(r => r.Id == id && r.CustomerId == currentUserId);

            if (contract == null) return NotFound();

            return View(contract);
        }

        // Tạo hóa đơn thuê đồ (Dành cho Customer)
        [Authorize(Roles = "Customer")]
        public IActionResult Create(int productId)
        {
            ViewBag.ProductId = productId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create(int productId, DateTime startDate, DateTime expectedReturnDate)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            int userId = int.Parse(userIdClaim);
            var product = await _context.Products.FindAsync(productId);

            if (product == null) return NotFound();

            int days = (expectedReturnDate.Date - startDate.Date).Days;
            if (days <= 0)
            {
                ModelState.AddModelError("", "Ngày trả phải sau ngày thuê.");
                ViewBag.ProductId = productId;
                return View();
            }

            var contract = new RentalContract
            {
                ContractCode = "RC" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                CustomerId = userId, // Tự động gán ID từ người đăng nhập
                StartDate = startDate,
                ExpectedReturnDate = expectedReturnDate,
                Status = "PendingDeposit",
                TotalAmount = product.RentalPricePerDay * days,
                DepositRequired = product.Deposit,
                CreatedAt = DateTime.Now
            };

            _context.RentalContracts.Add(contract);
            await _context.SaveChangesAsync();

            var detail = new RentalContractDetail
            {
                RentalContractId = contract.Id,
                ProductId = product.Id,
                Quantity = 1,
                NumberOfDays = days,
                SnapshotUnitPrice = product.RentalPricePerDay,
                SubTotal = product.RentalPricePerDay * days
            };

            _context.RentalContractDetails.Add(detail);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyContracts));
        }
    }
}