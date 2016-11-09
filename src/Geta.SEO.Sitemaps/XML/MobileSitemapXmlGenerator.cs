using System.Xml.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Geta.SEO.Sitemaps.Repositories;

namespace Geta.SEO.Sitemaps.XML
{
    [ServiceConfiguration(typeof(IMobileSitemapXmlGenerator))]
    public class MobileSitemapXmlGenerator : SitemapXmlGenerator, IMobileSitemapXmlGenerator
    {
        public MobileSitemapXmlGenerator(ISitemapRepository sitemapRepository, IContentRepository contentRepository, UrlResolver urlResolver, SiteDefinitionRepository siteDefinitionRepository, ILanguageBranchRepository languageBranchRepository) : base(sitemapRepository, contentRepository, urlResolver, siteDefinitionRepository, languageBranchRepository)
        {
        }

        protected XNamespace MobileNamespace
        {
            get { return @"http://www.google.com/schemas/sitemap-mobile/1.0"; }
        }

        protected override XElement GenerateSiteElement(IContent contentData, string url)
        {
            var element = base.GenerateSiteElement(contentData, url);

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
