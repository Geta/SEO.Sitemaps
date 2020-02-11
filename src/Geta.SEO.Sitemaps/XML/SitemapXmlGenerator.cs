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
        protected readonly UrlResolver UrlResolver;
        protected readonly ISiteDefinitionRepository SiteDefinitionRepository;
        protected readonly ILanguageBranchRepository LanguageBranchRepository;
        protected readonly IContentFilter ContentFilter;
        protected SitemapData SitemapData { get; set; }
        protected SiteDefinition SiteSettings { get; set; }
        protected IEnumerable<LanguageBranch> EnabledLanguages { get; set; }

        protected XNamespace SitemapXmlNamespace => @"http://www.sitemaps.org/schemas/sitemap/0.9";

        protected XNamespace SitemapXhtmlNamespace => @"http://www.w3.org/1999/xhtml";

        public bool IsDebugMode { get; set; }

        protected SitemapXmlGenerator(
            ISitemapRepository sitemapRepository,
            IContentRepository contentRepository,
            UrlResolver urlResolver,
            ISiteDefinitionRepository siteDefinitionRepository,
            ILanguageBranchRepository languageBranchRepository,
            IContentFilter contentFilter)
        {
            SitemapRepository = sitemapRepository;
            ContentRepository = contentRepository;
            UrlResolver = urlResolver;
            SiteDefinitionRepository = siteDefinitionRepository;
            LanguageBranchRepository = languageBranchRepository;
            EnabledLanguages = LanguageBranchRepository.ListEnabled();
            UrlSet = new HashSet<string>();
            ContentFilter = contentFilter;
        }

        protected virtual XElement GenerateRootElement()
        {
            var rootElement = new XElement(SitemapXmlNamespace + "urlset");

            if (SitemapData.IncludeAlternateLanguagePages)
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
                SitemapData = sitemapData;
                var sitemapSiteUri = new Uri(SitemapData.SiteUrl);
                SiteSettings = GetSiteDefinitionFromSiteUri(sitemapSiteUri);
                HostLanguageBranch = GetHostLanguageBranch();
                SiteDefinition.Current = SiteSettings;
                var sitemap = CreateSitemapXmlContents(out entryCount);

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
                    SitemapRepository.Save(sitemapData);
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
            var sitemapXmlElements = GetSitemapXmlElements();

            var sitemapElement = GenerateRootElement();

            sitemapElement.Add(sitemapXmlElements);

            entryCount = UrlSet.Count;
            return sitemapElement;
        }

        protected virtual IEnumerable<XElement> GetSitemapXmlElements()
        {
            if (SiteSettings == null)
            {
                return Enumerable.Empty<XElement>();
            }

            var rootPage = SitemapData.RootPageId < 0
                ? SiteSettings.StartPage
                : new ContentReference(SitemapData.RootPageId);

            IList<ContentReference> descendants = ContentRepository.GetDescendents(rootPage).ToList();

            if (!ContentReference.RootPage.CompareToIgnoreWorkID(rootPage))
            {
                descendants.Add(rootPage);
            }

            return GenerateXmlElements(descendants);
        }

        protected virtual IEnumerable<IContent> GetSitemapContent(IEnumerable<ContentReference> pages)
        {
            return ContentRepository
                .GetItems(pages, LanguageSelector.MasterLanguage())
                .Where(x => x is IRoutable
                            && !(x is MediaData)
                            && !(x is IExcludeFromSitemap));
        }

        protected virtual IEnumerable<XElement> GenerateXmlElements(IEnumerable<ContentReference> pages)
        {
            IList<XElement> sitemapXmlElements = new List<XElement>();
            var contents = GetSitemapContent(pages);

            foreach (var content in contents)
            {
                if (StopGeneration)
                {
                    return Enumerable.Empty<XElement>();
                }

                var existingLanguages = content is ILocalizable localizableContent
                    ? localizableContent.ExistingLanguages
                    : new[] {CultureInfo.InvariantCulture};

                foreach (var language in existingLanguages)
                {
                    if (StopGeneration)
                    {
                        return Enumerable.Empty<XElement>();
                    }

                    if (!Equals(language, CultureInfo.InvariantCulture) && ExcludeContentLanguageFromSitemap(language))
                    {
                        continue;
                    }

                    if (UrlSet.Count >= MaxSitemapEntryCount)
                    {
                        SitemapData.ExceedsMaximumEntryCount = true;
                        return sitemapXmlElements;
                    }

                    AddFilteredContentElement(new CurrentLanguageContent
                    {
                        Content = content,
                        CurrentLanguage = language,
                        MasterLanguage = GetMasterLanguage(content)
                    }, sitemapXmlElements);
                }
            }

            return sitemapXmlElements;
        }

        protected virtual IEnumerable<HrefLangData> GetHrefLangData(IContent content)
        {
            var localizableContent = content as ILocalizable;
            var existingLanguages = localizableContent?.ExistingLanguages ?? Enumerable.Empty<CultureInfo>();

            foreach (var language in existingLanguages)
            {
                if (!ContentRepository.TryGet(content.ContentLink, language, out IContent languageVersion))
                {
                    continue;
                }

                if (ContentFilter.ShouldExcludeContent(languageVersion))
                {
                    continue;
                }

                var hrefLangData = CreateHrefLangData(languageVersion);

                if (hrefLangData.Href == null)
                {
                    continue;
                }

                yield return hrefLangData;

                if (hrefLangData.HrefLang == "x-default")
                {
                    yield return new HrefLangData {HrefLang = language.Name, Href = hrefLangData.Href};
                }
            }
        }

        protected virtual HrefLangData CreateHrefLangData(IContent content)
        {
            var localizableContent = (ILocalizable) content;
            var languageUrl = UrlResolver.GetUrl(content);

            if (string.IsNullOrEmpty(languageUrl))
            {
                return new HrefLangData {Href = null};
            }

            var masterLanguageUrl = UrlResolver.GetUrl(content.ContentLink, localizableContent.MasterLanguage.Name);
            var data = new HrefLangData
            {
                HrefLang = languageUrl.Equals(masterLanguageUrl)
                    ? "x-default"
                    : localizableContent.Language.Name.ToLowerInvariant(),
                Href = GetAbsoluteUrl(languageUrl)
            };

            return data;
        }

        protected virtual void AddHrefLangToElement(IContent content, XElement element)
        {
            var langData = GetHrefLangData(content).ToList();
            var langCount = langData.Count;

            if (langCount < 2 || langCount == 2 && langData.Count(x => x.HrefLang == "x-default") == 1)
            {
                return;
            }

            foreach (var data in langData)
            {
                element.Add(CreateHrefLangElement(data));
            }
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

        protected virtual void AddFilteredContentElement(
            CurrentLanguageContent languageContentInfo,
            IList<XElement> xmlElements)
        {
            if (ContentFilter.ShouldExcludeContent(languageContentInfo, SiteSettings, SitemapData))
            {
                return;
            }

            var content = languageContentInfo.Content;
            string url;

            if (content is ILocalizable localizableContent)
            {
                var language = string.IsNullOrWhiteSpace(SitemapData.Language)
                    ? languageContentInfo.CurrentLanguage.Name
                    : SitemapData.Language;

                url = UrlResolver.GetUrl(content.ContentLink, language);

                if (string.IsNullOrWhiteSpace(url))
                {
                    return;
                }

                // Make 100% sure we remove the language part in the URL if the sitemap host is mapped to the page's LanguageBranch.
                if (HostLanguageBranch != null &&
                    localizableContent.Language.Name.Equals(HostLanguageBranch, StringComparison.InvariantCultureIgnoreCase))
                {
                    url = url.Replace(string.Format("/{0}/", HostLanguageBranch), "/");
                }
            }
            else
            {
                url = UrlResolver.GetUrl(content.ContentLink);

                if (string.IsNullOrWhiteSpace(url))
                {
                    return;
                }
            }

            url = GetAbsoluteUrl(url);

            var fullContentUrl = new Uri(url);

            if (UrlSet.Contains(fullContentUrl.ToString()) ||
                UrlFilter.IsUrlFiltered(fullContentUrl.AbsolutePath, SitemapData))
            {
                return;
            }

            var contentElement = GenerateSiteElement(content, fullContentUrl.ToString());

            if (contentElement == null)
            {
                return;
            }

            xmlElements.Add(contentElement);
            UrlSet.Add(fullContentUrl.ToString());
        }

        protected virtual XElement GenerateSiteElement(IContent contentData, string url)
        {
            var modified = DateTime.Now.AddMonths(-1);

            var changeTrackableContent = contentData as IChangeTrackable;
            var versionableContent = contentData as IVersionable;

            if (changeTrackableContent != null)
            {
                modified = changeTrackableContent.Saved;
            }
            else if (versionableContent?.StartPublish != null)
            {
                modified = versionableContent.StartPublish.Value;
            }

            var property = contentData.Property[PropertySEOSitemaps.PropertyName] as PropertySEOSitemaps;

            var element = new XElement(
                SitemapXmlNamespace + "url",
                new XElement(SitemapXmlNamespace + "loc", url),
                new XElement(SitemapXmlNamespace + "lastmod",
                    modified.ToString(DateTimeFormat, CultureInfo.InvariantCulture)),
                new XElement(SitemapXmlNamespace + "changefreq",
                    (property != null && !property.IsNull) ? property.ChangeFreq : "weekly"),
                new XElement(SitemapXmlNamespace + "priority",
                    (property != null && !property.IsNull) ? property.Priority : GetPriority(url))
            );

            if (SitemapData.IncludeAlternateLanguagePages)
            {
                AddHrefLangToElement(contentData, element);
            }

            if (IsDebugMode)
            {
                var language = contentData is ILocale localeContent ? localeContent.Language : CultureInfo.InvariantCulture;
                var contentName = Regex.Replace(contentData.Name, "[-]+", "", RegexOptions.None);

                element.AddFirst(new XComment(
                    $"page ID: '{contentData.ContentLink.ID}', name: '{contentName}', language: '{language.Name}'"));
            }

            return element;
        }

        protected virtual string GetPriority(string url)
        {
            var depth = new Uri(url).Segments.Length - 1;

            return Math.Max(1.0 - (depth / 10.0), 0.5).ToString(CultureInfo.InvariantCulture);
        }

        protected virtual CultureInfo GetMasterLanguage(IContent content)
        {
            if (content is ILocalizable localizableContent)
            {
                return localizableContent.MasterLanguage;
            }

            return CultureInfo.InvariantCulture;
        }

        protected virtual SiteDefinition GetSiteDefinitionFromSiteUri(Uri sitemapSiteUri)
        {
            return SiteDefinitionRepository
                .List()
                .FirstOrDefault(siteDef => siteDef.SiteUrl == sitemapSiteUri || siteDef.Hosts.Any(hostDef =>
                                               hostDef.Name.Equals(sitemapSiteUri.Host,
                                                   StringComparison.InvariantCultureIgnoreCase)));
        }

        protected virtual string GetHostLanguageBranch()
        {
            var hostDefinition = GetHostDefinition();

            return hostDefinition != null && hostDefinition.Language != null
                ? hostDefinition.Language.Name
                : null;
        }

        protected virtual bool HostDefinitionExistsForLanguage(string languageBranch)
        {
            var cacheKey = string.Format("HostDefinitionExistsFor{0}-{1}", SitemapData.SiteUrl, languageBranch);
            var cachedObject = HttpRuntime.Cache.Get(cacheKey);

            if (cachedObject == null)
            {
                cachedObject =
                    SiteSettings.Hosts.Any(
                        x =>
                            x.Language != null &&
                            x.Language.ToString().Equals(languageBranch, StringComparison.InvariantCultureIgnoreCase));

                HttpRuntime.Cache.Insert(cacheKey, cachedObject, null, DateTime.Now.AddMinutes(10),
                    Cache.NoSlidingExpiration);
            }

            return (bool) cachedObject;
        }

        protected virtual HostDefinition GetHostDefinition()
        {
            var siteUrl = new Uri(SitemapData.SiteUrl);
            var sitemapHost = siteUrl.Authority;

            return SiteSettings.Hosts.FirstOrDefault(x =>
                       x.Name.Equals(sitemapHost, StringComparison.InvariantCultureIgnoreCase)) ??
                   SiteSettings.Hosts.FirstOrDefault(x => x.Name.Equals(SiteDefinition.WildcardHostName));
        }

        protected virtual bool ExcludeContentLanguageFromSitemap(CultureInfo language)
        {
            return HostLanguageBranch != null &&
                   !HostLanguageBranch.Equals(language.Name, StringComparison.InvariantCultureIgnoreCase) &&
                   HostDefinitionExistsForLanguage(language.Name);
        }

        protected virtual string GetAbsoluteUrl(string url)
        {
            // Force the SiteUrl
            if (IsAbsoluteUrl(url, out var absoluteUri))
            {
                url = UriUtil.Combine(SitemapData.SiteUrl, absoluteUri.PathAndQuery);
            }
            // if the URL is relative we add the base site URL (protocol and hostname)
            else
            {
                url = UriUtil.Combine(SitemapData.SiteUrl, url);
            }

            return url;
        }

        protected virtual bool IsAbsoluteUrl(string url, out Uri absoluteUri)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out absoluteUri);
        }
    }
}