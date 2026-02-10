namespace AuthSystem.Api.Application.DTOs.Auth
{
    public class RefreshTokenResponseDto
    {
        public string AccessToken { get; set; } = default!;
        public DateTime ExpiresAt { get; set; }
        public bool RememberMe { get; set; } = false;
    }
}
