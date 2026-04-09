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
        // DÀNH CHO STAFF / ADMIN
        // ==========================================================

        // STAFF / ADMIN xem toàn bộ danh sách hợp đồng
        [Authorize(Roles = "Staff,Admin")]
        public async Task<IActionResult> Index()
        {
            var contracts = await _context.RentalContracts
                .Include(r => r.Customer)
                .Include(r => r.Staff)
                .Include("RentalContractDetails.Product")
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(contracts);
        }

        // STAFF / ADMIN xem chi tiết một hợp đồng bất kỳ
        [Authorize(Roles = "Staff,Admin")]
        public async Task<IActionResult> Details(int id)
        {
            var contract = await _context.RentalContracts
                .Include(r => r.Customer)
                .Include(r => r.Staff)
                .Include("RentalContractDetails.Product")
                .FirstOrDefaultAsync(r => r.Id == id);

            if (contract == null)
                return NotFound();

            return View(contract);
        }

        // ==========================================================
        // DÀNH CHO CUSTOMER (KHÁCH HÀNG)
        // ==========================================================

        // CUSTOMER xem danh sách hợp đồng của chính họ
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> MyContracts()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            int userId = int.Parse(userIdClaim);

            var contracts = await _context.RentalContracts
                .Include("RentalContractDetails.Product")
                .Where(r => r.CustomerId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(contracts);
        }

        // CUSTOMER xem chi tiết hợp đồng của chính họ
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> MyDetails(int id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            int userId = int.Parse(userIdClaim);

            var contract = await _context.RentalContracts
                .Include(r => r.Customer)
                .Include("RentalContractDetails.Product")
                .FirstOrDefaultAsync(r => r.Id == id && r.CustomerId == userId);

            if (contract == null)
                return NotFound();

            return View(contract);
        }

        // CUSTOMER khởi tạo đơn hàng/hợp đồng mới
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
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            int userId = int.Parse(userIdClaim);

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                return NotFound();

            // Tính toán số ngày thuê
            int days = (expectedReturnDate.Date - startDate.Date).Days;
            if (days <= 0)
            {
                ModelState.AddModelError("", "Ngày trả dự kiến phải sau ngày bắt đầu thuê ít nhất 1 ngày.");
                ViewBag.ProductId = productId;
                return View();
            }

            // 1. Khởi tạo đối tượng Hợp đồng (RentalContract)
            var contract = new RentalContract
            {
                ContractCode = "RC" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                CustomerId = userId,
                ContractType = "Online",
                StartDate = startDate,
                ExpectedReturnDate = expectedReturnDate,
                Status = "PendingDeposit", // Chờ đặt cọc
                SubTotal = product.RentalPricePerDay * days,
                DiscountAmount = 0,
                ShippingFee = 0,
                TotalAmount = product.RentalPricePerDay * days,
                DepositRequired = product.Deposit,
                DepositPaid = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.RentalContracts.Add(contract);
            await _context.SaveChangesAsync(); // Lưu để lấy ID của contract

            // 2. Khởi tạo Chi tiết hợp đồng (RentalContractDetail)
            var detail = new RentalContractDetail
            {
                RentalContractId = contract.Id,
                ProductId = product.Id,
                SelectedSize = product.Size, // Hoặc lấy từ Form nếu cho chọn Size
                Quantity = 1,
                NumberOfDays = days,
                SnapshotUnitPrice = product.RentalPricePerDay,
                SnapshotDeposit = product.Deposit,
                SubTotal = product.RentalPricePerDay * days
            };

            _context.RentalContractDetails.Add(detail);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyContracts));
        }

        // ==========================================================
        // QUẢN LÝ HỆ THỐNG (ADMIN)
        // ==========================================================

        // Giao diện xác nhận xóa hợp đồng
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var contract = await _context.RentalContracts
                .Include(r => r.Customer)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (contract == null)
                return NotFound();

            return View(contract);
        }

        // Thực thi xóa hợp đồng
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var contract = await _context.RentalContracts.FindAsync(id);
            if (contract != null)
            {
                // Lưu ý: Nếu có ràng buộc khóa ngoại mà không để Cascade Delete, 
                // bạn cần xóa RentalContractDetails trước.
                _context.RentalContracts.Remove(contract);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}