using System.ComponentModel.DataAnnotations;

namespace AuthSystem.Api.Domain.Entities
{
    public class Role
    {
        public int Id { get; set; }
        [StringLength(100)]
        public string Name { get; set; } = default!; // Admin, User, etc.

        // Navigation
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
