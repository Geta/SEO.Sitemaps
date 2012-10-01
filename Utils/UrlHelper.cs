using System.Text;
using EPiServer;
using EPiServer.Core;
using EPiServer.Globalization;
using EPiServer.Web;

namespace Geta.SEO.Sitemaps.Utils
{
    public static class UrlHelper
    {
        public static string GetContentUrl(PageData page)
        {
            var pageUrl = GetPageContentUrl(page);

            var ub = new UrlBuilder(pageUrl);
            var host = ub.Host;

            if (!string.IsNullOrEmpty(host))
            {
                pageUrl = ub.Path;
            }

            return pageUrl;
        }

        private static string GetPageContentUrl(PageData page)
        {
            ContentLanguage.Instance.SetCulture(page.LanguageBranch);

            var urlBuilder = new UrlBuilder(page.LinkURL);

            if (UrlRewriteProvider.IsFurlEnabled)
            {
                Global.UrlRewriteProvider.ConvertToExternal(urlBuilder, page.PageLink, Encoding.UTF8);
            }

            return urlBuilder.Uri.ToString();
        }

        public static string CombineUrl(string baseUrl, string pageUrl)
        {
            var ub = new UrlBuilder(pageUrl);
            var host = ub.Host;

            if (!string.IsNullOrEmpty(host))
            {
                return pageUrl;
            }

            return UriSupport.Combine(baseUrl, pageUrl);
        }
    }
}
