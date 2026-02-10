using AuthSystem.Api.Application.DTOs.Auth;
using AuthSystem.Api.Application.DTOs.Common;
using AuthSystem.Api.Application.Interfaces;
using AuthSystem.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthSystem.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
 
        private readonly ITokenService _tokenService;
        private readonly AuthService _authService;

        public AuthController( ITokenService tokenService,AuthService authService)
        {
            _tokenService = tokenService;
            _authService = authService;
        }

        //[HttpPost("login")]
        //public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        //{
        //    var result = await _authService.LoginAsync(request);

        //    if (!result.Success || result.Data?.User == null) // Ensure result.Data and result.Data.User are not null
        //        return Unauthorized(result);

        //    // إرسال Refresh Token كـ Cookie
        //    var refreshToken = await _authService.GetValidRefreshTokenAsync(result.Data.User.Id);
        //    if (refreshToken == null) // Handle the case where refreshToken is null
        //        return Unauthorized("Unable to generate a valid refresh token.");

        //    Response.Cookies.Append("refreshToken", refreshToken.TokenHash, new CookieOptions
        //    {
        //        HttpOnly = true,
        //        Secure = true,
        //        SameSite = SameSiteMode.Strict,
        //        Expires = refreshToken.ExpiresAt
        //    });

        //    return Ok(result);
        //}

        [HttpGet("test-crash")]
        public IActionResult TestCrash()
        {
            throw new Exception("Crash for testing");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var (result, refreshToken) = await _authService.LoginAsync(request);

            if (!result.Success || refreshToken == null)
                return Unauthorized(result);

            Response.Cookies.Append("refreshToken", refreshToken.RawToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // في الإنتاج
                SameSite = SameSiteMode.Strict,
                Expires = refreshToken.ExpiresAt
            });

            return Ok(result);
        }

        //[HttpPost("refresh")]
        //public async Task<IActionResult> Refresh()
        //{
        //    var rawToken = Request.Cookies["refreshToken"];
        //    if (string.IsNullOrEmpty(rawToken))
        //    {
        //        return Unauthorized(ApiResponse<object>.FailureResponse(
        //            "REFRESH_TOKEN_MISSING",
        //            "لم يتم العثور على Refresh Token"
        //        ));
        //    }

        //    var refreshTokenEntity = await _context.RefreshTokens
        //        .Where(rt => !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
        //        .OrderByDescending(rt => rt.ExpiresAt)
        //        .FirstOrDefaultAsync(rt => BCrypt.Net.BCrypt.Verify(rawToken, rt.TokenHash));

        //    if (refreshTokenEntity == null)
        //    {
        //        return Unauthorized(ApiResponse<object>.FailureResponse(
        //            "REFRESH_TOKEN_INVALID",
        //            "التوكن غير صالح أو منتهي"
        //        ));
        //    }

        //    var user = await _context.Users
        //        .Include(u => u.UserRoles)
        //        .ThenInclude(ur => ur.Role)
        //        .FirstOrDefaultAsync(u => u.Id == refreshTokenEntity.UserId);

        //    if (user == null)
        //    {
        //        return Unauthorized(ApiResponse<object>.FailureResponse(
        //            "USER_NOT_FOUND",
        //            "المستخدم غير موجود"
        //        ));
        //    }

        //    var newAccessToken = _tokenService.GenerateAccessToken(user);

        //    var response = new
        //    {
        //        AccessToken = newAccessToken,
        //        ExpiresAt = _tokenService.GetAccessTokenExpiry()
        //    };

        //    return Ok(ApiResponse<object>.SuccessResponse(response, "تم تجديد التوكن بنجاح"));
        //}

    }


}


//[HttpPost("login")]
//public async Task<IActionResult> Login([FromBody] LoginRequest request)
//{
//    var user = await _context.Users
//        .FirstOrDefaultAsync(u => u.Email == request.Email);

//    if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
//        return Unauthorized("Invalid credentials");

//    var accessToken = _tokenService.GenerateAccessToken(user);
//    var refreshToken = _tokenService.GenerateRefreshToken(user.Id, request.RememberMe);

//    // احفظ الـ RefreshToken في قاعدة البيانات
//    _context.RefreshTokens.Add(refreshToken);
//    await _context.SaveChangesAsync();

//    return Ok(new
//    {
//        AccessToken = accessToken,
//        RefreshToken = refreshToken.TokenHash
//    });
//}
