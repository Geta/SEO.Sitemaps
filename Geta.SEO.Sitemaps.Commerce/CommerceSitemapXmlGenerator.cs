using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.Logging.Compatibility;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Geta.SEO.Sitemaps.Entities;
using Geta.SEO.Sitemaps.Repositories;
using Geta.SEO.Sitemaps.SpecializedProperties;
using Geta.SEO.Sitemaps.Utils;
using Geta.SEO.Sitemaps.XML;
using Mediachase.Commerce.Catalog;

namespace Geta.SEO.Sitemaps.Commerce
{
    /// <summary>
    /// Known bug: You need to add * (wildcard) url in sitedefinitions in admin mode for this job to run. See: http://world.episerver.com/forum/developer-forum/EPiServer-Commerce/Thread-Container/2013/12/Null-exception-in-GetUrl-in-search-provider-indexer/
    /// </summary>
    [ServiceConfiguration(typeof(ICommerceSitemapXmlGenerator))]
    public class CommerceSitemapXmlGenerator : ICommerceSitemapXmlGenerator
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SitemapXmlGenerator));

        protected const string DateTimeFormat = "yyyy-MM-ddTHH:mm:sszzz";

        private readonly ISitemapRepository _sitemapRepository;
        private readonly IContentRepository _contentRepository;
        private readonly UrlResolver _urlResolver;
        private readonly SiteDefinitionRepository _siteDefinitionRepository;

        private const int MaxSitemapEntryCount = 50000;

        private SitemapData _sitemapData;
        private readonly HashSet<string> _urlSet;
        private SiteDefinition _settings;
        private string _hostLanguageBranch;

        public CommerceSitemapXmlGenerator(ISitemapRepository sitemapRepository, IContentRepository contentRepository, UrlResolver urlResolver, SiteDefinitionRepository siteDefinitionRepository)
        {
            this._sitemapRepository = sitemapRepository;
            this._contentRepository = contentRepository;
            this._urlResolver = urlResolver;
            this._siteDefinitionRepository = siteDefinitionRepository;
            this._urlSet = new HashSet<string>();
        }

        public bool Generate(SitemapData sitemapData, out int entryCount)
        {
            try
            {
                this._sitemapData = sitemapData;
                var sitemapSiteUri = new Uri(this._sitemapData.SiteUrl);
                this._settings = GetSiteDefinitionFromSiteUri(sitemapSiteUri);
                this._hostLanguageBranch = GetHostLanguageBranch();

                XElement sitemap = CreateSitemapXmlContents(out entryCount);

                var doc = new XDocument(new XDeclaration("1.0", "utf-8", null));
                doc.Add(sitemap);

                using (var ms = new MemoryStream())
                {
                    var xtw = new XmlTextWriter(ms, Encoding.UTF8);
                    doc.Save(xtw);
                    xtw.Flush();
                    sitemapData.Data = ms.ToArray();
                }

                this._sitemapRepository.Save(sitemapData);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Error generating commerce xml sitemap" + Environment.NewLine + ex);
                entryCount = 0;
                return false;
            }

            return false;
        }

        public bool IsDebugMode { get; set; }

        /// <summary>
        /// Creates xml content for a given sitemap configuration entity
        /// </summary>
        /// <param name="entryCount">out: count of sitemap entries in the returned element</param>
        /// <returns>XElement that contains sitemap entries according to the configuration</returns>
        private XElement CreateSitemapXmlContents(out int entryCount)
        {
            XElement sitemapElement = GenerateRootElement();

            sitemapElement.Add(GetSitemapXmlElements());

            entryCount = _urlSet.Count;
            return sitemapElement;
        }

        private IEnumerable<XElement> GetSitemapXmlElements()
        {

            if (this._settings == null)
            {
                return Enumerable.Empty<XElement>();
            }

            var referenceConverter = ServiceLocator.Current.GetInstance<ReferenceConverter>();

            IList<ContentReference> descendants = this._contentRepository.GetDescendents(referenceConverter.GetRootLink()).ToList();

            return GenerateXmlElements(descendants);
        }

        private IEnumerable<XElement> GenerateXmlElements(IEnumerable<ContentReference> pages)
        {
            IList<XElement> sitemapXmlElements = new List<XElement>();

            foreach (ContentReference contentReference in pages)
            {
                var page = this._contentRepository.Get<CatalogContentBase>(contentReference);

                //if (ExcludePageLanguageFromSitemap(page))
                //{
                //    continue;
                //}

                if (this._urlSet.Count >= MaxSitemapEntryCount)
                {
                    this._sitemapData.ExceedsMaximumEntryCount = true;
                    return sitemapXmlElements;
                }

                AddFilteredPageElement(page, sitemapXmlElements);
            }

            return sitemapXmlElements;
        }

        private void AddFilteredPageElement(CatalogContentBase page, IList<XElement> xmlElements)
        {
            if (page.ShouldExcludeContent())
            {
                return;
            }

            try
            {
                string url = this._urlResolver.GetUrl(page.ContentLink);

                if (string.IsNullOrEmpty(url))
                {
                    return;
                }

                Uri absoluteUri;

                // if the URL is relative we add the base site URL (protocol and hostname)
                if (!IsAbsoluteUrl(url, out absoluteUri))
                {
                    url = UriSupport.Combine(this._sitemapData.SiteUrl, url);
                }
                // Force the SiteUrl
                else
                {
                    url = UriSupport.Combine(this._sitemapData.SiteUrl, absoluteUri.AbsolutePath);
                }

                var fullPageUrl = new Uri(url);

                if (this._urlSet.Contains(fullPageUrl.ToString()) || UrlFilter.IsUrlFiltered(fullPageUrl.AbsolutePath, this._sitemapData))
                {
                    return;
                }

                XElement pageElement = this.GenerateSiteElement(page, fullPageUrl.ToString());

                xmlElements.Add(pageElement);
                this._urlSet.Add(fullPageUrl.ToString());
            }
            catch (Exception ex)
            {

            }

        }

        private XElement GenerateSiteElement(CatalogContentBase pageData, string url)
        {
            var property = pageData.Property[PropertySEOSitemaps.PropertyName] as PropertySEOSitemaps;

            var element = new XElement(
                SitemapXmlNamespace + "url",
                new XElement(SitemapXmlNamespace + "loc", url),
                new XElement(SitemapXmlNamespace + "lastmod", pageData.StartPublish.Value.ToString(DateTimeFormat)), // TODO use modified
                new XElement(SitemapXmlNamespace + "changefreq", (property != null) ? property.ChangeFreq : "weekly"),
                new XElement(SitemapXmlNamespace + "priority", (property != null) ? property.Priority : GetPriority(url)));

            if (IsDebugMode)
            {
                element.AddFirst(new XComment(
                    string.Format(
                        "content ID: '{0}', name: '{1}', language: '{2}'",
                        pageData.ContentLink.ID, pageData.Name, pageData.Language)));
            }

            return element;
        }

        private bool IsAbsoluteUrl(string url, out Uri absoluteUri)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out absoluteUri);
        }

        private XElement GenerateRootElement()
        {
            return new XElement(SitemapXmlNamespace + "urlset");
        }

        private XNamespace SitemapXmlNamespace
        {
            get { return @"http://www.sitemaps.org/schemas/sitemap/0.9"; }
        }

        /// <summary>
        /// TODO could return null if URL is changed. Since that's used as key. Return more descriptive error message.
        /// </summary>
        /// <param name="sitemapSiteUri"></param>
        /// <returns></returns>
        private SiteDefinition GetSiteDefinitionFromSiteUri(Uri sitemapSiteUri)
        {
            return this._siteDefinitionRepository
                .List()
                .FirstOrDefault(siteDef => siteDef.SiteUrl == sitemapSiteUri || siteDef.Hosts.Any(hostDef => hostDef.Name.Equals(sitemapSiteUri.Host, StringComparison.InvariantCultureIgnoreCase)));
        }

        private string GetHostLanguageBranch()
        {
            var hostDefinition = GetHostDefinition();

            return hostDefinition != null && hostDefinition.Language != null
                ? hostDefinition.Language.ToString()
                : null;
        }

        private HostDefinition GetHostDefinition()
        {
            var siteUrl = new Uri(this._sitemapData.SiteUrl);
            string sitemapHost = siteUrl.Host;

            return this._settings.Hosts.FirstOrDefault(x => x.Name.Equals(sitemapHost, StringComparison.InvariantCultureIgnoreCase)) ??
                   this._settings.Hosts.FirstOrDefault(x => x.Name.Equals(SiteDefinition.WildcardHostName));
        }

        private static string GetPriority(string url)
        {
            int depth = new Uri(url).Segments.Length - 1;

            return Math.Max(1.0 - (depth / 10.0), 0.5).ToString(CultureInfo.InvariantCulture);
        }
    }
}