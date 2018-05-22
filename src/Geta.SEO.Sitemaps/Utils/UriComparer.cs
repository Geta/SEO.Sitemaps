using System;

namespace Geta.SEO.Sitemaps.Utils
{
    public static class UriComparer
    {
        public static bool SchemeAndServerEquals(Uri first, Uri second)
        {
            return Uri.Compare(
                       first,
                       second,
                       UriComponents.SchemeAndServer,
                       UriFormat.SafeUnescaped,
                       StringComparison.OrdinalIgnoreCase) == 0;
        }
    }
}