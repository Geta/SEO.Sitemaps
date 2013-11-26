using System;
using System.Collections.Generic;
using System.Xml.Linq;
using EPiServer;
using EPiServer.Configuration;
using EPiServer.Core;
using EPiServer.Framework.Initialization;
using Geta.SEO.Sitemaps.Entities;
using Geta.SEO.Sitemaps.XML;

namespace Geta.SEO.Sitemaps.Utils
{
    public class SitemapContentHelper
    {
        private const int MaxSitemapEntryCount = 50000;

        /// <summary>
        /// Creates xml content for a given sitemap configuration entity
        /// </summary>
        /// <param name="sitemapData">sitemap configuration object</param>
        /// <param name="entryCount">out: count of sitemap entries in the returned element</param>
        /// <returns>XElement that contains sitemap entries according to the configuration</returns>
        public static XElement CreateSitemapXmlContents(SitemapData sitemapData, out int entryCount)
        {
            ISitemapXmlGenerator sitemapGenerator = GetSitemapXmlGenerator(sitemapData);

            var sitemapElement = sitemapGenerator.GenerateRootElement();

            ISet<string> urlSet = new HashSet<string>();

            sitemapElement.Add(GetSitemapXmlElements(sitemapData, sitemapGenerator, urlSet));

            entryCount = urlSet.Count;
            return sitemapElement;
        }

        private static ISitemapXmlGenerator GetSitemapXmlGenerator(SitemapData sitemapData)
        {
            ISitemapXmlGenerator xmlGenerator;

            switch (sitemapData.SitemapFormat)
            {
                case SitemapFormat.Mobile:
                    xmlGenerator = new MobileSitemapXmlGenerator();
                    break;

                default:
                    xmlGenerator = new StandardSitemapXmlGenerator();
                    break;
            }

            xmlGenerator.IsDebugMode = sitemapData.IncludeDebugInfo;

            return xmlGenerator;
        }

        private static IList<XElement> GetSitemapXmlElements(SitemapData sitemapData,  ISitemapXmlGenerator sitemapGenerator, ISet<string> urlSet)
        {
            Settings settings = Settings.MapUrlToSettings(new Uri(sitemapData.SiteUrl));
            PageReference rootPage = sitemapData.RootPageId < 0 ? new PageReference(settings.PageStartId) : new PageReference(sitemapData.RootPageId);

            var descendants = DataFactory.Instance.GetDescendents(rootPage);

            if (rootPage != ContentReference.RootPage)
            {
                descendants.Add(rootPage);
            }

            return GenerateXmlElements(descendants, urlSet, sitemapData, sitemapGenerator);
        }

        private static IList<XElement> GenerateXmlElements(IEnumerable<PageReference> pages,
                                                        ISet<string> urlSet,
                                                        SitemapData sitemapData,
                                                        ISitemapXmlGenerator sitemapGenerator)
        {
            IList<XElement> sitemapXmlElements = new List<XElement>();
            Uri baseUri = string.IsNullOrEmpty(sitemapData.SiteUrl)
				? Settings.Instance.SiteUrl
				: new Uri(sitemapData.SiteUrl);

            foreach (PageReference pageReference in pages)
            {
                var languagePages = DataFactory.Instance.GetLanguageBranches(pageReference);

                foreach (var page in languagePages)
                {
                    if (urlSet.Count >= MaxSitemapEntryCount)
                    {
                        sitemapData.ExceedsMaximumEntryCount = true;
                        return sitemapXmlElements;
                    }

                    AddFilteredPageElement(page, baseUri, urlSet, sitemapData, sitemapGenerator, sitemapXmlElements);
                }
            }

            return sitemapXmlElements;
        }

        private static void AddFilteredPageElement(PageData page,
                                                Uri baseUri,
                                                ISet<string> urlSet,
                                                SitemapData sitemapData,
                                                ISitemapXmlGenerator sitemapGenerator,
                                                IList<XElement> xmlElements)
        {
            // filter the page
            if (PageFilter.FilterPage(page))
            {
                return;
            }

            // get page url
            string contentUrl = UrlHelper.GetContentUrl(page);
	        string pageLanguage = page.LanguageBranch.ToLower();
	        string defaultLanguage = SiteMappingConfiguration.Instance.LanguageForHost(baseUri.Host);

			if (pageLanguage.Equals(defaultLanguage))
			{
				contentUrl = contentUrl.Replace(string.Format("/{0}/", defaultLanguage), "/");
			}

            string fullPageUrl = UrlHelper.CombineUrl(baseUri.ToString(), contentUrl);

            // filter url
            if (urlSet.Contains(fullPageUrl) || UrlFilter.IsUrlFiltered(contentUrl, sitemapData))
            {
                return;
            }

            // get xml element
            var pageElement = sitemapGenerator.GenerateSiteElement(page, fullPageUrl);

            xmlElements.Add(pageElement);
            urlSet.Add(fullPageUrl);
        }
    }
}
