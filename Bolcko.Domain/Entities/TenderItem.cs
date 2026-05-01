using Bolcko.Domain.Common;

namespace Bolcko.Domain.Entities
{
    public class TenderItem : BaseEntity
    {
        public int TenderId { get; set; }
        public Tender Tender { get; set; } = null!;
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public decimal RequestedQuantity { get; set; }
        public decimal? ProposedPricePerUnit { get; set; }
        public decimal? SubtotalItem { get; set; }
    }
}