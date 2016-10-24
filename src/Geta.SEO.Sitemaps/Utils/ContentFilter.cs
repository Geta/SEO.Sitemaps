using System;
using System.Reflection;
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
        protected static Injected<TemplateResolver> TemplateResolver { get; set; }

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

            var versionableContent = content as IVersionable;

            if (versionableContent != null && !IsPublished(versionableContent))
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

            if (content.ContentLink.CompareToIgnoreWorkID(ContentReference.WasteBasket))
            {
                return true;
            }

            if (content is BlockData || content is MediaData)
            {
                return true;
            }

            var page = content as PageData;

            if (page != null && IsLink(page))
            {
                return true;
            }

            return false;
        }

        private static bool IsVisibleOnSite(IContent content)
        {
            return TemplateResolver.Service.HasTemplate(content, TemplateTypeCategories.Page);
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
            if (property==null) //not set on the page, check if there are default values for a page type perhaps 
            { 
                var page = content as PageData;
                if (page == null)
                    return true;

                var seoProperty = page.GetType().GetProperty(PropertySEOSitemaps.PropertyName);
                if (seoProperty?.GetValue(page) is PropertySEOSitemaps) //check unlikely situation when the property name is the same as defined for SEOSiteMaps
                {
                    var isEnabled= ((PropertySEOSitemaps)seoProperty.GetValue(page)).Enabled;
                    return isEnabled;
                }

            }

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

        private static bool IsPublished(IVersionable versionableContent)
        {
            var isPublished = versionableContent.Status == VersionStatus.Published;

            if (!isPublished || versionableContent.IsPendingPublish)
            {
                return false;
            }

            var now = DateTime.Now.ToUniversalTime();
            var startPublish = versionableContent.StartPublish.GetValueOrDefault(DateTime.MinValue).ToUniversalTime();
            var stopPublish = versionableContent.StopPublish.GetValueOrDefault(DateTime.MaxValue).ToUniversalTime();

            if (startPublish > now || stopPublish < now)
            {
                return false;
            }

            return true;
        }
    }
}
