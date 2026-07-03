using System.ComponentModel.DataAnnotations;

namespace Blocko.Services.DTOs.Api.Cart
{
    public class UpdateCartItemRequestDto
    {
        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }
    }
}
