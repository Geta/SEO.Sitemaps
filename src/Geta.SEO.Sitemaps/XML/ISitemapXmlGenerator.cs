using Geta.SEO.Sitemaps.Entities;

namespace Geta.SEO.Sitemaps.XML
{
    public interface ISitemapXmlGenerator
    {
        bool IsDebugMode { get; set; }
        bool Generate(SitemapData sitemapData, bool persistData, out int entryCount);
        void Stop();
    }
}
