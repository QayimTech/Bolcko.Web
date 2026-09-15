using System;

namespace Bolcko.Web.App.Extensions
{
    public static class ImageUrlExtensions
    {
        /// <summary>
        /// Normalizes a stored image path into a browser-usable src: absolute URLs pass through,
        /// everything else is rooted with a leading "/" so it resolves from the site root
        /// regardless of the current page's URL (fixes relative paths saved without one).
        /// </summary>
        public static string? ToImageUrl(this string? imagePath)
        {
            if (string.IsNullOrEmpty(imagePath)) return null;

            return imagePath.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? imagePath
                : "/" + imagePath.TrimStart('/');
        }

        private static readonly Lazy<TimeZoneInfo> JordanTimeZone = new(() =>
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Jordan Standard Time");
            }
            catch
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("Asia/Amman");
                }
                catch
                {
                    return TimeZoneInfo.CreateCustomTimeZone("Jordan Standard Time", TimeSpan.FromHours(3), "Jordan Standard Time", "Jordan Standard Time");
                }
            }
        });

        /// <summary>
        /// Converts a UTC DateTime to Jordan Local Time (UTC+3)
        /// </summary>
        public static DateTime ToJordanTime(this DateTime utcDateTime)
        {
            if (utcDateTime.Kind == DateTimeKind.Unspecified)
            {
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            }
            else if (utcDateTime.Kind == DateTimeKind.Local)
            {
                utcDateTime = utcDateTime.ToUniversalTime();
            }

            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, JordanTimeZone.Value);
        }

        /// <summary>
        /// Resolves a valid Material Symbol icon for a category based on its image/icon string and name
        /// </summary>
        public static string ToCategoryIcon(this string? imageUrl, string? categoryName = null)
        {
            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                var trimmed = imageUrl.Trim();
                if (!trimmed.Contains('/') && !trimmed.Contains('.') && !trimmed.Contains('\\') && trimmed.Length < 30)
                {
                    return trimmed;
                }
            }

            var name = (categoryName ?? string.Empty).ToLowerInvariant();
            if (name.Contains("اسمنت") || name.Contains("إسمنت") || name.Contains("cement") || name.Contains("خرسانة") || name.Contains("concrete"))
                return "foundation";
            if (name.Contains("حديد") || name.Contains("steel") || name.Contains("rebar"))
                return "precision_manufacturing";
            if (name.Contains("حجر") || name.Contains("stone") || name.Contains("واجهات"))
                return "domain";
            if (name.Contains("طوب") || name.Contains("بلوك") || name.Contains("block"))
                return "view_in_ar";
            if (name.Contains("سباكة") || name.Contains("صحي") || name.Contains("plumbing") || name.Contains("water") || name.Contains("sehi"))
                return "water_drop";
            if (name.Contains("كهرباء") || name.Contains("electrical") || name.Contains("wire"))
                return "bolt";
            if (name.Contains("دهان") || name.Contains("دهانات") || name.Contains("paint"))
                return "format_paint";
            if (name.Contains("خشب") || name.Contains("نجارة") || name.Contains("wood"))
                return "carpenter";
            if (name.Contains("باب") || name.Contains("أبواب") || name.Contains("شباك") || name.Contains("شبابيك") || name.Contains("door") || name.Contains("window"))
                return "door_front";
            if (name.Contains("عوازل") || name.Contains("insulation") || name.Contains("roof"))
                return "shield";
            if (name.Contains("بلاط") || name.Contains("سيراميك") || name.Contains("tiles"))
                return "grid_view";
            if (name.Contains("أدوات") || name.Contains("معدات") || name.Contains("tools") || name.Contains("handyman"))
                return "handyman";

            return "category";
        }
    }
}
