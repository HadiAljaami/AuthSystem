using System.ComponentModel.DataAnnotations;

namespace AuthSystem.Api.Application.DTOs.Auth
{
    public class ResetPasswordRequestDto
    {
        [Required]
        public string Token { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 4)]
        public string NewPassword { get; set; }
    }

}
