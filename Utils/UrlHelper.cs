using System.Text;
using EPiServer;
using EPiServer.Core;
using EPiServer.Globalization;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Routing;

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

			if (UrlRewriteProvider.IsFurlEnabled)
            {
				var urlResolver = ServiceLocator.Current.GetInstance<UrlResolver>();
	            return urlResolver.GetVirtualPath(page.ContentLink);
            }

	        return page.LinkURL;
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
