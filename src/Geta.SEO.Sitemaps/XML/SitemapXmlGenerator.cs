// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Caching;
using System.Xml;
using System.Xml.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Framework.Cache;
using EPiServer.Logging.Compatibility;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Geta.SEO.Sitemaps.Entities;
using Geta.SEO.Sitemaps.Models;
using Geta.SEO.Sitemaps.Repositories;
using Geta.SEO.Sitemaps.SpecializedProperties;
using Geta.SEO.Sitemaps.Utils;

namespace Geta.SEO.Sitemaps.XML
{
    public abstract class SitemapXmlGenerator : ISitemapXmlGenerator
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SitemapXmlGenerator));
        protected const int MaxSitemapEntryCount = 50000;
        protected ISet<string> UrlSet { get; private set; }
        protected bool StopGeneration { get; private set; }
        protected string HostLanguageBranch { get; set; }

        protected const string DateTimeFormat = "yyyy-MM-ddTHH:mm:sszzz";

        protected readonly ISitemapRepository SitemapRepository;
        protected readonly IContentRepository ContentRepository;
        protected readonly IUrlResolver UrlResolver;
        protected readonly ISiteDefinitionRepository SiteDefinitionRepository;
        protected readonly ILanguageBranchRepository LanguageBranchRepository;
        protected readonly IContentFilter ContentFilter;
        protected SitemapData SitemapData { get; set; }
        protected SiteDefinition SiteSettings { get; set; }
        protected IEnumerable<LanguageBranch> EnabledLanguages { get; set; }
        protected IEnumerable<CurrentLanguageContent> HrefLanguageContents { get; set; }

        protected XNamespace SitemapXmlNamespace
        {
            get { return @"http://www.sitemaps.org/schemas/sitemap/0.9"; }
        }

        protected XNamespace SitemapXhtmlNamespace
        {
            get { return @"http://www.w3.org/1999/xhtml"; }
        }

        public bool IsDebugMode { get; set; }

        protected SitemapXmlGenerator(ISitemapRepository sitemapRepository, IContentRepository contentRepository, IUrlResolver urlResolver, ISiteDefinitionRepository siteDefinitionRepository, ILanguageBranchRepository languageBranchRepository,
            IContentFilter contentFilter)
        {
            this.SitemapRepository = sitemapRepository;
            this.ContentRepository = contentRepository;
            this.UrlResolver = urlResolver;
            this.SiteDefinitionRepository = siteDefinitionRepository;
            this.LanguageBranchRepository = languageBranchRepository;
            this.EnabledLanguages = this.LanguageBranchRepository.ListEnabled();
            this.UrlSet = new HashSet<string>();
            this.ContentFilter = contentFilter;
        }

        protected virtual XElement GenerateRootElement()
        {
            var rootElement = new XElement(SitemapXmlNamespace + "urlset");

            if (this.SitemapData.IncludeAlternateLanguagePages)
            {
                rootElement.Add(new XAttribute(XNamespace.Xmlns + "xhtml", SitemapXhtmlNamespace));
            }

            return rootElement;
        }

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
                this.HostLanguageBranch = GetHostLanguageBranch();
                SiteDefinition.Current = SiteSettings;
                XElement sitemap = CreateSitemapXmlContents(out entryCount);

                var doc = new XDocument(new XDeclaration("1.0", "utf-8", null));
                doc.Add(sitemap);

                using (var ms = new MemoryStream())
                {
                    var xtw = new XmlTextWriter(ms, new UTF8Encoding(false));
                    doc.Save(xtw);
                    xtw.Flush();
                    sitemapData.Data = ms.ToArray();
                }

                if (persistData && !StopGeneration)
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

        public void Stop()
        {
            StopGeneration = true;
        }

        /// <summary>
        /// Creates xml content for a given sitemap configuration entity
        /// </summary>
        /// <param name="entryCount">out: count of sitemap entries in the returned element</param>
        /// <returns>XElement that contains sitemap entries according to the configuration</returns>
        private XElement CreateSitemapXmlContents(out int entryCount)
        {
            IEnumerable<XElement> sitemapXmlElements = GetSitemapXmlElements();

            XElement sitemapElement = GenerateRootElement();

            sitemapElement.Add(sitemapXmlElements);

            entryCount = UrlSet.Count;
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

            if (!ContentReference.RootPage.CompareToIgnoreWorkID(rootPage))
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
                if (StopGeneration)
                {
                    return Enumerable.Empty<XElement>();
                }

                if (TryGet<IExcludeFromSitemap>(contentReference, out _))
                {
                    continue;
                }

                var contentLanguages = this.GetLanguageBranches(contentReference);

                foreach (var contentLanguageInfo in contentLanguages)
                {
                    if (StopGeneration)
                    {
                        return Enumerable.Empty<XElement>();
                    }

                    var localeContent = contentLanguageInfo.Content as ILocale;

                    if (localeContent != null && ExcludeContentLanguageFromSitemap(localeContent.Language))
                    {
                        continue;
                    }

                    if (this.UrlSet.Count >= MaxSitemapEntryCount)
                    {
                        this.SitemapData.ExceedsMaximumEntryCount = true;
                        return sitemapXmlElements;
                    }

                    AddFilteredContentElement(contentLanguageInfo, sitemapXmlElements);
                }
            }

            return sitemapXmlElements;
        }

        protected virtual IEnumerable<CurrentLanguageContent> GetLanguageBranches(ContentReference contentLink)
        {
            bool isSpecificLanguage = !string.IsNullOrWhiteSpace(this.SitemapData.Language);

            if (isSpecificLanguage)
            {
                LanguageSelector languageSelector = !this.SitemapData.EnableLanguageFallback
                    ? new LanguageSelector(this.SitemapData.Language)
                    : LanguageSelector.Fallback(this.SitemapData.Language, false);

                if (TryGet<IContent>(contentLink, out var contentData, languageSelector))
                {
                    return new[] { new CurrentLanguageContent { Content = contentData, CurrentLanguage = new CultureInfo(this.SitemapData.Language), MasterLanguage = GetMasterLanguage(contentData) } };
                }

                return Enumerable.Empty<CurrentLanguageContent>();
            }

            if (this.SitemapData.EnableLanguageFallback)
            {
                return GetFallbackLanguageBranches(contentLink);
            }

            if (TryGetLanguageBranches<IContent>(contentLink, out var contentLanguages))
            {
                return contentLanguages.Select(x => new CurrentLanguageContent { Content = x, CurrentLanguage = GetCurrentLanguage(x), MasterLanguage = GetMasterLanguage(x) });
            }
            return Enumerable.Empty<CurrentLanguageContent>();
        }

        protected virtual IEnumerable<CurrentLanguageContent> GetFallbackLanguageBranches(ContentReference contentLink)
        {
            foreach (var languageBranch in this.EnabledLanguages)
            {
                var languageContent = ContentRepository.Get<IContent>(contentLink, LanguageSelector.Fallback(languageBranch.Culture.Name, false));

                if (languageContent == null)
                {
                    continue;
                }

                yield return new CurrentLanguageContent { Content = languageContent, CurrentLanguage = languageBranch.Culture, MasterLanguage = GetMasterLanguage(languageContent) };
            }
        }

        protected virtual IEnumerable<HrefLangData> GetHrefLangDataFromCache(ContentReference contentLink)
        {
            var cacheKey = string.Format("HrefLangData-{0}", contentLink.ToReferenceWithoutVersion());
            var cachedObject = CacheManager.Get(cacheKey) as IEnumerable<HrefLangData>;

            if (cachedObject == null)
            {
                cachedObject = GetHrefLangData(contentLink);
                CacheManager.Insert(cacheKey, cachedObject, new CacheEvictionPolicy(null, new[] { "SitemapGenerationKey" }, TimeSpan.FromMinutes(10), CacheTimeoutType.Absolute));
            }

            return cachedObject;
        }

        protected virtual IEnumerable<HrefLangData> GetHrefLangData(ContentReference contentLink)
        {
            foreach (var languageBranch in this.EnabledLanguages)
            {
                var languageContent = ContentRepository.Get<IContent>(contentLink, LanguageSelector.Fallback(languageBranch.Culture.Name, false));

                if (languageContent == null || ContentFilter.ShouldExcludeContent(languageContent))
                {
                    continue;
                }

                var hrefLangData = CreateHrefLangData(languageContent, languageBranch.Culture, GetMasterLanguage(languageContent));
                yield return hrefLangData;

                if (hrefLangData.HrefLang == "x-default")
                {
                    yield return new HrefLangData
                    {
                        HrefLang = languageBranch.Culture.Name.ToLowerInvariant(),
                        Href = hrefLangData.Href
                    };
                }
            }
        }

        protected virtual HrefLangData CreateHrefLangData(IContent content, CultureInfo language, CultureInfo masterLanguage)
        {
            string languageUrl;
            string masterLanguageUrl;

            if (this.SitemapData.EnableSimpleAddressSupport && content is PageData pageData && !string.IsNullOrWhiteSpace(pageData.ExternalURL))
            {
                languageUrl = pageData.ExternalURL;
                
                TryGet(content.ContentLink, out IContent masterContent, new LanguageSelector(masterLanguage.Name));
                
                masterLanguageUrl = string.Empty;
                if (masterContent is PageData masterPageData && !string.IsNullOrWhiteSpace(masterPageData.ExternalURL))
                {
                    masterLanguageUrl = masterPageData.ExternalURL;
                }
            }
            else
            {
                languageUrl = UrlResolver.GetUrl(content.ContentLink, language.Name);
                masterLanguageUrl = UrlResolver.GetUrl(content.ContentLink, masterLanguage.Name);
            }


            var data = new HrefLangData();

            if (languageUrl.Equals(masterLanguageUrl) && content.ContentLink.CompareToIgnoreWorkID(this.SiteSettings.StartPage))
            {

                data.HrefLang = "x-default";
            }
            else
            {
                data.HrefLang = language.Name.ToLowerInvariant();
            }

            data.Href = GetAbsoluteUrl(languageUrl);
            return data;
        }

        protected virtual XElement GenerateSiteElement(IContent contentData, string url)
        {
            DateTime modified = DateTime.Now.AddMonths(-1);

            var changeTrackableContent = contentData as IChangeTrackable;
            var versionableContent = contentData as IVersionable;

            if (changeTrackableContent != null)
            {
                modified = changeTrackableContent.Saved;
            }
            else if (versionableContent != null && versionableContent.StartPublish.HasValue)
            {
                modified = versionableContent.StartPublish.Value;
            }

            var property = contentData.Property[PropertySEOSitemaps.PropertyName] as PropertySEOSitemaps;

            var element = new XElement(
                SitemapXmlNamespace + "url",
                new XElement(SitemapXmlNamespace + "loc", url),
                new XElement(SitemapXmlNamespace + "lastmod", modified.ToString(DateTimeFormat, CultureInfo.InvariantCulture)),
                new XElement(SitemapXmlNamespace + "changefreq", (property != null && !property.IsNull) ? property.ChangeFreq : "weekly"),
                new XElement(SitemapXmlNamespace + "priority", (property != null && !property.IsNull) ? property.Priority : GetPriority(url))
            );

            if (this.SitemapData.IncludeAlternateLanguagePages)
            {
                AddHrefLangToElement(contentData, element);
            }

            if (IsDebugMode)
            {
                var localeContent = contentData as ILocale;
                var language = localeContent != null ? localeContent.Language : CultureInfo.InvariantCulture;
                var contentName = Regex.Replace(contentData.Name, "[-]+", "", RegexOptions.None);

                element.AddFirst(new XComment($"page ID: '{contentData.ContentLink.ID}', name: '{contentName}', language: '{language.Name}'"));
            }

            return element;
        }

        protected virtual void AddHrefLangToElement(IContent content, XElement element)
        {
            var localeContent = content as ILocalizable;

            if (localeContent == null)
            {
                return;
            }

            var hrefLangDatas = GetHrefLangDataFromCache(content.ContentLink);
            var count = hrefLangDatas.Count();

            if (count < 2)
            {
                return;
            }

            if (count == 2 && hrefLangDatas.Count(x => x.HrefLang == "x-default") == 1)
            {
                return;
            }

            foreach (var hrefLangData in hrefLangDatas)
            {
                element.Add(CreateHrefLangElement(hrefLangData));
            }
        }

        protected virtual void AddFilteredContentElement(CurrentLanguageContent languageContentInfo,
            IList<XElement> xmlElements)
        {
            if (ContentFilter.ShouldExcludeContent(languageContentInfo, SiteSettings, SitemapData))
            {
                return;
            }

            var content = languageContentInfo.Content;
            string url = null;

            if (this.SitemapData.EnableSimpleAddressSupport && content is PageData pageData && !string.IsNullOrWhiteSpace(pageData.ExternalURL))
            {
                url = pageData.ExternalURL;
            }

            if (string.IsNullOrWhiteSpace(url))
            {
                var localizableContent = content as ILocalizable;

                if (localizableContent != null)
                {
                    string language = string.IsNullOrWhiteSpace(this.SitemapData.Language)
                        ? languageContentInfo.CurrentLanguage.Name
                        : this.SitemapData.Language;

                    url = this.UrlResolver.GetUrl(content.ContentLink, language);

                    if (string.IsNullOrWhiteSpace(url))
                    {
                        return;
                    }

                    // Make 100% sure we remove the language part in the URL if the sitemap host is mapped to the page's LanguageBranch.
                    if (this.HostLanguageBranch != null && localizableContent.Language.Name.Equals(this.HostLanguageBranch,
                            StringComparison.InvariantCultureIgnoreCase))
                    {
                        url = url.Replace(string.Format("/{0}/", this.HostLanguageBranch), "/");
                    }
                }
                else
                {
                    url = this.UrlResolver.GetUrl(content.ContentLink);

                    if (string.IsNullOrWhiteSpace(url))
                    {
                        return;
                    }
                }
            }

            url = GetAbsoluteUrl(url);

            var fullContentUrl = new Uri(url);

            if (this.UrlSet.Contains(fullContentUrl.ToString()) || UrlFilter.IsUrlFiltered(fullContentUrl.AbsolutePath, this.SitemapData))
            {
                return;
            }

            XElement contentElement = this.GenerateSiteElement(content, fullContentUrl.ToString());

            if (contentElement == null)
            {
                return;
            }

            xmlElements.Add(contentElement);
            this.UrlSet.Add(fullContentUrl.ToString());
        }

        protected virtual XElement CreateHrefLangElement(HrefLangData data)
        {
            return new XElement(
                SitemapXhtmlNamespace + "link",
                new XAttribute("rel", "alternate"),
                new XAttribute("hreflang", data.HrefLang),
                new XAttribute("href", data.Href)
                );
        }

        protected virtual string GetPriority(string url)
        {
            int depth = new Uri(url).Segments.Length - 1;

            return Math.Max(1.0 - (depth / 10.0), 0.5).ToString(CultureInfo.InvariantCulture);
        }

        protected CultureInfo GetCurrentLanguage(IContent content)
        {
            var localizableContent = content as ILocalizable;

            if (localizableContent != null)
            {
                return localizableContent.Language;
            }

            return CultureInfo.InvariantCulture;
        }

        protected CultureInfo GetMasterLanguage(IContent content)
        {
            var localizableContent = content as ILocalizable;

            if (localizableContent != null)
            {
                return localizableContent.MasterLanguage;
            }

            return CultureInfo.InvariantCulture;
        }

        public SiteDefinition GetSiteDefinitionFromSiteUri(Uri sitemapSiteUri)
        {
            return this.SiteDefinitionRepository
                .List()
                .FirstOrDefault(siteDef => siteDef.SiteUrl == sitemapSiteUri || siteDef.Hosts.Any(hostDef => hostDef.Name.Equals(sitemapSiteUri.Authority, StringComparison.InvariantCultureIgnoreCase)));
        }

        protected string GetHostLanguageBranch()
        {
            var hostDefinition = GetHostDefinition();

            return hostDefinition != null && hostDefinition.Language != null
                ? hostDefinition.Language.Name
                : null;
        }

        protected bool HostDefinitionExistsForLanguage(string languageBranch)
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

        protected HostDefinition GetHostDefinition()
        {
            var siteUrl = new Uri(this.SitemapData.SiteUrl);
            string sitemapHost = siteUrl.Authority;

            return this.SiteSettings.Hosts.FirstOrDefault(x => x.Name.Equals(sitemapHost, StringComparison.InvariantCultureIgnoreCase)) ??
                   this.SiteSettings.Hosts.FirstOrDefault(x => x.Name.Equals(SiteDefinition.WildcardHostName));
        }

        protected bool ExcludeContentLanguageFromSitemap(CultureInfo language)
        {
            return this.HostLanguageBranch != null &&
               !this.HostLanguageBranch.Equals(language.Name, StringComparison.InvariantCultureIgnoreCase) &&
               HostDefinitionExistsForLanguage(language.Name);
        }

        protected string GetAbsoluteUrl(string url)
        {
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

            return url;
        }

        protected bool IsAbsoluteUrl(string url, out Uri absoluteUri)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out absoluteUri);
        }

        protected bool TryGet<T>(ContentReference contentLink, out T content, LoaderOptions settings = null) where T : IContentData
        {
            content = default(T);
            try
            {
                T local;
                var status = settings != null ? this.ContentRepository.TryGet<T>(contentLink, settings, out local)
                    : this.ContentRepository.TryGet<T>(contentLink, out local);
                content = (T)local;
                return status;
            }
            catch (Exception ex)
            {
                Log.Error($"Error TryGet for {nameof(contentLink)}: {contentLink?.ID}", ex);
            }

            return false;
        }

        protected bool TryGetLanguageBranches<T>(ContentReference contentLink, out IEnumerable<T> content) where T : IContentData
        {
            content = Enumerable.Empty<T>();
            try
            {
                content = this.ContentRepository.GetLanguageBranches<T>(contentLink);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Error TryGetLanguageBranches for {nameof(contentLink)}: {contentLink?.ID}", ex);
            }
            return false;
        }
    }
}
