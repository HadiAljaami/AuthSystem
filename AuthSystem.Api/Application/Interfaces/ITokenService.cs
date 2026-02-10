using AuthSystem.Api.Domain.Entities;

namespace AuthSystem.Api.Application.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(User user);
        RefreshToken GenerateRefreshToken(int userId, bool rememberMe);
        DateTime GetAccessTokenExpiry();
    }
}
