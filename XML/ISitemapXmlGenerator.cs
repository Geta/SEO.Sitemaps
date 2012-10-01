using System.Xml.Linq;
using EPiServer.Core;

namespace Geta.SEO.Sitemaps.XML
{
    public interface ISitemapXmlGenerator
    {
        bool IsDebugMode { get; set; }
        XElement GenerateSiteElement(PageData pageData, string url);
        XElement GenerateRootElement();
    }
}
