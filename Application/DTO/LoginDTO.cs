using System.ComponentModel.DataAnnotations;

namespace Application.DTO
{
    public class LoginDTO
    {
        [Required(ErrorMessage = "نام کاربری الزامی است.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "رمز عبور الزامی است.")]
        public string Password { get; set; }
    }
}