// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using EPiServer;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using Geta.SEO.Sitemaps.Entities;
using Geta.SEO.Sitemaps.Repositories;
using Geta.SEO.Sitemaps.Utils;
using Geta.SEO.Sitemaps.XML;

namespace Geta.SEO.Sitemaps
{
    [ScheduledPlugIn(DisplayName = "Generate search engine sitemaps")]
    public class SitemapCreateJob : ScheduledJobBase
    {
        private readonly ISitemapRepository _sitemapRepository;
        private readonly SitemapXmlGeneratorFactory _sitemapXmlGeneratorFactory;
        private ISitemapXmlGenerator _currentGenerator;

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

            CacheManager.Insert("SitemapGenerationKey", DateTime.Now.Ticks);

            // create xml sitemap for each configuration
            foreach (var sitemapConfig in sitemapConfigs)
            {
                if (_stopSignaled)
                {
                    CacheManager.Remove("SitemapGenerationKey");
                    return "Stop of job was called.";
                }

                OnStatusChanged(string.Format("Generating {0}{1}.", sitemapConfig.SiteUrl, _sitemapRepository.GetHostWithLanguage(sitemapConfig)));
                this.GenerateSitemaps(sitemapConfig, message);
            }

            CacheManager.Remove("SitemapGenerationKey");

            if (_stopSignaled)
            {
                return "Stop of job was called.";
            }

            return string.Format("Job successfully executed.<br/>Generated sitemaps: {0}", message);
        }

        private void GenerateSitemaps(SitemapData sitemapConfig, StringBuilder message)
        {
            int entryCount;
            _currentGenerator = _sitemapXmlGeneratorFactory.GetSitemapXmlGenerator(sitemapConfig);
            bool success = _currentGenerator.Generate(sitemapConfig, true, out entryCount);

            if (success)
            {
                message.Append(string.Format("<br/>\"{0}{1}\": {2} entries", sitemapConfig.SiteUrl, _sitemapRepository.GetHostWithLanguage(sitemapConfig), entryCount));
            }
            else
            {
                message.Append("<br/>Error creating sitemap for \"" + _sitemapRepository.GetHostWithLanguage(sitemapConfig) + "\"");
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

            if (_currentGenerator != null)
            {
                _currentGenerator.Stop();
            }

            base.Stop();
        }
    }
}
