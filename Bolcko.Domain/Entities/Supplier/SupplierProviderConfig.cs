using System;
using Bolcko.Domain.Common;

namespace Bolcko.Domain.Entities.Supplier
{
    public class SupplierProviderConfig : BaseEntity
    {
        public string SupplierName { get; set; } = string.Empty; // e.g. "شركة القناص لتجارة الجملة"
        public string SupplierKey { get; set; } = string.Empty;  // e.g. "qannas", "vendor_b"
        public string BaseUrl { get; set; } = "https://api.qannasjo.com/";
        
        // API Credentials
        public string? ApiPhoneNumber { get; set; } // "0782023800"
        public string? ApiPassword { get; set; }    // "Qannas@100"
        public string? ApiEmail { get; set; }
        public string? AuthToken { get; set; }
        public int? ExternalCustomerId { get; set; } // e.g. 503
        public int? ExternalUserId { get; set; }     // e.g. 546
        
        // Origin Pickup Details for Delivery Couriers (e.g. GLC / Aramex)
        public string? PickupAddressLine { get; set; } = "عمان - رأس العين - بجانب مجمع الشحن";
        public long PickupCityId { get; set; } = 395;       // Amman City ID in GLC
        public long PickupRegionId { get; set; } = 33;      // Ras Al-Ain Region ID
        public long PickupVillageId { get; set; } = 18076;
        public string? ContactPersonName { get; set; } = "موظف بلوكو - مستودع رأس العين";
        public string? ContactPhoneNumber { get; set; } = "0782023800";
        
        // Settings
        public bool AutoDispatchEnabled { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public string? CustomHeadersJson { get; set; }
        public string? CustomPayloadMappingJson { get; set; }
        public string? Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
