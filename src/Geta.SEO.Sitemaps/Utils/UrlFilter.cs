// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Linq;
using Geta.SEO.Sitemaps.Entities;

namespace Geta.SEO.Sitemaps.Utils
{
    /// <summary>
    /// Administrators are able to specify specific paths to exclude (blacklist) or include (whitelist) in sitemaps.
    /// This class is used to check this.
    /// </summary>
    public class UrlFilter
    {
        public static bool IsUrlFiltered(string url, SitemapData sitemapConfig)
        {
            IList<string> whiteList = sitemapConfig.PathsToInclude;
            IList<string> blackList = sitemapConfig.PathsToAvoid;

            if (IsNotInWhiteList(url, whiteList) || IsInBlackList(url, blackList))
            {
                return true;
            }

            return false;
        }

        private static bool IsNotInWhiteList(string url, IList<string> paths)
        {
            return IsPathInUrl(url, paths, true);
        }

        private static bool IsInBlackList(string url, IList<string> paths)
        {
            return IsPathInUrl(url, paths, false);
        }

        private static bool IsPathInUrl(string url, IList<string> paths, bool mustContainPath)
        {
            if (paths != null && paths.Count > 0)
            {
                var anyPathIsInUrl = paths.Any(x =>
                {
                    var dir = AddStartSlash(AddTailingSlash(x.ToLower().Trim()));
                    return url.ToLower().StartsWith(dir);
                });

                if (anyPathIsInUrl != mustContainPath)
                {
                    return true;
                }
            }

            return false;
        }

        private static string AddTailingSlash(string url)
        {
            if (url[url.Length - 1] != '/')
            {
                url = url + "/";
            }

            return url;
        }

        private static string AddStartSlash(string url)
        {
            if (!url.StartsWith("/"))
            {
                url = "/" + url;
            }

            return url;
        }
    }
}
