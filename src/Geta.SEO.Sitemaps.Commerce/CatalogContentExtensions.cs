using System.Linq;
using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Framework.Web;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Geta.SEO.Sitemaps.SpecializedProperties;

namespace Geta.SEO.Sitemaps.Commerce
{
    public static class CatalogContentExtensions
    {
        public static bool ShouldExcludeContent(this CatalogContentBase catalogContent)
        {
            if (catalogContent == null)
            {
                return true;
            }

            if (catalogContent.IsPendingPublish)
            {
                return true;
            }

            var visitorPrinciple = new System.Security.Principal.GenericPrincipal(
                new System.Security.Principal.GenericIdentity("visitor"),
                new[] { "Everyone" });

            var securityDescriptor = catalogContent.GetSecurityDescriptor();

            if (!securityDescriptor.HasAccess(visitorPrinciple, AccessLevel.Read))
            {
                return true;
            }

            if (!IsSitemapPropertyEnabled(catalogContent))
            {
                return true;
            }

            if (catalogContent.IsDeleted)
            {
                return true;
            }

            if (!catalogContent.HasTemplate())
            {
                return true;
            }

            return false;
        }

        private static bool IsSitemapPropertyEnabled(CatalogContentBase page)
        {
            var property = page.Property[PropertySEOSitemaps.PropertyName] as PropertySEOSitemaps;

            if (null != property && !property.Enabled)
            {
                return false;
            }

            return true;
        }

        public static bool HasTemplate(this IContentData contentData)
        {
            if (contentData == null)
            {
                return false;
            }

            var templateRepository = ServiceLocator.Current.GetInstance<ITemplateRepository>();

            return templateRepository.List(contentData.GetOriginalType()).Any(x => x.TemplateTypeCategory.IsCategory(TemplateTypeCategories.Page));
        }
    }
}