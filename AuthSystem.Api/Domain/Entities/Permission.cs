using System.ComponentModel.DataAnnotations;

namespace AuthSystem.Api.Domain.Entities
{
    public class Permission
    {
        public int Id { get; set; }
        [StringLength(100)]
        public string Name { get; set; } = default!; // e.g. "CreateUser", "DeletePost"

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
