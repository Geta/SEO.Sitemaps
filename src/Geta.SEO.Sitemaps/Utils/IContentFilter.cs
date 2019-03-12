using EPiServer.Core;

namespace Geta.SEO.Sitemaps.Utils
{
    public interface IContentFilter
    {
        bool ShouldExcludeContent(IContent content);
        bool ShouldExcludeContent(CurrentLanguageContent languageContentInfo);
    }
}
