using System.ComponentModel.DataAnnotations;

namespace ChoThueQuanAo.ViewModels
{
    public class CheckoutViewModel
    {
        [Required]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Size")]
        public string SelectedSize { get; set; } = "";

        [Required]
        [Range(1, 30, ErrorMessage = "Số ngày thuê phải từ {1} đến {2} ngày")]
        public int NumberOfDays { get; set; } = 3;

        public DateTime ExpectedReturnDate => DateTime.Now.AddDays(NumberOfDays);

        public string? Note { get; set; }

        // MÃ KHUYẾN MÃI
        public string? PromoCode { get; set; }

        // THÔNG TIN HIỂN THỊ (Không bắt buộc post lên)
        public string? ProductName { get; set; }
        public string? ProductImageUrl { get; set; }
        public decimal RentalPricePerDay { get; set; }
        public decimal Deposit { get; set; }
        public decimal SubTotal => RentalPricePerDay * NumberOfDays;
    }
}
