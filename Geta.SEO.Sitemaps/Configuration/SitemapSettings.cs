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

        [ConfigurationProperty("enableHrefLang", DefaultValue = false, IsRequired = false)]
        public bool EnableHrefLang
        {
            get
            {
                return (bool)this["enableHrefLang"];
            }
            set
            {
                this["enableHrefLang"] = value;
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