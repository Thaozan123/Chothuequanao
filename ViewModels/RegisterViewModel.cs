using System.ComponentModel.DataAnnotations;

namespace ChoThueQuanAo.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Họ tên không được để trống")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string Email { get; set; } = "";

        public string? Phone { get; set; }

        public string? Address { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu nhập lại không khớp")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = "";
    }
}