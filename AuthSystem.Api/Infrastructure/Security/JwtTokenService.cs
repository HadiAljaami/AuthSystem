using AuthSystem.Api.Application.Interfaces;
using AuthSystem.Api.Domain.Entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AuthSystem.Api.Infrastructure.Security
{
    public class JwtTokenService: ITokenService
    {
        private readonly IConfiguration _config;

        public JwtTokenService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateAccessToken(User user)
        {
        var claims = new List<Claim>
            {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            };

            // إضافة الأدوار
            foreach (var role in user.UserRoles.Select(ur => ur.Role.Name))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // إضافة الصلاحيات
            var permissions = user.UserRoles
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission.Name)
                .Distinct();

            foreach (var permission in permissions)
            {
                claims.Add(new Claim("permission", permission));
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!)
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    int.Parse(_config["Jwt:AccessTokenExpirationMinutes"]!)
                ),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public DateTime GetAccessTokenExpiry()
        {
            return DateTime.UtcNow.AddMinutes(
                int.Parse(_config["Jwt:AccessTokenExpirationMinutes"]!)
            );
        }

        public RefreshToken GenerateRefreshToken(int userId, bool rememberMe)
        {
            var randomBytes = RandomNumberGenerator.GetBytes(64);
            var rawToken = Convert.ToBase64String(randomBytes);

            // توليد معرف سريع (GUID) للبحث السريع في قاعدة البيانات
            var identifier = Guid.NewGuid().ToString("N");

            return new RefreshToken
            {
                UserId = userId,
                TokenIdentifier = identifier, // معرف سريع
                RawToken = rawToken,
                TokenHash = BCrypt.Net.BCrypt.HashPassword(rawToken), // تخزين الـ Hash فقط
                ExpiresAt = rememberMe
                    ? DateTime.UtcNow.AddDays(int.Parse(_config["Jwt:RefreshTokenExpirationDays"]!))
                    : DateTime.UtcNow.AddDays(1),
                CreatedAt = DateTime.UtcNow,
                RememberMe = rememberMe
            };
        }


        //public RefreshToken GenerateRefreshToken(int userId, bool rememberMe)
        //{
        //    var randomBytes = RandomNumberGenerator.GetBytes(64);
        //    var rawToken = Convert.ToBase64String(randomBytes);

        //    return new RefreshToken
        //    {
        //        UserId = userId,
        //        RawToken = rawToken,
        //        TokenHash = BCrypt.Net.BCrypt.HashPassword(rawToken),
        //        ExpiresAt = rememberMe
        //            ? DateTime.UtcNow.AddDays(
        //                int.Parse(_config["Jwt:RefreshTokenExpirationDays"]!)
        //              )
        //            : DateTime.UtcNow.AddDays(1),
        //    };
        //}

    }
}
