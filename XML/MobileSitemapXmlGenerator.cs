using System.Xml.Linq;
using EPiServer.Core;
using Geta.SEO.Sitemaps.Repositories;

namespace Geta.SEO.Sitemaps.XML
{
    public class MobileSitemapXmlGenerator : StandardSitemapXmlGenerator
    {
        public MobileSitemapXmlGenerator(ISitemapRepository sitemapRepository) : base(sitemapRepository)
        {
        }

        protected XNamespace MobileNamespace
        {
            get { return @"http://www.google.com/schemas/sitemap-mobile/1.0"; }
        }

        protected override XElement GenerateSiteElement(PageData pageData, string url)
        {
            var element = base.GenerateSiteElement(pageData, url);

            // add <mobile:mobile/> to standard sitemap url element
            element.Add(new XElement(MobileNamespace + "mobile"));

            return element;
        }

        protected override XElement GenerateRootElement()
        {
            var element = base.GenerateRootElement();

            element.Add(new XAttribute(XNamespace.Xmlns + "mobile", MobileNamespace));

            return element;
        }
    }
}
