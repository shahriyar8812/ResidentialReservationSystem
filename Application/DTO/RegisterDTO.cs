using System.ComponentModel.DataAnnotations;

namespace Application.DTO
{
    public class RegisterDTO
    {
        [Required(ErrorMessage = "نام کاربری الزامی است.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "رمز عبور الزامی است")]
        public string Password { get; set; }

        [Required(ErrorMessage = "نام و نام خانوادگی الزامی میباشند.")]
        public string FullName { get; set; }
    }
}