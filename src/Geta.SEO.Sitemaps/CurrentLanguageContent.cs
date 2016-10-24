using System.Globalization;
using EPiServer.Core;

namespace Geta.SEO.Sitemaps
{
    public class CurrentLanguageContent
    {
        public IContent Content { get; set; }
        public CultureInfo CurrentLanguage { get; set; }
        public CultureInfo MasterLanguage { get; set; }
    }
}