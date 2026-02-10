using AuthSystem.Api.Application.DTOs.Auth;
using AuthSystem.Api.Application.DTOs.Common;
using Microsoft.AspNetCore.Identity.Data;
using LoginRequest = AuthSystem.Api.Application.DTOs.Auth.LoginRequest;

namespace AuthSystem.Api.Application.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequest request);
    }
}
