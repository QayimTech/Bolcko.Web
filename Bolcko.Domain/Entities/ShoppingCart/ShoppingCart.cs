using Bolcko.Domain.Common;

namespace Bolcko.Domain.Entities.ShoppingCart
{
    public class ShoppingCart : BaseEntity
    {
        public string SessionId { get; set; } = string.Empty;
        public int? UserId { get; set; }
        public Bolcko.Domain.Entities.User.User? User { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ShoppingCartItem> Items { get; set; } = new List<ShoppingCartItem>();
    }
}
