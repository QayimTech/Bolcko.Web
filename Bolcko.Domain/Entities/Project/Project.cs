using Bolcko.Domain.Common;
using Bolcko.Domain.Enums;
using Bolcko.Domain.Entities.User;

namespace Bolcko.Domain.Entities.Project
{
    public class Project : BaseEntity
    {
        public int UserId { get; set; }
        public Bolcko.Domain.Entities.User.User User { get; set; } = null!;
        public string ProjectName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? LocationAddressId { get; set; }
        public Address? LocationAddress { get; set; }
        public ProjectStatus Status { get; set; }
    }
}