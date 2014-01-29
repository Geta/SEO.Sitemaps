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
                    xmlGenerator = new MobileSitemapXmlGenerator(_sitemapRepository);
                    break;

                default:
                    xmlGenerator = new StandardSitemapXmlGenerator(_sitemapRepository);
                    break;
            }

            xmlGenerator.IsDebugMode = sitemapData.IncludeDebugInfo;

            return xmlGenerator;
        } 
    }
}