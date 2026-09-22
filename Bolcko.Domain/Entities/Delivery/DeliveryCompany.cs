using Bolcko.Domain.Common;
using System.Collections.Generic;

namespace Bolcko.Domain.Entities.Delivery
{
    public class DeliveryCompany : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? CommercialRegister { get; set; }
        
        // Base rate the company charges (can be overridden per job)
        public decimal BaseDeliveryRate { get; set; } = 25.00m;

        public string? ManagerUserId { get; set; }

        public bool IsActive { get; set; } = true;
        public bool SupportsOversized { get; set; } = true;
        public bool IsApiIntegration { get; set; } = true;

        // LOG-01 3PL Carrier KYC Specifications
        public string? TaxId { get; set; }
        public string? TransportCommissionLicense { get; set; } // ترخيص هيئة تنظيم النقل البري LTRC
        public string? CommercialRegisterDocUrl { get; set; }
        public string? TransportLicenseDocUrl { get; set; }
        public string? CliqAlias { get; set; }
        public int TotalTrucksCount { get; set; } = 5;
        public bool IsApproved { get; set; } = false;
        
        public ICollection<DeliveryDriver> Drivers { get; set; } = new List<DeliveryDriver>();
    }
}
