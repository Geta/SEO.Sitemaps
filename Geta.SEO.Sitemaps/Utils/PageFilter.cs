using EPiServer.Core;
using EPiServer.Framework.Web;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using Geta.SEO.Sitemaps.SpecializedProperties;

namespace Geta.SEO.Sitemaps.Utils
{
    public class ContentFilter
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

        public static bool ShouldExcludeContent(IContent content)
        {
            if (content == null)
            {
                return true;
            }

            var securableContent = content as ISecurable;

            if (securableContent != null && !IsAccessibleToEveryone(securableContent))
            {
                return true;
            }

            if (content.IsDeleted)
            {
                return true;
            }

            if (!IsSitemapPropertyEnabled(content))
            {
                return true;
            }

            if (!IsVisibleOnSite(content))
            {
                return true;
            }

            if (!ServiceLocator.Current.GetInstance<TemplateResolver>().HasTemplate(content, TemplateTypeCategories.Page))
            {
                return false;
            }

            return false;
        }

        private static bool IsVisibleOnSite(PageData page)
        {
            return page.HasTemplate() && !page.IsPendingPublish && !string.IsNullOrEmpty(page.StaticLinkURL);
        }

        private static bool IsVisibleOnSite(IContent content)
        {
            var hasTemplate = ServiceLocator.Current.GetInstance<TemplateResolver>()
                .HasTemplate(content, TemplateTypeCategories.Page);

            if (!hasTemplate)
            {
                return false;
            }

            var versionableContent = content as IVersionable;

            if (versionableContent != null)
            {
                return !versionableContent.IsPendingPublish;
            }

            return true;
        }

        private static bool IsLink(PageData page)
        {
            return page.LinkType == PageShortcutType.External ||
                          page.LinkType == PageShortcutType.Shortcut ||
                          page.LinkType == PageShortcutType.Inactive;
        }

        private static bool IsSitemapPropertyEnabled(IContentData content)
        {
            var property = content.Property[PropertySEOSitemaps.PropertyName] as PropertySEOSitemaps;

            if (null != property && !property.Enabled)
            {
                return false;
            }

            return true;
        }

        private static bool IsAccessibleToEveryone(ISecurable content)
        {
            var visitorPrinciple = new System.Security.Principal.GenericPrincipal(
                new System.Security.Principal.GenericIdentity("visitor"),
                new[] { "Everyone" });

            return content.GetSecurityDescriptor().HasAccess(visitorPrinciple, AccessLevel.Read);
        }
    }
}
