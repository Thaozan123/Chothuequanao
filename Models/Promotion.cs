using System.ComponentModel.DataAnnotations;

namespace ChoThueQuanAo.Models
{
    public class Promotion
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên chương trình không được để trống")]
        [Display(Name = "Tên chương trình")]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Mã không được để trống")]
        [Display(Name = "Mã giảm giá")]
        public string Code { get; set; } = "";

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Loại giảm giá")]
        public string DiscountType { get; set; } = "Percent";

        [Required]
        [Display(Name = "Giá trị giảm")]
        public decimal DiscountValue { get; set; }

        [Display(Name = "Đơn tối thiểu")]
        public decimal MinOrderAmount { get; set; } = 0;

        [Required]
        [Display(Name = "Ngày bắt đầu")]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Ngày kết thúc")]
        public DateTime EndDate { get; set; } = DateTime.Now.AddDays(7);

        [Display(Name = "Giới hạn dùng")]
        public int UsageLimit { get; set; } = 100;

        [Display(Name = "Đã dùng")]
        public int UsedCount { get; set; } = 0;

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; } = true;
    }
}