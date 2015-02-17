using EPiServer.ServiceLocation;
using Geta.SEO.Sitemaps.Entities;
using Geta.SEO.Sitemaps.Repositories;
using Geta.SEO.Sitemaps.XML;

namespace Geta.SEO.Sitemaps.Utils
{
    public class SitemapXmlGeneratorFactory
    {
        private readonly ISitemapRepository _sitemapRepository;

        public SitemapXmlGeneratorFactory(ISitemapRepository sitemapRepository)
        {
            this._sitemapRepository = sitemapRepository;
        }

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
                default:
                    xmlGenerator = ServiceLocator.Current.GetInstance<StandardSitemapXmlGenerator>();
                    break;
            }

            xmlGenerator.IsDebugMode = sitemapData.IncludeDebugInfo;

            return xmlGenerator;
        } 
    }
}