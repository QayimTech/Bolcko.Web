using Bolcko.Domain.Common;
using System;

namespace Bolcko.Domain.Entities.Delivery
{
    public class VendorCustomCarrier : BaseEntity
    {
        public int VendorId { get; set; }
        public string CarrierName { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string FleetType { get; set; } = "تريلات مسطحة وونشات ثقيلة";
        public decimal BaseTariffJod { get; set; } = 25.00m;
        public decimal CraneFeeJod { get; set; } = 35.00m;
        public string CoveredGovernorates { get; set; } = "عمان, الزرقاء, البلقاء";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
