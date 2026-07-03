using System.ComponentModel.DataAnnotations;

namespace Blocko.Services.DTOs.Api.Auth
{
    public class ResetPasswordRequestDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }
    }
}
