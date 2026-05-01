using Bolcko.Domain.Common;
using Bolcko.Domain.Enums;

namespace Bolcko.Domain.Entities
{
    public class User : BaseEntity
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public UserType UserType { get; set; }
        public string? CompanyName { get; set; }
        public string? BusinessRegistrationNumber { get; set; }
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }

        public ICollection<Address> Addresses { get; set; } = new List<Address>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Project> Projects { get; set; } = new List<Project>();
        public ICollection<Tender> Tenders { get; set; } = new List<Tender>();
    }
}