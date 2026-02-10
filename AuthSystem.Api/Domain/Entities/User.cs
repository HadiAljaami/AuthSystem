using System.ComponentModel.DataAnnotations;

namespace AuthSystem.Api.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = default!;
        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = default!;
        [Required]
        [MinLength(4)]
        public string PasswordHash { get; set; } = default!;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
