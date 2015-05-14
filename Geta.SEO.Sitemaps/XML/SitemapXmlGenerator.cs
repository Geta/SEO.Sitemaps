using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Caching;
using System.Xml;
using System.Xml.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.Logging.Compatibility;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Geta.SEO.Sitemaps.Configuration;
using Geta.SEO.Sitemaps.Entities;
using Geta.SEO.Sitemaps.Repositories;
using Geta.SEO.Sitemaps.Utils;

namespace Geta.SEO.Sitemaps.XML
{
    public abstract class SitemapXmlGenerator
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SitemapXmlGenerator));
        private const int MaxSitemapEntryCount = 50000;
        private readonly ISet<string> _urlSet;
        private string _hostLanguageBranch;

        protected readonly ISitemapRepository SitemapRepository;
        protected readonly IContentRepository ContentRepository;
        protected readonly UrlResolver UrlResolver;
        protected readonly SiteDefinitionRepository SiteDefinitionRepository;
        protected SitemapData SitemapData { get; set; }
        protected SiteDefinition SiteSettings { get; set; }

        protected SitemapXmlGenerator(ISitemapRepository sitemapRepository, IContentRepository contentRepository, UrlResolver urlResolver, SiteDefinitionRepository siteDefinitionRepository)
        {
            this.SitemapRepository = sitemapRepository;
            this.ContentRepository = contentRepository;
            this.UrlResolver = urlResolver;
            this.SiteDefinitionRepository = siteDefinitionRepository;
            this._urlSet = new HashSet<string>();
        }

        protected abstract XElement GenerateSiteElement(IContent contentData, string url);

        protected abstract XElement GenerateRootElement();

        /// <summary>
        /// Generates a xml sitemap about pages on site
        /// </summary>
        /// <param name="sitemapData">SitemapData object containing configuration info for sitemap</param>
        /// <param name="persistData">True if the sitemap data should be persisted in DDS</param>
        /// <param name="entryCount">out count of site entries in generated sitemap</param>
        /// <returns>True if sitemap generation successful, false if error encountered</returns>
        public virtual bool Generate(SitemapData sitemapData, bool persistData, out int entryCount)
        {
            try
            {
                this.SitemapData = sitemapData;
                var sitemapSiteUri = new Uri(this.SitemapData.SiteUrl);
                this.SiteSettings = GetSiteDefinitionFromSiteUri(sitemapSiteUri);
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

                if (persistData)
                {
                    this.SitemapRepository.Save(sitemapData);
                }

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
            XElement sitemapElement = GenerateRootElement();

            sitemapElement.Add(GetSitemapXmlElements());

            entryCount = _urlSet.Count;
            return sitemapElement;
        }

        protected virtual IEnumerable<XElement> GetSitemapXmlElements()
        {

            if (this.SiteSettings == null)
            {
                return Enumerable.Empty<XElement>();
            }

            var rootPage = this.SitemapData.RootPageId < 0 ? this.SiteSettings.StartPage : new ContentReference(this.SitemapData.RootPageId);

            IList<ContentReference> descendants = this.ContentRepository.GetDescendents(rootPage).ToList();

            if (rootPage != ContentReference.RootPage)
            {
                descendants.Add(rootPage);
            }

            return GenerateXmlElements(descendants);
        }

        protected virtual IEnumerable<XElement> GenerateXmlElements(IEnumerable<ContentReference> pages)
        {
            IList<XElement> sitemapXmlElements = new List<XElement>();

            foreach (ContentReference contentReference in pages)
            {
                var languageContents = this.GetLanguageBranches(contentReference);

                foreach (var content in languageContents)
                {
                    var localeContent = content as ILocale;

                    if (localeContent != null && ExcludeContentLanguageFromSitemap(localeContent))
                    {
                        continue;
                    }

                    if (this._urlSet.Count >= MaxSitemapEntryCount)
                    {
                        this.SitemapData.ExceedsMaximumEntryCount = true;
                        return sitemapXmlElements;
                    }

                    AddFilteredContentElement(content, sitemapXmlElements);
                }
            }

            return sitemapXmlElements;
        }

        protected virtual IEnumerable<IContent> GetLanguageBranches(ContentReference contentLink)
        {
            if (!string.IsNullOrWhiteSpace(this.SitemapData.Language))
            {
                IContent contentData;
                ILanguageSelector languageSelector = this.SitemapData.EnableLanguageFallback
                    ? LanguageSelector.Fallback(this.SitemapData.Language, true) 
                    : new LanguageSelector(this.SitemapData.Language);

                if (this.ContentRepository.TryGet(contentLink, languageSelector, out contentData))
                {
                    return new [] { contentData };
                }

                return Enumerable.Empty<IContent>();
            }

            return this.ContentRepository.GetLanguageBranches<IContentData>(contentLink).OfType<IContent>();
        }

        private SiteDefinition GetSiteDefinitionFromSiteUri(Uri sitemapSiteUri)
        {
            return this.SiteDefinitionRepository
                .List()
                .FirstOrDefault(siteDef => siteDef.SiteUrl == sitemapSiteUri || siteDef.Hosts.Any(hostDef => hostDef.Name.Equals(sitemapSiteUri.Host, StringComparison.InvariantCultureIgnoreCase)));
        }

        private string GetHostLanguageBranch()
        {
            var hostDefinition = GetHostDefinition();

            return hostDefinition != null && hostDefinition.Language != null
                ? hostDefinition.Language.Name
                : null;
        }

        private bool HostDefinitionExistsForLanguage(string languageBranch)
        {
            var cacheKey = string.Format("HostDefinitionExistsFor{0}-{1}", this.SitemapData.SiteUrl, languageBranch);
            object cachedObject = HttpRuntime.Cache.Get(cacheKey);

            if (cachedObject == null)
            {
                cachedObject =
                    this.SiteSettings.Hosts.Any(
                        x =>
                        x.Language != null &&
                        x.Language.ToString().Equals(languageBranch, StringComparison.InvariantCultureIgnoreCase));

                HttpRuntime.Cache.Insert(cacheKey, cachedObject, null, DateTime.Now.AddMinutes(10), Cache.NoSlidingExpiration);
            }

            return (bool)cachedObject;
        }

        private HostDefinition GetHostDefinition()
        {
            var siteUrl = new Uri(this.SitemapData.SiteUrl);
            string sitemapHost = siteUrl.Host;

            return this.SiteSettings.Hosts.FirstOrDefault(x => x.Name.Equals(sitemapHost, StringComparison.InvariantCultureIgnoreCase)) ??
                   this.SiteSettings.Hosts.FirstOrDefault(x => x.Name.Equals(SiteDefinition.WildcardHostName));
        }

        private bool ExcludeContentLanguageFromSitemap(ILocale content)
        {
            return this._hostLanguageBranch != null &&
               !this._hostLanguageBranch.Equals(content.Language.Name, StringComparison.InvariantCultureIgnoreCase) &&
               HostDefinitionExistsForLanguage(content.Language.Name);
        }

        private void AddFilteredContentElement(IContent contentData, IList<XElement> xmlElements)
        {
            var page = contentData as PageData;

            if (page != null && ContentFilter.ShouldExcludePage((PageData)contentData))
            {
                return;
            }

            if (ContentFilter.ShouldExcludeContent(contentData))
            {
                return;
            }

            string url;

            var localizableContent = contentData as ILocalizable;

            if (localizableContent != null)
            {
                string language = string.IsNullOrWhiteSpace(this.SitemapData.Language)
                    ? localizableContent.Language.Name
                    : this.SitemapData.Language;

                url = this.UrlResolver.GetUrl(contentData.ContentLink, language);

                if (string.IsNullOrWhiteSpace(url))
                {
                    return;
                }

                // Make 100% sure we remove the language part in the URL if the sitemap host is mapped to the page's LanguageBranch.
                if (this._hostLanguageBranch != null && localizableContent.Language.Name.Equals(this._hostLanguageBranch, StringComparison.InvariantCultureIgnoreCase))
                {
                    url = url.Replace(string.Format("/{0}/", this._hostLanguageBranch), "/");
                }
            }
            else
            {
                url = this.UrlResolver.GetUrl(contentData.ContentLink);

                if (string.IsNullOrWhiteSpace(url))
                {
                    return;
                }
            }

            Uri absoluteUri;

            // if the URL is relative we add the base site URL (protocol and hostname)
            if (!IsAbsoluteUrl(url, out absoluteUri))
            {
                url = UriSupport.Combine(this.SitemapData.SiteUrl, url);
            }
            // Force the SiteUrl
            else
            {
                url = UriSupport.Combine(this.SitemapData.SiteUrl, absoluteUri.AbsolutePath);
            }

            var fullPageUrl = new Uri(url);

            if (this._urlSet.Contains(fullPageUrl.ToString()) || UrlFilter.IsUrlFiltered(fullPageUrl.AbsolutePath, this.SitemapData))
            {
                return;
            }

            XElement pageElement = this.GenerateSiteElement(contentData, fullPageUrl.ToString());

            xmlElements.Add(pageElement);
            this._urlSet.Add(fullPageUrl.ToString());
        }

        private bool IsAbsoluteUrl(string url, out Uri absoluteUri)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out absoluteUri);
        }
    }
}