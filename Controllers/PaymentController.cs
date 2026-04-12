using Microsoft.AspNetCore.Mvc;
using ChoThueQuanAo.Data;
using ChoThueQuanAo.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ChoThueQuanAo.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly AppDbContext _context;

        public PaymentController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Hiển thị trang quét mã QR thanh toán
        public async Task<IActionResult> CheckoutQR(int contractId, string type = "Deposit")
        {
            var contract = await _context.RentalContracts.FindAsync(contractId);
            if (contract == null) return NotFound();

            // Tính số tiền cần thanh toán
            decimal amountToPay = 0;
            string description = "";

            if (type == "Deposit" && contract.Status == "PendingDeposit")
            {
                amountToPay = contract.DepositRequired - contract.DepositPaid;
                description = $"COC HD {contract.ContractCode}";
            }
            else if (type == "RentalFee" && contract.Status == "Active")
            {
                amountToPay = contract.TotalAmount; // Hoặc thêm logic khấu trừ cọc
                description = $"TT HD {contract.ContractCode}";
            }
            else
            {
                // Trạng thái không hợp lệ để thanh toán
                return RedirectToAction("Details", "RentalContract", new { id = contractId });
            }

            if (amountToPay <= 0)
            {
                // Đã thanh toán đủ
                return RedirectToAction("Details", "RentalContract", new { id = contractId });
            }

            ViewBag.PaymentType = type;
            ViewBag.Amount = amountToPay;
            ViewBag.Description = description;
            
            return View(contract);
        }

        // POST: Xác nhận đã nhận tiền
        [HttpPost]
        public async Task<IActionResult> ConfirmPayment(int contractId, string paymentType, decimal amount, string transactionCode = "")
        {
            var contract = await _context.RentalContracts.FindAsync(contractId);
            if (contract == null) return NotFound();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var payment = new Payment
                {
                    RentalContractId = contract.Id,
                    PaymentType = paymentType,
                    Amount = amount,
                    PaymentMethod = "Banking",
                    Status = "Completed",
                    TransactionCode = transactionCode,
                    PaymentDate = DateTime.Now
                };

                if (User.FindFirstValue(ClaimTypes.NameIdentifier) is string userIdStr && int.TryParse(userIdStr, out int userId))
                {
                    payment.CreatedBy = userId;
                }

                _context.Payments.Add(payment);

                if (paymentType == "Deposit")
                {
                    contract.DepositPaid += amount;
                    if (contract.DepositPaid >= contract.DepositRequired)
                    {
                        contract.Status = "Active";
                    }
                }
                else if (paymentType == "RentalFee")
                {
                    contract.Status = "Settled";
                    contract.ActualReturnDate = DateTime.Now;
                }

                _context.Update(contract);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = $"Thanh toán {paymentType} thành công!";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Lỗi xử lý thanh toán: " + ex.Message;
            }

            return RedirectToAction("PaymentSuccess", new { contractId = contract.Id });
        }

        // GET: Hiển thị trang báo thành công
        public IActionResult PaymentSuccess(int contractId)
        {
            ViewBag.ContractId = contractId;
            return View();
        }
    }
}
