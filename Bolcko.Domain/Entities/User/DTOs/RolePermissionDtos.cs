using System.Collections.Generic;

namespace Bolcko.Domain.Entities.User.DTOs
{
    public class RoleDetailsDto
    {
        public int Id { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public int UsersCount { get; set; }
        public List<string> SelectedPermissions { get; set; } = new();
    }

    public class CreateUpdateRoleDto
    {
        public int? RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public List<string> SelectedPermissions { get; set; } = new();
    }
}
