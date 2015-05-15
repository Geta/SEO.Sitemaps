using System.Web.Mvc;
using System.Web.Routing;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;

namespace Geta.SEO.Sitemaps
{
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    [InitializableModule]
    public class SitemapInitialization : IInitializableModule
    {
        private static bool _initialized;

        public void Initialize(InitializationEngine context)
        {
            if (_initialized || context.HostType != HostType.WebApplication)
            {
                return;
            }

            RouteTable.Routes.MapRoute("Sitemap index", "sitemapindex.xml", new { controller = "GetaSitemapIndex", action = "Index" });
            RouteTable.Routes.MapRoute("Sitemap without path", "sitemap.xml", new { controller = "GetaSitemap", action = "Index" });
            RouteTable.Routes.MapRoute("Sitemap with path", "{path}sitemap.xml", new { controller = "GetaSitemap", action = "Index" });
            RouteTable.Routes.MapRoute("Sitemap with language", "{language}/sitemap.xml", new { controller = "GetaSitemap", action = "Index" });
            RouteTable.Routes.MapRoute("Sitemap with language and path", "{language}/{path}sitemap.xml", new { controller = "GetaSitemap", action = "Index" });

            _initialized = true;
        }

        public void Uninitialize(InitializationEngine context)
        {
        }

        public void Preload(string[] parameters)
        {
        }
    }
}