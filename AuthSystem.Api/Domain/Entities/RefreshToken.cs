using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthSystem.Api.Domain.Entities
{
    public class RefreshToken
    {
        public int Id { get; set; }

        public string TokenIdentifier { get; set; } = default!; // معرف سريع للبحث
        [StringLength(500)]
        public string TokenHash { get; set; } = default!;

        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; } = false;
        public DateTime? RevokedAt { get; set; } // وقت الإلغاء
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool RememberMe { get; set; }
        [NotMapped]
        public string RawToken { get; set; } = null!;

        public int UserId { get; set; }
        public User User { get; set; } = default!;
    }

}

/*
 * 📌 لماذا نخزن الـ TokenHash وليس التوكن نفسه؟
التوكن نفسه يعتبر سر حساس.

الأفضل أن نخزن نسخة مشفرة/مُهشّة (مثل كلمة المرور).

عند التحقق، نقوم بعمل Hash للتوكن القادم ونقارنه مع المخزن.

هذا يمنع أي اختراق لقاعدة البيانات من كشف التوكنات مباشرة.

📌 كيف يعمل النظام مع RefreshToken؟
عند تسجيل الدخول:

السيرفر يصدر Access Token قصير العمر.

يصدر Refresh Token طويل العمر ويخزنه في جدول RefreshTokens.

يرسل Refresh Token للمتصفح في HttpOnly Cookie.

عند انتهاء الـ Access Token:

الـ Frontend يرسل طلب Refresh.

السيرفر يبحث عن الـ Refresh Token في الجدول ويتأكد أنه:

موجود.

غير منتهي.

غير موقوف (IsRevoked = false).

إذا صحيح → يصدر Access Token جديد.

إذا غير صحيح → يرفض الطلب.

عند تسجيل الخروج:

السيرفر يضع الـ Refresh Token كـ Revoked في الجدول.

يمسح الكوكي من المتصفح.
*/