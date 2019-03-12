using EPiServer.Core;
using EPiServer.Web;
using Geta.SEO.Sitemaps.Entities;

namespace Geta.SEO.Sitemaps.Utils
{
    public interface IContentFilter
    {
        bool ShouldExcludeContent(IContent content);
        bool ShouldExcludeContent(
            CurrentLanguageContent languageContentInfo, SiteDefinition siteSettings, SitemapData sitemapData);
    }
}
