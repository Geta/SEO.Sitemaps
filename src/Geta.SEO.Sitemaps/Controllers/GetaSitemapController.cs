// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.IO.Compression;
using System.Reflection;
using System.Web.Caching;
using System.Web.Mvc;
using EPiServer;
using EPiServer.Framework.Cache;
using EPiServer.Logging.Compatibility;
using EPiServer.ServiceLocation;
using Geta.SEO.Sitemaps.Configuration;
using Geta.SEO.Sitemaps.Entities;
using Geta.SEO.Sitemaps.Repositories;
using Geta.SEO.Sitemaps.Utils;
using Geta.SEO.Sitemaps.Compression;

namespace Geta.SEO.Sitemaps.Controllers
{
    public class GetaSitemapController : Controller
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ISitemapRepository _sitemapRepository;
        private readonly SitemapXmlGeneratorFactory _sitemapXmlGeneratorFactory;

        // This constructor was added to support web forms projects without dependency injection configured.
        public GetaSitemapController() : this(ServiceLocator.Current.GetInstance<ISitemapRepository>(), ServiceLocator.Current.GetInstance<SitemapXmlGeneratorFactory>())
        {
        }

        public GetaSitemapController(ISitemapRepository sitemapRepository, SitemapXmlGeneratorFactory sitemapXmlGeneratorFactory)
        {
            _sitemapRepository = sitemapRepository;
            _sitemapXmlGeneratorFactory = sitemapXmlGeneratorFactory;
        }

        public ActionResult Index()
        {
            SitemapData sitemapData = _sitemapRepository.GetSitemapData(Request.Url.ToString());

            if (sitemapData == null)
            {
                Log.Error("Xml sitemap data not found!");
                return new HttpNotFoundResult();
            }

            if (sitemapData.Data == null || (SitemapSettings.Instance.EnableRealtimeSitemap))
            {
                if (!GetSitemapData(sitemapData))
                {
                    Log.Error("Xml sitemap data not found!");
                    return new HttpNotFoundResult();
                }
            }

            CompressionHandler.ChooseSuitableCompression(Request.Headers, Response);

            return new FileContentResult(sitemapData.Data, "text/xml; charset=utf-8");
        }

        private bool GetSitemapData(SitemapData sitemapData)
        {
            int entryCount;
            string userAgent = Request.ServerVariables["USER_AGENT"];

            var isGoogleBot = userAgent != null &&
                              userAgent.IndexOf("Googlebot", StringComparison.InvariantCultureIgnoreCase) > -1;

            string googleBotCacheKey = isGoogleBot ? "Google-" : string.Empty;

            if (SitemapSettings.Instance.EnableRealtimeSitemap)
            {
                string cacheKey = googleBotCacheKey + _sitemapRepository.GetSitemapUrl(sitemapData);

                var sitemapDataData = CacheManager.Get(cacheKey) as byte[];

                if (sitemapDataData != null)
                {
                    sitemapData.Data = sitemapDataData;
                    return true;
                }

                if (_sitemapXmlGeneratorFactory.GetSitemapXmlGenerator(sitemapData).Generate(sitemapData, false, out entryCount))
                {
                    if (SitemapSettings.Instance.EnableRealtimeCaching)
                    {
                        CacheEvictionPolicy cachePolicy;

                        if (isGoogleBot)
                        {
                            cachePolicy = new CacheEvictionPolicy(null, new[] {DataFactoryCache.VersionKey}, null, Cache.NoSlidingExpiration, CacheTimeoutType.Sliding);
                        }
                        else
                        {
                            cachePolicy = null;
                        }

                        CacheManager.Insert(cacheKey, sitemapData.Data, cachePolicy);
                    }

                    return true;
                }

                return false;
            }

            return _sitemapXmlGeneratorFactory.GetSitemapXmlGenerator(sitemapData).Generate(sitemapData, !SitemapSettings.Instance.EnableRealtimeSitemap, out entryCount);
        }
    }
}