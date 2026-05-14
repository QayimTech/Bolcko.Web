namespace Bolcko.Web.App.Areas.Shop.Models.Cart
{
    public class CartItemVm
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public string? ImageUrl { get; set; }

        public decimal LineTotal => UnitPrice * Quantity;
    }
}

