using System.Xml.Linq;
using EPiServer.Core;

namespace Geta.SEO.Sitemaps.XML
{
    public class MobileSitemapXmlGenerator : StandardSitemapXmlGenerator
    {
        protected XNamespace MobileNamespace
        {
            get { return @"http://www.google.com/schemas/sitemap-mobile/1.0"; }
        }

        public override XElement GenerateSiteElement(PageData pageData, string url)
        {
            var element = base.GenerateSiteElement(pageData, url);

            // add <mobile:mobile/> to standard sitemap url element
            element.Add(new XElement(MobileNamespace + "mobile"));

            return element;
        }

        public override XElement GenerateRootElement()
        {
            var element = base.GenerateRootElement();

            element.Add(new XAttribute(XNamespace.Xmlns + "mobile", MobileNamespace));

            return element;
        }
    }
}
