using System;
using System.ComponentModel.DataAnnotations;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using EPiServer.Reference.Commerce.Site.Infrastructure;
using Geta.SEO.Sitemaps.SpecializedProperties;

namespace EPiServer.Reference.Commerce.Site.Features.Campaign.Pages
{
    [ContentType(DisplayName = "Campaign page", GUID = "bfba39b8-3161-4d01-a543-f4b0e18e995b", Description = "A Page which is used to show campaign details.")]
    [ImageUrl("~/styles/images/page_type.png")]
    public class CampaignPage : PageData
    {
        [Display(Name = "Page Title", 
            GroupName = SystemTabNames.Content, 
            Order = 10)]
        [CultureSpecific]
        public virtual String PageTitle { get; set; }

        [Display(Name = "Main Content Area",
          Description = "This is the main content area",
          GroupName = SystemTabNames.Content,
          Order = 20)]
        public virtual ContentArea MainContentArea { get; set; }

        [Display(
            Name = "Seo sitemap settings",
            Description = "",
            Order = 100,
            GroupName = SiteTabs.SEO)]
        [UIHint("SeoSitemap")]
        [BackingType(typeof(PropertySEOSitemaps))]
        public virtual string SEOSitemaps { get; set; }

        public override void SetDefaultValues(ContentType contentType)
        {
            base.SetDefaultValues(contentType);
            var sitemap = new PropertySEOSitemaps
            {
                Enabled = false
            };
            sitemap.Serialize();
            this.SEOSitemaps = sitemap.ToString();
        }
    }
}