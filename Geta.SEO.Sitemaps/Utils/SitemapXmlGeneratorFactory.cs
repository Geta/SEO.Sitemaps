using EPiServer.ServiceLocation;
using Geta.SEO.Sitemaps.Entities;
using Geta.SEO.Sitemaps.XML;

namespace Geta.SEO.Sitemaps.Utils
{
    public class SitemapXmlGeneratorFactory
    {
        public ISitemapXmlGenerator GetSitemapXmlGenerator(SitemapData sitemapData)
        {
            ISitemapXmlGenerator xmlGenerator;

            switch (sitemapData.SitemapFormat)
            {
                case SitemapFormat.Mobile:
                    xmlGenerator = ServiceLocator.Current.GetInstance<MobileSitemapXmlGenerator>();
                    break;
                case SitemapFormat.Commerce:
                    xmlGenerator = ServiceLocator.Current.GetInstance<ICommerceSitemapXmlGenerator>();
                    break;
                case SitemapFormat.StandardAndCommerce:
                    xmlGenerator = ServiceLocator.Current.GetInstance<ICommerceAndStandardSitemapXmlGenerator>();
                    break;
                default:
                    xmlGenerator = ServiceLocator.Current.GetInstance<SitemapXmlGenerator>();
                    break;
            }

            xmlGenerator.IsDebugMode = sitemapData.IncludeDebugInfo;

            return xmlGenerator;
        } 
    }
}