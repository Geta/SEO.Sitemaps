using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Geta.SEO.Sitemaps.Repositories;
using Geta.SEO.Sitemaps.SpecializedProperties;
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
        protected const string DateTimeFormat = "yyyy-MM-ddTHH:mm:sszzz";

        private SiteDefinition _settings;

        public CommerceSitemapXmlGenerator(ISitemapRepository sitemapRepository, IContentRepository contentRepository, UrlResolver urlResolver, SiteDefinitionRepository siteDefinitionRepository) : base(sitemapRepository, contentRepository, urlResolver, siteDefinitionRepository)
        {
        }

        public bool IsDebugMode { get; set; }

        protected override XElement GenerateSiteElement(IContent contentData, string url)
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            var catalogContent = (CatalogContentBase) contentData;
            var property = contentData.Property[PropertySEOSitemaps.PropertyName] as PropertySEOSitemaps;

            var element = new XElement(
                SitemapXmlNamespace + "url",
                new XElement(SitemapXmlNamespace + "loc", url),
                new XElement(SitemapXmlNamespace + "lastmod", catalogContent.StartPublish.Value.ToString(DateTimeFormat)), // TODO use modified
                new XElement(SitemapXmlNamespace + "changefreq", (property != null) ? property.ChangeFreq : "weekly"),
                new XElement(SitemapXmlNamespace + "priority", (property != null) ? property.Priority : GetPriority(url)));

            if (IsDebugMode)
            {
                element.AddFirst(new XComment(
                    string.Format(
                        "content ID: '{0}', name: '{1}', language: '{2}'",
                        contentData.ContentLink.ID, contentData.Name, catalogContent.Language)));
            }

            return element;
        }

        protected override XElement GenerateRootElement()
        {
            return new XElement(SitemapXmlNamespace + "urlset");
        }

        protected XNamespace SitemapXmlNamespace
        {
            get { return @"http://www.sitemaps.org/schemas/sitemap/0.9"; }
        }

        protected override IEnumerable<XElement> GetSitemapXmlElements()
        {

            if (this._settings == null)
            {
                return Enumerable.Empty<XElement>();
            }

            var referenceConverter = ServiceLocator.Current.GetInstance<ReferenceConverter>();

            IList<ContentReference> descendants = ContentRepository.GetDescendents(referenceConverter.GetRootLink()).ToList();

            return GenerateXmlElements(descendants);
        }

        private static string GetPriority(string url)
        {
            int depth = new Uri(url).Segments.Length - 1;

            return Math.Max(1.0 - (depth / 10.0), 0.5).ToString(CultureInfo.InvariantCulture);
        }
    }
}