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


    public async Task<(ApiResponse<LoginResponseDto> Response, RefreshToken RefreshToken)>
    LoginAsync(LoginRequestDto loginRequestDto)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == loginRequestDto.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(loginRequestDto.Password, user.PasswordHash))
        {
            return (
                ApiResponse<LoginResponseDto>.FailureResponse(
                    "AUTH_INVALID_CREDENTIALS",
                    "البريد الإلكتروني أو كلمة المرور غير صحيحة"
                ),
                null!
            );
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
                Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList() // دعم تعدد الأدوار
            },
            RememberMe = refreshTokenEntity.RememberMe 
        };

        return (
            ApiResponse<LoginResponseDto>.SuccessResponse(loginDto, "تم تسجيل الدخول بنجاح"),
            refreshTokenEntity
        );
    }


    public async Task<(ApiResponse<RefreshTokenResponseDto>, RefreshToken?)>
    RefreshAsync(string tokenIdentifier, string rawToken)
    {
        var tokenEntity = await _context.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(rt =>
                rt.TokenIdentifier == tokenIdentifier &&
                !rt.IsRevoked &&
                rt.ExpiresAt > DateTime.UtcNow);

        if (tokenEntity == null || !BCrypt.Net.BCrypt.Verify(rawToken, tokenEntity.TokenHash))
        {
            return (
                ApiResponse<RefreshTokenResponseDto>.FailureResponse(
                    "INVALID_REFRESH_TOKEN",
                    "انتهت الجلسة، يرجى تسجيل الدخول مرة أخرى"
                ),
                null
            );
        }

        // إلغاء التوكن القديم
        tokenEntity.IsRevoked = true;
        tokenEntity.RevokedAt = DateTime.UtcNow;

        // توليد RefreshToken جديد بنفس قيمة RememberMe المخزنة
        var newRefreshToken = _tokenService.GenerateRefreshToken(
            tokenEntity.UserId,
            tokenEntity.RememberMe
        );

        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync();

        var dto = new RefreshTokenResponseDto
        {
            AccessToken = _tokenService.GenerateAccessToken(tokenEntity.User),
            ExpiresAt = _tokenService.GetAccessTokenExpiry(),
            RememberMe = newRefreshToken.RememberMe
        };

        return (
            ApiResponse<RefreshTokenResponseDto>.SuccessResponse(
                dto,
                "تم تحديث الجلسة بنجاح"
            ),
            newRefreshToken
        );
    }

    public async Task<ApiResponse<object>> LogoutAsync(string tokenIdentifier, string rawToken)
    {
        var tokenEntity = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt =>
                rt.TokenIdentifier == tokenIdentifier &&
                !rt.IsRevoked);

        if (tokenEntity == null)
        {
            return ApiResponse<object>.FailureResponse(
                "INVALID_SESSION",
                "الجلسة غير موجودة"
            );
        }

        // تحقق أمني
        if (!BCrypt.Net.BCrypt.Verify(rawToken, tokenEntity.TokenHash))
        {
            return ApiResponse<object>.FailureResponse(
                "INVALID_SESSION",
                "رمز الجلسة غير صالح"
            );
        }

        tokenEntity.IsRevoked = true;
        tokenEntity.RevokedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return ApiResponse<object>.SuccessResponse(
            null,
            "تم تسجيل الخروج بنجاح"
        );
    }


}
/*
 - ارسلت رفرش ولكن التوكن القديم مازال لم ينتهي ولم يلغى 
= سيقوم بانشاء جديد والغاء السابق مع تحديد وقت اللغاء

- ارسلت رفرش والتكون القديم ما زال صال ولم يلغى ولكن الايدندفاير كان خطأ  
او 
التوكن كان خطائيا 

 */
