using EPiServer.Core;
using Geta.SEO.Sitemaps.SpecializedProperties;

namespace Geta.SEO.Sitemaps.Utils
{
    public class PageFilter
    {
        public static bool ShouldExcludePage(PageData page)
        {
            if (page == null)
            {
                return true;
            }

            if (!IsAccessibleToEveryone(page))
            {
                return true;
            }                                             

            if (!page.CheckPublishedStatus(PagePublishedStatus.Published))
            {
                return true;
            }

            if (!IsVisibleOnSite(page))
            {
                return true;
            }

            if (IsLink(page))
            {
                return true;
            }

            if (!IsSitemapPropertyEnabled(page))
            {
                return true;
            }

            if (page.PageLink == ContentReference.WasteBasket)
            {
                return true;
            }

            if (page.IsDeleted)
            {
                return true;
            }

            if (!page.HasTemplate())
            {
                return true;
            }

            return false;
        }

        private static bool IsVisibleOnSite(PageData page)
        {
            return page.HasTemplate() && !page.IsPendingPublish && !string.IsNullOrEmpty(page.StaticLinkURL);
        }

        private static bool IsLink(PageData page)
        {
            return page.LinkType == PageShortcutType.External ||
                          page.LinkType == PageShortcutType.Shortcut ||
                          page.LinkType == PageShortcutType.Inactive;
        }

        private static bool IsSitemapPropertyEnabled(PageData page)
        {
            var property = page.Property[PropertySEOSitemaps.PropertyName] as PropertySEOSitemaps;

            if (null != property && !property.Enabled)
            {
                return false;
            }

            return true;
        }

        private static bool IsAccessibleToEveryone(PageData page)
        {
            var visitorPrinciple = new System.Security.Principal.GenericPrincipal(
                new System.Security.Principal.GenericIdentity("visitor"),
                new[] { "Everyone" });

            return page.ACL.QueryDistinctAccess(visitorPrinciple, EPiServer.Security.AccessLevel.Read);
        }
    }
}
