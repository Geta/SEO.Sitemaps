// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Reflection;
using System.Web.Caching;
using System.Web.Mvc;
using EPiServer.Core;
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
        private readonly IContentCacheKeyCreator _contentCacheKeyCreator;
        private readonly ISynchronizedObjectInstanceCache _synchronizedObjectInstanceCache;

        // This constructor was added to support web forms projects without dependency injection configured.
        public GetaSitemapController() : this(
            ServiceLocator.Current.GetInstance<ISitemapRepository>(),
            ServiceLocator.Current.GetInstance<SitemapXmlGeneratorFactory>(),
            ServiceLocator.Current.GetInstance<IContentCacheKeyCreator>(),
            ServiceLocator.Current.GetInstance<ISynchronizedObjectInstanceCache>())
        {
        }

        public GetaSitemapController(
            ISitemapRepository sitemapRepository,
            SitemapXmlGeneratorFactory sitemapXmlGeneratorFactory,
            IContentCacheKeyCreator contentCacheKeyCreator,
            ISynchronizedObjectInstanceCache synchronizedObjectInstanceCache)
        {
            _sitemapRepository = sitemapRepository;
            _sitemapXmlGeneratorFactory = sitemapXmlGeneratorFactory;
            _contentCacheKeyCreator = contentCacheKeyCreator;
            _synchronizedObjectInstanceCache = synchronizedObjectInstanceCache;
        }

        public ActionResult Index()
        {
            var sitemapData = _sitemapRepository.GetSitemapData(Request.Url.ToString());

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
            var userAgent = Request.ServerVariables["USER_AGENT"];

            var isGoogleBot = userAgent != null &&
                              userAgent.IndexOf("Googlebot", StringComparison.InvariantCultureIgnoreCase) > -1;

            var googleBotCacheKey = isGoogleBot ? "Google-" : string.Empty;

            if (SitemapSettings.Instance.EnableRealtimeSitemap)
            {
                var cacheKey = googleBotCacheKey + _sitemapRepository.GetSitemapUrl(sitemapData);

                if (_synchronizedObjectInstanceCache.Get(cacheKey) is byte[] sitemapDataData)
                {
                    sitemapData.Data = sitemapDataData;
                    return true;
                }

                if (!_sitemapXmlGeneratorFactory
                    .GetSitemapXmlGenerator(sitemapData)
                    .Generate(sitemapData, false, out _))
                {
                    return false;
                }

                if (!SitemapSettings.Instance.EnableRealtimeCaching)
                {
                    return true;
                }

                var cachePolicy = isGoogleBot
                    ? new CacheEvictionPolicy(Cache.NoSlidingExpiration, CacheTimeoutType.Sliding, null,
                        new[] {_contentCacheKeyCreator.VersionKey})
                    : null;

                _synchronizedObjectInstanceCache.Insert(cacheKey, sitemapData.Data, cachePolicy);
                return true;
            }

            return _sitemapXmlGeneratorFactory
                .GetSitemapXmlGenerator(sitemapData)
                .Generate(sitemapData, !SitemapSettings.Instance.EnableRealtimeSitemap, out _);
        }
    }
}