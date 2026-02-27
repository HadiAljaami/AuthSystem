using AuthSystem.Api.Domain.Entities;
using System.Security.Cryptography;
using System.Text;

namespace AuthSystem.Api.Infrastructure.Security
{
    public class PasswordResetTokenService
    {
        private readonly IConfiguration _config;

        public PasswordResetTokenService(IConfiguration config)
        {
            _config = config;
        }

        // دالة خاصة لتوليد الهاش مع الـ Pepper
        private string ComputeHash(string rawToken)
        {
            using var sha256 = SHA256.Create();
            var secret = _config["PasswordReset:HashSecret"];
            var combined = rawToken + secret;

            return Convert.ToHexString(
                sha256.ComputeHash(Encoding.UTF8.GetBytes(combined)));
        }

        // دالة توليد التوكن
        public (string rawToken, PasswordResetToken entity) GenerateToken(int userId)
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            var rawToken = Convert.ToHexString(bytes);

            var tokenHash = ComputeHash(rawToken);

            var expiryMinutes = _config.GetValue<int>("PasswordReset:TokenExpiryMinutes");
            var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

            var entity = new PasswordResetToken
            {
                UserId = userId,
                TokenHash = tokenHash,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow,
                IsUsed = false
            };

            return (rawToken, entity);
        }

        // دالة للتحقق من التوكن
        public string ComputeHashForVerification(string rawToken)
        {
            return ComputeHash(rawToken);
        }
    }

}

//public (string rawToken, PasswordResetToken entity) GenerateToken(int userId)
//{
//    var rawToken = Guid.NewGuid().ToString("N");

//    using var sha256 = System.Security.Cryptography.SHA256.Create();
//    var tokenHash = Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(rawToken)));

//    var expiryMinutes = _config.GetValue<int>("PasswordReset:TokenExpiryMinutes");
//    var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

//    var entity = new PasswordResetToken
//    {
//        UserId = userId,
//        TokenHash = tokenHash,
//        ExpiresAt = expiresAt,
//        CreatedAt = DateTime.UtcNow,
//        IsUsed = false
//    };

//    return (rawToken, entity);
//}

//public (string rawToken, PasswordResetToken entity) GenerateToken(int userId)
//{
//    // Generate a strong random token (256-bit entropy)
//    var bytes = RandomNumberGenerator.GetBytes(32);
//    var rawToken = Convert.ToHexString(bytes);

//    using var sha256 = SHA256.Create();

//    // Add a secret pepper from configuration for extra security
//    var secret = _config["PasswordReset:HashSecret"];
//    var combined = rawToken + secret;

//    // Hash the combined token
//    var tokenHash = Convert.ToHexString(
//        sha256.ComputeHash(Encoding.UTF8.GetBytes(combined)));

//    // Read expiry from configuration
//    var expiryMinutes = _config.GetValue<int>("PasswordReset:TokenExpiryMinutes");
//    var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

//    var entity = new PasswordResetToken
//    {
//        UserId = userId,
//        TokenHash = tokenHash,
//        ExpiresAt = expiresAt,
//        CreatedAt = DateTime.UtcNow,
//        IsUsed = false
//    };

//    // Return rawToken (to send via email) and entity (to store in DB)
//    return (rawToken, entity);
//}


