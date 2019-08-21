// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

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