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
    }
}
