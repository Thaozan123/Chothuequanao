using System.ComponentModel.DataAnnotations;

namespace ChoThueQuanAo.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email hoặc Số điện thoại không được để trống")]
        // Xóa dòng [EmailAddress] cũ đi để nhập được cả số và chữ
        [Display(Name = "Email hoặc Số điện thoại")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";
    }
}