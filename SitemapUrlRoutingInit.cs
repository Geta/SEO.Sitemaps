using System.Web;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Web;
using Geta.SEO.Sitemaps.Services;

using InitializationModule = EPiServer.Web.InitializationModule;

namespace Geta.SEO.Sitemaps
{
    [InitializableModule]
    [ModuleDependency(typeof(InitializationModule))]
    public class SitemapUrlRoutingInit : IInitializableModule
    {
        private readonly ISitemapRepository sitemapRepository;

        public SitemapUrlRoutingInit()
            : this(new SitemapRepository())
        {
        }

        public SitemapUrlRoutingInit(ISitemapRepository sitemapRepository)
        {
            this.sitemapRepository = sitemapRepository;
        }

        public void Initialize(InitializationEngine context)
        {
            UrlRewriteModuleBase.HttpRewriteInit += HttpRewriteInit;
        }

        public void Preload(string[] parameters)
        {
        }

        public void Uninitialize(InitializationEngine context)
        {
            UrlRewriteModuleBase.HttpRewriteInit -= HttpRewriteInit;
        }

        private void HttpRewriteInit(object sender, UrlRewriteEventArgs e)
        {
            var urm = (UrlRewriteModule)sender;

            urm.HttpRewritingToInternal += HttpRewritingToInternal;
        }

        public const string SitemapSessionKey = "SitemapDataSessionKey";

        private void HttpRewritingToInternal(object sender, UrlRewriteEventArgs e)
        {
            if (e.Url.Path.ToLower().EndsWith("sitemap.xml"))
            {
                var sitemap = sitemapRepository.GetSitemapData(e.Url.ToString());

                if (sitemap != null)
                {
                    HttpContext.Current.Items.Add(SitemapSessionKey, sitemap);

                    e.UrlContext.InternalUrl.Path = "/Modules/Geta.SEO.Sitemaps/SitemapHandler.ashx";
                    e.IsModified = true;
                }
            }
        }
    }
}