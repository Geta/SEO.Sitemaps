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
using Geta.SEO.Sitemaps.Utils;
using Geta.SEO.Sitemaps.XML;
using Mediachase.Commerce.Catalog;

namespace Geta.SEO.Sitemaps.Commerce
{
    /// <summary>
    /// Known bug: You need to add * (wildcard) url in sitedefinitions in admin mode for this job to run. See: http://world.episerver.com/forum/developer-forum/EPiServer-Commerce/Thread-Container/2013/12/Null-exception-in-GetUrl-in-search-provider-indexer/
    /// </summary>
    [ServiceConfiguration(typeof(ICommerceSitemapXmlGenerator))]
    public class CommerceSitemapXmlGenerator : SitemapXmlGenerator, ICommerceSitemapXmlGenerator
    {
        private readonly ReferenceConverter _referenceConverter;

        public CommerceSitemapXmlGenerator(ISitemapRepository sitemapRepository, IContentRepository contentRepository, UrlResolver urlResolver, ISiteDefinitionRepository siteDefinitionRepository, ILanguageBranchRepository languageBranchRepository, ReferenceConverter referenceConverter, IContentFilter contentFilter) : base(sitemapRepository, contentRepository, urlResolver, siteDefinitionRepository, languageBranchRepository, contentFilter)
        {
            if (referenceConverter == null) throw new ArgumentNullException("referenceConverter");
            _referenceConverter = referenceConverter;
        }

        protected override IEnumerable<XElement> GetSitemapXmlElements()
        {
            var rootContentReference = _referenceConverter.GetRootLink();

            if (SitemapData.RootPageId != -1)
            {
                rootContentReference = new ContentReference(SitemapData.RootPageId)
                {
                    ProviderName = "CatalogContent"
                };
            }

            IList<ContentReference> descendants = ContentRepository.GetDescendents(rootContentReference).ToList();

            return GenerateXmlElements(descendants);
        }
    }
}