using AuthSystem.Api.Application.DTOs.Auth;
using AuthSystem.Api.Application.DTOs.Common;
using AuthSystem.Api.Application.Interfaces;
using AuthSystem.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
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

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequestDto dto)
        {
            var result = await _authService.RegisterAsync(dto);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }


        [Authorize]
        [HttpPost("logout-all")]
        public async Task<IActionResult> LogoutAll()
        {
            var userId = int.Parse(User.FindFirst("sub")!.Value);

            var result = await _authService.LogoutAllAsync(userId);

            // حذف الكوكي من الجهاز الحالي
            Response.Cookies.Delete("refreshToken");

            return Ok(result);
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            var userId = int.Parse(User.FindFirst("sub")!.Value);

            var result = await _authService.ChangePasswordAsync(userId, dto);

            if (!result.Success)
                return BadRequest(result);

            // حذف الكوكي حتى يخرج المستخدم فوراً من الجلسة الحالية
            Response.Cookies.Delete("refreshToken");

            return Ok(result);
        }


        /// <summary>
        /// Request a password reset link via email.
        /// </summary>
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
        {
            var response = await _authService.ForgotPasswordAsync(request.Email);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        /// <summary>
        /// Reset the password using the token from email.
        /// </summary>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
        {
            var response = await _authService.ResetPasswordAsync(request.Token, request.NewPassword);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }
    }


}


