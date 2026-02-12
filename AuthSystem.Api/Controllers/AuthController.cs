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

            // تخزين الـ RawToken في Cookie
            Response.Cookies.Append("refreshToken", refreshToken.RawToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // في الإنتاج
                SameSite = SameSiteMode.Strict,
                Expires = refreshToken.ExpiresAt
            });

            // إرسال الـ TokenIdentifier في الـ Header
            Response.Headers.Append("X-Refresh-Token-Id", refreshToken.TokenIdentifier);

            return Ok(result);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            // قراءة الـ RawToken من الـ Cookie
            var rawToken = Request.Cookies["refreshToken"];
            // قراءة الـ TokenIdentifier من الـ Header
            var tokenIdentifier = Request.Headers["X-Refresh-Token-Id"].FirstOrDefault();

            if (string.IsNullOrEmpty(rawToken) || string.IsNullOrEmpty(tokenIdentifier))
            {
                return Unauthorized(ApiResponse<object>.FailureResponse(
                    "REFRESH_TOKEN_MISSING",
                    "رمز التحديث مفقود"
                ));
            }

            // استدعاء الـ Service للتحقق وتوليد التوكن الجديد
            var (result, newRefreshToken) =
                await _authService.RefreshAsync(tokenIdentifier, rawToken);

            if (!result.Success || newRefreshToken == null)
                return Unauthorized(result);

            // تحديث الـ Cookie بالـ RawToken الجديد
            Response.Cookies.Append("refreshToken", newRefreshToken.RawToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // في الإنتاج
                SameSite = SameSiteMode.Strict,
                Expires = newRefreshToken.ExpiresAt
            });

          
            // إرسال الـ TokenIdentifier الجديد في الـ Header
            Response.Headers.Append("X-Refresh-Token-Id", newRefreshToken.TokenIdentifier);

            // إرجاع النتيجة (AccessToken الجديد + ExpiresAt)
            return Ok(result);
        }


        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var rawToken = Request.Cookies["refreshToken"];
            var tokenIdentifier = Request.Headers["X-Refresh-Token-Id"].FirstOrDefault();

            if (string.IsNullOrEmpty(rawToken) || string.IsNullOrEmpty(tokenIdentifier))
            {
                return Unauthorized(ApiResponse<object>.FailureResponse(
                    "SESSION_MISSING",
                    "لا توجد جلسة نشطة"
                ));
            }

            var result = await _authService.LogoutAsync(tokenIdentifier, rawToken);

            if (!result.Success)
                return Unauthorized(result);

            // حذف الكوكي
            Response.Cookies.Delete("refreshToken");

            return Ok(result);
        }

    }


}
