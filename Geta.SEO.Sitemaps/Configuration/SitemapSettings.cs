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

        [ConfigurationProperty("enableRealtimeSitemap", DefaultValue = false, IsRequired = true)]
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
    }
}