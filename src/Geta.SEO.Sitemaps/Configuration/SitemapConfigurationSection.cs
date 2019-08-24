// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Configuration;
using System.Web.Configuration;

namespace Geta.SEO.Sitemaps.Configuration
{
    public class SitemapConfigurationSection : ConfigurationSection
    {
        private static SitemapConfigurationSection _instance;
        private static readonly object Lock = new object();

        public static SitemapConfigurationSection Instance
        {
            get
            {
                lock (Lock)
                {
                    return _instance ?? (_instance = GetSection());
                }
            }
        }

        public static SitemapConfigurationSection GetSection()
        {
            var section = WebConfigurationManager.GetSection("Geta.SEO.Sitemaps") as SitemapConfigurationSection;

            if (section == null)
            {
                return new SitemapConfigurationSection();
            }

            return section;
        }

        [ConfigurationProperty("settings", IsRequired = true)]
        public SitemapSettings Settings
        {
            get
            {
                return (SitemapSettings)base["settings"];
            }
        }
    }
}
