// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Configuration;

namespace Geta.SEO.Sitemaps.Configuration
{
    public class SitemapSettings : ConfigurationElement
    {
        private static SitemapSettings _instance;
        private static readonly object Lock = new object();

        public static SitemapSettings Instance
        {
            get
            {
                lock (Lock)
                {
                    return _instance ?? (_instance = SitemapConfigurationSection.Instance.Settings);
                }
            }
        }

        [ConfigurationProperty("enableRealtimeSitemap", DefaultValue = false, IsRequired = false)]
        public bool EnableRealtimeSitemap
        {
            get
            {
                return (bool)this["enableRealtimeSitemap"];
            }
            set
            {
                this["enableRealtimeSitemap"] = value;
            }
        }

        [ConfigurationProperty("enableRealtimeCaching", DefaultValue = true, IsRequired = false)]
        public bool EnableRealtimeCaching
        {
            get
            {
                return (bool)this["enableRealtimeCaching"];
            }
            set
            {
                this["enableRealtimeCaching"] = value;
            }
        }

        [ConfigurationProperty("enableLanguageDropDownInAdmin", DefaultValue = false, IsRequired = false)]
        public bool EnableLanguageDropDownInAdmin
        {
            get
            {
                return (bool)this["enableLanguageDropDownInAdmin"];
            }
            set
            {
                this["enableLanguageDropDownInAdmin"] = value;
            }
        }
    }
}