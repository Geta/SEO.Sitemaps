using System.Collections.Generic;
using System.Xml.Linq;
using EPiServer;
using EPiServer.Configuration;
using EPiServer.Core;
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

        private static IList<XElement> GetSitemapXmlElements(SitemapData sitemapData, 
                                                            ISitemapXmlGenerator sitemapGenerator, 
                                                            ISet<string> urlSet)
        {
            var rootPage = sitemapData.RootPageId == 0
                               ? PageReference.RootPage
                               : new PageReference(sitemapData.RootPageId);

            var descendants = DataFactory.Instance.GetDescendents(rootPage);

            if (rootPage != PageReference.RootPage)
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

            var baseUrl = string.IsNullOrEmpty(sitemapData.SiteUrl)
                             ? Settings.Instance.SiteUrl.ToString()
                             : sitemapData.SiteUrl;

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

                    AddFilteredPageElement(page, baseUrl, urlSet, sitemapData, sitemapGenerator, sitemapXmlElements);
                }
            }

            return sitemapXmlElements;
        }

        private static void AddFilteredPageElement(PageData page,
                                                string baseUrl,
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
            string fullPageUrl = UrlHelper.CombineUrl(baseUrl, contentUrl);

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
