using AuthSystem.Api.Application.DTOs.Auth;
using AuthSystem.Api.Application.DTOs.Common;
using AuthSystem.Api.Application.Interfaces;
using AuthSystem.Api.Domain.Entities;
using AuthSystem.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class AuthService 
{
    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;

    public AuthService(AppDbContext context, ITokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    public async Task<(ApiResponse<LoginResponseDto> Response, RefreshToken RefreshToken)> LoginAsync(LoginRequestDto loginRequestDto)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == loginRequestDto.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(loginRequestDto.Password, user.PasswordHash))
        {
            return (ApiResponse<LoginResponseDto>
                .FailureResponse("AUTH_INVALID_CREDENTIALS"
                , "البريد الإلكتروني أو كلمة المرور غير صحيحة")
                , null!);
        }

        var accessToken = _tokenService.GenerateAccessToken(user);

        var refreshTokenEntity = _tokenService.GenerateRefreshToken(user.Id, loginRequestDto.RememberMe);
        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        var loginDto = new LoginResponseDto
        {
            AccessToken = accessToken,
            ExpiresAt = _tokenService.GetAccessTokenExpiry(),
            User = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.UserRoles.FirstOrDefault()?.Role.Name ?? "User"
            }
        };

        return (ApiResponse<LoginResponseDto>.SuccessResponse(loginDto, "تم تسجيل الدخول بنجاح"), refreshTokenEntity);
    }

    public async Task<RefreshToken?> GetValidRefreshTokenAsync(int userId)
    {
        return await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(rt => rt.ExpiresAt)
            .FirstOrDefaultAsync();
    }


}

/*
 
    public async Task<ApiResponse<LoginResponseDto>> LoginAsync2(LoginRequestDto loginRequestDto)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == loginRequestDto.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(loginRequestDto.Password, user.PasswordHash))
        {
            return ApiResponse<LoginResponseDto>.FailureResponse(
                "AUTH_INVALID_CREDENTIALS",
                "البريد الإلكتروني أو كلمة المرور غير صحيحة"
            );
        }

        // توليد التوكن
        var accessToken = _tokenService.GenerateAccessToken(user);

        // 2️⃣ توليد Refresh Token
        var refreshTokenEntity = _tokenService.GenerateRefreshToken(user.Id, loginRequestDto.RememberMe);
        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();


        var loginDto = new LoginResponseDto
        {
            AccessToken = accessToken,   
            ExpiresAt = _tokenService.GetAccessTokenExpiry(),
            User = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.UserRoles.FirstOrDefault()?.Role.Name ?? "User"
            }
        };

        return ApiResponse<LoginResponseDto>.SuccessResponse(loginDto, "تم تسجيل الدخول بنجاح");
    }

 */