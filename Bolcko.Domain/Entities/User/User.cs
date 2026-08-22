using Bolcko.Domain.Common;
using Bolcko.Domain.Enums;
using Bolcko.Domain.Entities.Order;
using Bolcko.Domain.Entities.Project;
using Bolcko.Domain.Entities.Tender;
using Microsoft.AspNetCore.Identity;

namespace Bolcko.Domain.Entities.User
{
    public class User : IdentityUser<int>
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public UserType UserType { get; set; }
        public string? CompanyName { get; set; }
        public string? BusinessRegistrationNumber { get; set; }
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }

        /// <summary>
        /// Forces the user to change their password on next login.
        /// Used for seeded admin accounts to avoid hardcoded password exposure.
        /// </summary>
        public bool MustChangePassword { get; set; } = false;

        public ICollection<Address> Addresses { get; set; } = new List<Address>();
        public ICollection<Bolcko.Domain.Entities.Order.Order> Orders { get; set; } = new List<Bolcko.Domain.Entities.Order.Order>();
        public ICollection<Bolcko.Domain.Entities.Project.Project> Projects { get; set; } = new List<Bolcko.Domain.Entities.Project.Project>();
        public ICollection<Bolcko.Domain.Entities.Tender.Tender> Tenders { get; set; } = new List<Bolcko.Domain.Entities.Tender.Tender>();
    }
}