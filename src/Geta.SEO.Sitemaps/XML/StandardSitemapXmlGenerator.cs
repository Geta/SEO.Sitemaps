using EPiServer;
using EPiServer.DataAbstraction;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Geta.SEO.Sitemaps.Repositories;

namespace Geta.SEO.Sitemaps.XML
{
    [ServiceConfiguration(typeof(IStandardSitemapXmlGenerator))]
    public class StandardSitemapXmlGenerator : SitemapXmlGenerator, IStandardSitemapXmlGenerator
    {
        public StandardSitemapXmlGenerator(ISitemapRepository sitemapRepository, IContentRepository contentRepository, UrlResolver urlResolver, SiteDefinitionRepository siteDefinitionRepository, ILanguageBranchRepository languageBranchRepository) : base(sitemapRepository, contentRepository, urlResolver, siteDefinitionRepository, languageBranchRepository)
        {
        }
    }
}