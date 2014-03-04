using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Geta.SEO.Sitemaps.Entities;
using Geta.SEO.Sitemaps.Repositories;
using Geta.SEO.Sitemaps.Utils;
using log4net;

namespace Geta.SEO.Sitemaps.XML
{
    public abstract class SitemapXmlGenerator
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SitemapXmlGenerator));

        private readonly ISitemapRepository _sitemapRepository;

        private const int MaxSitemapEntryCount = 50000;

        private SitemapData _sitemapData;
        private readonly ISet<string> _urlSet;
        private SiteDefinition _settings;
        private string _hostLanguageBranch;

        public SitemapXmlGenerator(ISitemapRepository sitemapRepository)
        {
            this._sitemapRepository = sitemapRepository;

            this._urlSet = new HashSet<string>();
        }

        protected abstract XElement GenerateSiteElement(PageData pageData, string url);

        protected abstract XElement GenerateRootElement();

        /// <summary>
        /// Generates a xml sitemap about pages on site
        /// </summary>
        /// <param name="sitemapData">SitemapData object containing configuration info for sitemap</param>
        /// <param name="entryCount">out count of site entries in generated sitemap</param>
        /// <returns>True if sitemap generation successful, false if error encountered</returns>
        public virtual bool Generate(SitemapData sitemapData, out int entryCount)
        {
            try
            {
                this._sitemapData = sitemapData;
                var sitemapSiteUri = new Uri(this._sitemapData.SiteUrl);
                this._settings = GetSiteDefinitionFromSiteUrl(sitemapSiteUri);
                this._hostLanguageBranch = GetHostLanguageBranch();
                XElement sitemap = this.CreateSitemapXmlContents(out entryCount);

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
            catch (Exception e)
            {
                Log.Error("Error on generating xml sitemap" + Environment.NewLine + e);
                entryCount = 0;
                return false;
            }
        }

        /// <summary>
        /// Creates xml content for a given sitemap configuration entity
        /// </summary>
        /// <param name="entryCount">out: count of sitemap entries in the returned element</param>
        /// <returns>XElement that contains sitemap entries according to the configuration</returns>
        private XElement CreateSitemapXmlContents(out int entryCount)
        {
            XElement sitemapElement = this.GenerateRootElement();

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

            var rootPage = this._sitemapData.RootPageId < 0 ? this._settings.StartPage : new ContentReference(this._sitemapData.RootPageId);

            var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();

            IList<ContentReference> descendants = contentLoader.GetDescendents(rootPage).ToList();

            if (rootPage != ContentReference.RootPage)
            {
                descendants.Add(rootPage);
            }

            return GenerateXmlElements(descendants);
        }

        private IEnumerable<XElement> GenerateXmlElements(IEnumerable<ContentReference> pages)
        {
            IList<XElement> sitemapXmlElements = new List<XElement>();
            var contentRepository = ServiceLocator.Current.GetInstance<IContentRepository>();

            if (this._hostLanguageBranch == null)
            {
                foreach (ContentReference contentReference in pages)
                {
                    var languagePages = contentRepository.GetLanguageBranches<PageData>(contentReference);

                    foreach (PageData page in languagePages)
                    {
                        if (this._urlSet.Count >= MaxSitemapEntryCount)
                        {
                            this._sitemapData.ExceedsMaximumEntryCount = true;
                            return sitemapXmlElements;
                        }

                        AddFilteredPageElement(page, sitemapXmlElements);
                    }
                }
            }
            else
            {
                var languageSelector = new LanguageSelector(this._hostLanguageBranch);

                foreach (ContentReference contentReference in pages)
                {
                    PageData page;

                    if (contentRepository.TryGet(contentReference, languageSelector, out page))
                    {
                        if (this._urlSet.Count >= MaxSitemapEntryCount)
                        {
                            this._sitemapData.ExceedsMaximumEntryCount = true;
                            return sitemapXmlElements;
                        }

                        AddFilteredPageElement(page, sitemapXmlElements);
                    }
                }
            }

            return sitemapXmlElements;
        }

        private string GetHostLanguageBranch()
        {
            var hostDefinition = GetHostDefinition();

            return hostDefinition != null && hostDefinition.Language != null
                ? hostDefinition.Language.ToString()
                : null;
        }

        private SiteDefinition GetSiteDefinitionFromSiteUrl(Uri sitemapSiteUri)
        {
            var siteDefinitionRepository = ServiceLocator.Current.GetInstance<SiteDefinitionRepository>();
            return siteDefinitionRepository
                .List()
                .FirstOrDefault(siteDef => siteDef.SiteUrl == sitemapSiteUri || siteDef.Hosts.Any(hostDef => hostDef.Name.Equals(sitemapSiteUri.Host)));
        }

        private HostDefinition GetHostDefinition()
        {
            var siteUrl = new Uri(this._sitemapData.SiteUrl);
            string sitemapHost = siteUrl.Host;

            return this._settings.Hosts.FirstOrDefault(x => x.Name.Equals(sitemapHost, StringComparison.InvariantCultureIgnoreCase)) ??
                   this._settings.Hosts.FirstOrDefault(x => x.Name.Equals(SiteDefinition.WildcardHostName));
        }

        private void AddFilteredPageElement(PageData page, IList<XElement> xmlElements)
        {
            if (PageFilter.ShouldExcludePage(page))
            {
                return;
            }

            var urlResolver = ServiceLocator.Current.GetInstance<UrlResolver>();

            string url = urlResolver.GetUrl(page.ContentLink, page.LanguageBranch);

            if (this._hostLanguageBranch != null)
            {
                url = url.Replace(string.Format("/{0}/", this._hostLanguageBranch), "/");
            }

            // if the URL is relative we add the base site URL (protocol and hostname)
            if (!IsAbsoluteUrl(url))
            {
                url = UriSupport.Combine(this._sitemapData.SiteUrl, url);
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

        private bool IsAbsoluteUrl(string url)
        {
            Uri result;
            return Uri.TryCreate(url, UriKind.Absolute, out result);
        }
    }
}