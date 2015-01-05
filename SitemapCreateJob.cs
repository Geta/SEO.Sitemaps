using System;
using System.Text;
using System.Collections.Generic;
using EPiServer.BaseLibrary.Scheduling;
using EPiServer.PlugIn;
using EPiServer.ServiceLocation;
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

        private bool _stopSignaled;

        public SitemapCreateJob()
        {
            IsStoppable = true;

            this._sitemapRepository = ServiceLocator.Current.GetInstance<ISitemapRepository>();
            this._sitemapXmlGeneratorFactory = ServiceLocator.Current.GetInstance<SitemapXmlGeneratorFactory>();
        }

        public override string Execute()
        {
            OnStatusChanged("Starting generation of sitemaps");
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
                if (_stopSignaled)
                {
                    return "Stop of job was called";
                }

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

        public override void Stop()
        {
            _stopSignaled = true;
        }
    }
}
