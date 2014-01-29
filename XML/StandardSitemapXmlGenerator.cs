using System;
using System.Globalization;
using System.Xml.Linq;
using EPiServer.Core;
using Geta.SEO.Sitemaps.Repositories;
using Geta.SEO.Sitemaps.SpecializedProperties;

namespace Geta.SEO.Sitemaps.XML
{
    public class StandardSitemapXmlGenerator : SitemapXmlGenerator, ISitemapXmlGenerator
    {
        protected const string DateTimeFormat = "yyyy-MM-ddTHH:mm:sszzz";

        public StandardSitemapXmlGenerator(ISitemapRepository sitemapRepository) : base(sitemapRepository)
        {
        }

        protected XNamespace SitemapXmlNamespace
        {
            get { return @"http://www.sitemaps.org/schemas/sitemap/0.9"; }
        }

        public bool IsDebugMode { get; set; }

        protected override XElement GenerateSiteElement(PageData pageData, string url)
        {
            var property = pageData.Property[PropertySEOSitemaps.PropertyName] as PropertySEOSitemaps;

            var element = new XElement(
                SitemapXmlNamespace + "url",
                new XElement(SitemapXmlNamespace + "loc", url),
                new XElement(SitemapXmlNamespace + "lastmod", pageData.Saved.ToString(DateTimeFormat)),
                new XElement(SitemapXmlNamespace + "changefreq", (property != null) ? property.ChangeFreq : "weekly"),
                new XElement(SitemapXmlNamespace + "priority", (property != null) ? property.Priority : GetPriority(url)));

            if (IsDebugMode)
            {
                element.AddFirst(new XComment(
                    string.Format(
                        "page ID: '{0}', name: '{1}', language: '{2}'",
                        pageData.PageLink.ID, pageData.PageName, pageData.LanguageBranch)));
            }

            return element;
        }

        private static string GetPriority(string url)
        {
            int depth = new Uri(url).Segments.Length - 1;

            return Math.Max(1.0 - (depth / 10.0), 0.5).ToString(CultureInfo.InvariantCulture);
        }

        protected override XElement GenerateRootElement()
        {
            return new XElement(SitemapXmlNamespace + "urlset");
        }
    }
}
