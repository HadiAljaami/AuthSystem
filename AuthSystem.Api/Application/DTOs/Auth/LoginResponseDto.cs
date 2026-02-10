namespace AuthSystem.Api.Application.DTOs.Auth
{
    public class LoginResponseDto
    {
        public string AccessToken { get; set; }
        public DateTime ExpiresAt { get; set; }
        public UserDto User { get; set; }
    }
}
