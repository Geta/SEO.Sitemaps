using System.Web.Routing;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using InitializationModule = EPiServer.Web.InitializationModule;

namespace Geta.SEO.Sitemaps
{
    [InitializableModule]
    [ModuleDependency(typeof (InitializationModule))]
    public class SitemapUrlRoutingInit : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            RouteTable.Routes.MapPageRoute("Sitemap with path",
                                           "{path}/sitemap.xml",
                                           "~/modules/Geta.SEO.Sitemaps/SitemapHandler.aspx");

            RouteTable.Routes.MapPageRoute("Sitemap without path",
                                           "sitemap.xml",
                                           "~/modules/Geta.SEO.Sitemaps/SitemapHandler.aspx");
        }

        public void Uninitialize(InitializationEngine context)
        {
        }

        public void Preload(string[] parameters)
        {
        }
    }
}