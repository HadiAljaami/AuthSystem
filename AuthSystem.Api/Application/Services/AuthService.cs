using AuthSystem.Api.Application.DTOs.Auth;
using AuthSystem.Api.Application.DTOs.Common;
using AuthSystem.Api.Application.Interfaces;
using AuthSystem.Api.Domain.Entities;
using AuthSystem.Api.Infrastructure.Persistence;
using AuthSystem.Api.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using System.Text;

public class AuthService 
{
    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;

    private readonly PasswordResetTokenService _resetTokenService;

    public AuthService(AppDbContext context, ITokenService tokenService, PasswordResetTokenService passwordResetTokenService )
    {
        _context = context;
        _tokenService = tokenService;
        _resetTokenService = passwordResetTokenService;
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

    public async Task<ApiResponse<UserDto>> RegisterAsync(RegisterRequestDto dto)
    {
        // Check if email already exists
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
        {
            return ApiResponse<UserDto>.FailureResponse(
                "EMAIL_EXISTS",
                "This email is already registered"
            );
        }

        // Hash password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        // Create user entity
        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };

        // Assign default role (User)
        var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
        if (defaultRole != null)
        {
            user.UserRoles = new List<UserRole>
                {
                    new UserRole { RoleId = defaultRole.Id }
                };
        }

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Build response DTO
        var userDto = new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
        };

        return ApiResponse<UserDto>.SuccessResponse(userDto, "Registration successful");
    }

    public async Task<ApiResponse<object>> LogoutAllAsync(int userId)
    {
        // تعطيل جميع التوكنات دفعة واحدة بدون تحميلها في الذاكرة
        var revokedCount = await _context.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.IsRevoked, true)
                .SetProperty(t => t.RevokedAt, DateTime.UtcNow)
            );

        return ApiResponse<object>.SuccessResponse(
            new { RevokedCount = revokedCount, LoggedOutAt = DateTime.UtcNow },
            "تم تسجيل الخروج من جميع الأجهزة"
        );
    }

    public async Task<ApiResponse<object>> ChangePasswordAsync(
    int userId,
    ChangePasswordDto dto)
    {
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
            return ApiResponse<object>.FailureResponse("NOT_FOUND",
                "المستخدم غير موجود",null!);

       
        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
        {
            return ApiResponse<object>.FailureResponse(
                "INVALID_PASSWORD",
                "كلمة المرور الحالية غير صحيحة",
                null!
            );
        }

       
        //if (dto.NewPassword.Length < 8)
        //{
        //    return ApiResponse<object>.FailureResponse(
        //        "WEAK_PASSWORD",
        //        "كلمة المرور الجديدة يجب أن تكون 8 أحرف على الأقل",
        //        null!
        //    );
        //}

  
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

        // تعطيل جميع الـ RefreshTokens دفعة واحدة
        var revokedCount = await _context.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.IsRevoked, true)
                .SetProperty(t => t.RevokedAt, DateTime.UtcNow)
            );

        await _context.SaveChangesAsync();

        return ApiResponse<object>.SuccessResponse(
            new { RevokedCount = revokedCount, ChangedAt = DateTime.UtcNow },
            "تم تغيير كلمة المرور ويجب تسجيل الدخول مجدداً" 
        );
    }


    public async Task<ApiResponse<object>> ForgotPasswordAsync(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            return ApiResponse<object>.FailureResponse("NOT_FOUND", "البريد الإلكتروني غير مسجل");

        // توليد التوكن باستخدام الخدمة
        var (rawToken, entity) = _resetTokenService.GenerateToken(user.Id);

        // حفظ التوكن في قاعدة البيانات
        _context.passwordResetTokens.Add(entity);
        await _context.SaveChangesAsync();

        // إرسال البريد الإلكتروني مع الرابط (rawToken هو الذي نرسله)
        // مثال: https://yourapp.com/reset-password?token=rawToken
        // EmailService.SendResetLink(user.Email, rawToken);

        return ApiResponse<object>.SuccessResponse(null, "تم إرسال رابط إعادة تعيين كلمة المرور إلى بريدك الإلكتروني");
    }



    public async Task<ApiResponse<object>> ResetPasswordAsync(string rawToken, string newPassword)
    {
        var tokenHash = _resetTokenService.ComputeHashForVerification(rawToken);

        var resetToken = await _context.passwordResetTokens
            .FirstOrDefaultAsync(t =>
                t.TokenHash == tokenHash &&
                !t.IsUsed &&
                t.ExpiresAt > DateTime.UtcNow);


        if (resetToken == null)
            return ApiResponse<object>.FailureResponse("INVALID_TOKEN", "الرابط غير صالح أو منتهي");

        var user = await _context.Users.FindAsync(resetToken.UserId);
        if (user == null)
            return ApiResponse<object>.FailureResponse("NOT_FOUND", "المستخدم غير موجود");

        // Update password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

        // Revoke all refresh tokens for this user
        await _context.RefreshTokens
            .Where(t => t.UserId == user.Id && !t.IsRevoked)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.IsRevoked, true)
                .SetProperty(t => t.RevokedAt, DateTime.UtcNow)
            );

        // Mark reset token as used
        resetToken.IsUsed = true;

        await _context.SaveChangesAsync();

        return ApiResponse<object>.SuccessResponse(null, "تم إعادة تعيين كلمة المرور بنجاح، يرجى تسجيل الدخول مجدداً");
    }



    //public async Task<ApiResponse<object>> ResetPasswordAsync(string rawToken, string newPassword)
    //{
    //    // حساب SHA256 للـ RawToken المدخل
    //    using var sha256 = System.Security.Cryptography.SHA256.Create();
    //    var tokenHash = Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(rawToken)));

    //    // البحث عن التوكن في قاعدة البيانات مباشرة بالـ Hash
    //    var resetToken = await _context.passwordResetTokens
    //        .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow);

    //    if (resetToken == null)
    //        return ApiResponse<object>.FailureResponse("INVALID_TOKEN", "الرابط غير صالح أو منتهي");

    //    // جلب المستخدم المرتبط بالتوكن
    //    var user = await _context.Users.FindAsync(resetToken.UserId);
    //    if (user == null)
    //        return ApiResponse<object>.FailureResponse("NOT_FOUND", "المستخدم غير موجود");

    //    // تحديث كلمة المرور
    //    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

    //    // تعطيل جميع الـ RefreshTokens الخاصة بالمستخدم
    //    await _context.RefreshTokens
    //        .Where(t => t.UserId == user.Id && !t.IsRevoked)
    //        .ExecuteUpdateAsync(s => s
    //            .SetProperty(t => t.IsRevoked, true)
    //            .SetProperty(t => t.RevokedAt, DateTime.UtcNow)
    //        );

    //    // تعطيل التوكن بعد استخدامه
    //    resetToken.IsUsed = true;

    //    await _context.SaveChangesAsync();

    //    return ApiResponse<object>.SuccessResponse(null, "تم إعادة تعيين كلمة المرور بنجاح، يرجى تسجيل الدخول مجدداً");
    //}


}
/*
 - ارسلت رفرش ولكن التوكن القديم مازال لم ينتهي ولم يلغى 
= سيقوم بانشاء جديد والغاء السابق مع تحديد وقت اللغاء

- ارسلت رفرش والتكون القديم ما زال صال ولم يلغى ولكن الايدندفاير كان خطأ  
او 
التوكن كان خطائيا 

 */
