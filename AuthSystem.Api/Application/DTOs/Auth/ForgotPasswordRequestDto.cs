using System.ComponentModel.DataAnnotations;

namespace AuthSystem.Api.Application.DTOs.Auth
{
    public class ForgotPasswordRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }


}
