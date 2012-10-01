using System.Text;
using System.Collections.Generic;
using EPiServer.BaseLibrary.Scheduling;
using EPiServer.PlugIn;
using Geta.SEO.Sitemaps.Entities;
using Geta.SEO.Sitemaps.Services;

namespace Geta.SEO.Sitemaps
{
    [ScheduledPlugIn(DisplayName = "Generate search engine sitemaps")]
    public class SitemapCreateJob : JobBase
    {
        private readonly ISitemapService sitemapService;

        public SitemapCreateJob()
        {
            sitemapService = new SitemapService();
        }

        public override string Execute()
        {
            var builder = new StringBuilder();

            IList<SitemapData> sitemapConfigs = sitemapService.GetAllSitemapData();

            // if no configuration present create one with default values
            if (sitemapConfigs.Count == 0)
            {
                sitemapService.Save(CreateDefaultConfig());
            }

            // create xml sitemap for each configuration
            foreach (var sitemapConfig in sitemapConfigs)
            {
                GenerateSitemaps(sitemapConfig, builder);
            }   

            return "Job successfull. Generated sitemaps:" + builder;
        }

        private void GenerateSitemaps(SitemapData sitemapConfig, StringBuilder builder)
        {
            int entryCount;
            var success = sitemapService.Generate(sitemapConfig, out entryCount);

            if (success)
            {
                builder.Append(string.Format("<br/>\"{0}\": {1} entries", sitemapConfig.Host, entryCount));
            }
            else
            {
                builder.Append("<br/>Error creating sitemap for \"" + sitemapConfig.Host + "\"");
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
