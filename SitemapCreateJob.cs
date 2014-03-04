using System.Text;
using System.Collections.Generic;
using EPiServer.BaseLibrary.Scheduling;
using EPiServer.PlugIn;
using Geta.SEO.Sitemaps.Entities;
using Geta.SEO.Sitemaps.Repositories;
using Geta.SEO.Sitemaps.Utils;

namespace Geta.SEO.Sitemaps
{
    [ScheduledPlugIn(DisplayName = "Generate search engine sitemaps")]
    public class SitemapCreateJob : JobBase
    {
        private readonly ISitemapRepository _sitemapRepository;
        private readonly SitemapXmlGeneratorFactory _sitemapXmlGeneratorFactory;
        public SitemapCreateJob()
        {
            this._sitemapRepository = new SitemapRepository();
            this._sitemapXmlGeneratorFactory = new SitemapXmlGeneratorFactory(this._sitemapRepository);
        }

        public override string Execute()
        {
            var message = new StringBuilder();

            IList<SitemapData> sitemapConfigs = _sitemapRepository.GetAllSitemapData();

            // if no configuration present create one with default values
            if (sitemapConfigs.Count == 0)
            {
                _sitemapRepository.Save(CreateDefaultConfig());
            }

            // create xml sitemap for each configuration
            foreach (var sitemapConfig in sitemapConfigs)
            {
                this.GenerateSitemaps(sitemapConfig, message);
            }

            return string.Format("Job successfully executed.<br/>Generated sitemaps: {0}", message);
        }

        private void GenerateSitemaps(SitemapData sitemapConfig, StringBuilder message)
        {
            int entryCount;
            bool success = _sitemapXmlGeneratorFactory.GetSitemapXmlGenerator(sitemapConfig).Generate(sitemapConfig, out entryCount);

            if (success)
            {
                message.Append(string.Format("<br/>\"{0}{1}\": {2} entries", sitemapConfig.SiteUrl, sitemapConfig.Host, entryCount));
            }
            else
            {
                message.Append("<br/>Error creating sitemap for \"" + sitemapConfig.Host + "\"");
            }
        }

        private static SitemapData CreateDefaultConfig()
        {
            var blankConfig = new SitemapData
            {
                Host = "sitemap.xml",
                IncludeDebugInfo = false,
                SitemapFormat = SitemapFormat.Standard
            };
            
            return blankConfig;
        }
    }
}
