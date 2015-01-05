using System.Web.Mvc;
using System.Web.Routing;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using Geta.SEO.Sitemaps.Repositories;
using StructureMap;
using StructureMap.Configuration.DSL;

namespace Geta.SEO.Sitemaps
{
    [ModuleDependency(typeof(ServiceContainerInitialization))]
    [InitializableModule]
    public class SitemapInitialization : IConfigurableModule
    {
        private static bool _initialized;

        public void ConfigureContainer(ServiceConfigurationContext context)
        {
            context.Container.Configure(ConfigureContainer);
        }

        private static void ConfigureContainer(ConfigurationExpression container)
        {
            container.AddRegistry<SitemapRegistry>();
        }

        public void Initialize(InitializationEngine context)
        {
            if (_initialized || context.HostType != HostType.WebApplication)
            {
                return;
            }

            RouteTable.Routes.MapRoute("Sitemap without path", "sitemap.xml", new { controller = "GetaSitemap", action = "Index" });
            RouteTable.Routes.MapRoute("Sitemap with path", "{path}sitemap.xml", new { controller = "GetaSitemap", action = "Index" });

            _initialized = true;
        }

        public void Uninitialize(InitializationEngine context)
        {
        }

        public void Preload(string[] parameters)
        {
        }
    }

    public class SitemapRegistry : Registry
    {
        public SitemapRegistry()
        {
            For<ISitemapRepository>().Use<SitemapRepository>();
        }
    }
}