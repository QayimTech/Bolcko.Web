using Bolcko.Domain.Common;
using Bolcko.Domain.Entities.User;
using Bolcko.Domain.Enums;
using System;

namespace Bolcko.Domain.Entities.Catalog
{
    public class VendorTeamMember : BaseEntity
    {
        public int VendorId { get; set; }
        public VendorProfile? Vendor { get; set; }

        public int? UserId { get; set; }
        public User.User? User { get; set; }

        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? JobTitle { get; set; }

        public VendorRoleType RoleType { get; set; } = VendorRoleType.DataEntry;

        // Granular Permissions
        public bool CanManageCatalog { get; set; } = true;
        public bool CanManageOrders { get; set; } = false;
        public bool CanViewFinance { get; set; } = false;
        public bool CanManageTeam { get; set; } = false;

        public bool IsActive { get; set; } = true;
        public DateTime InvitedAt { get; set; } = DateTime.UtcNow;
        public DateTime? JoinedAt { get; set; }
    }
}
