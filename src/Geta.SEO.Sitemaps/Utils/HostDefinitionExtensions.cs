using System;
using EPiServer.Web;

namespace Geta.SEO.Sitemaps.Utils
{
    public static class HostDefinitionExtensions
    {
        public static Uri GetUri(this HostDefinition host)
        {
            var scheme = "http";
            if (host.UseSecureConnection != null && host.UseSecureConnection == true)
            {
                scheme = "https";
            }

            var hostUrl = $"{scheme}://{host.Name}/";
            return new Uri(hostUrl);
        }
    }
}