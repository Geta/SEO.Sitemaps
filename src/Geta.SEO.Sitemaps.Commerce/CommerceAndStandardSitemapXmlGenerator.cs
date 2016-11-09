using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Geta.SEO.Sitemaps.Repositories;
using Geta.SEO.Sitemaps.XML;
using Mediachase.Commerce.Catalog;

namespace Geta.SEO.Sitemaps.Commerce
{
    /// <summary>
    /// Known bug: You need to add * (wildcard) url in sitedefinitions in admin mode for this job to run. See: http://world.episerver.com/forum/developer-forum/EPiServer-Commerce/Thread-Container/2013/12/Null-exception-in-GetUrl-in-search-provider-indexer/
    /// </summary>
    [ServiceConfiguration(typeof(ICommerceAndStandardSitemapXmlGenerator))]
    public class CommerceAndStandardSitemapXmlGenerator : CommerceSitemapXmlGenerator, ICommerceAndStandardSitemapXmlGenerator
    {
        public CommerceAndStandardSitemapXmlGenerator(ISitemapRepository sitemapRepository, IContentRepository contentRepository, UrlResolver urlResolver, SiteDefinitionRepository siteDefinitionRepository, ILanguageBranchRepository languageBranchRepository, ReferenceConverter referenceConverter)
            : base(sitemapRepository, contentRepository, urlResolver, siteDefinitionRepository, languageBranchRepository, referenceConverter)
        {
        }

        protected override IEnumerable<XElement> GetSitemapXmlElements()
        {
            IList<ContentReference> contentDescendants = ContentRepository.GetDescendents(this.SiteSettings.StartPage).ToList();

            contentDescendants.Insert(0, this.SiteSettings.StartPage);

            IEnumerable<XElement> contentElements = GenerateXmlElements(contentDescendants);
            return contentElements.Union(base.GetSitemapXmlElements());
        }
    }
}