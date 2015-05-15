using System;
using System.IO.Compression;
using System.Reflection;
using System.Web.Caching;
using System.Web.Mvc;
using EPiServer;
using EPiServer.Framework.Cache;
using Geta.SEO.Sitemaps.Configuration;
using Geta.SEO.Sitemaps.Entities;
using Geta.SEO.Sitemaps.Repositories;
using Geta.SEO.Sitemaps.Utils;
using log4net;

namespace Geta.SEO.Sitemaps.Controllers
{
    public class GetaSitemapController : Controller
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ISitemapRepository _sitemapRepository;
        private readonly SitemapXmlGeneratorFactory _sitemapXmlGeneratorFactory;

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

            if (sitemapData.Data == null || SitemapSettings.Instance.EnableRealtimeSitemap)
            {
                if (!GetSitemapData(sitemapData))
                {
                    Log.Error("Xml sitemap data not found!");
                    return new HttpNotFoundResult();
                }
            }

            Response.Filter = new GZipStream(Response.Filter, CompressionMode.Compress);
            Response.AppendHeader("Content-Encoding", "gzip");

            return new FileContentResult(sitemapData.Data, "text/xml");
        }

        private bool GetSitemapData(SitemapData sitemapData)
        {
            int entryCount;
            if (SitemapSettings.Instance.EnableRealtimeSitemap)
            {
                string cacheKey = _sitemapRepository.GetSitemapUrl(sitemapData);

                var sitemapDataData = CacheManager.Get(cacheKey) as byte[];

                if (sitemapDataData != null)
                {
                    sitemapData.Data = sitemapDataData;
                    return true;
                }

                if (_sitemapXmlGeneratorFactory.GetSitemapXmlGenerator(sitemapData).Generate(sitemapData, false, out entryCount))
                {
                    CacheManager.Insert(cacheKey, sitemapData.Data,
                        new CacheEvictionPolicy(null, new[] {DataFactoryCache.VersionKey}, null,
                            Cache.NoSlidingExpiration, CacheTimeoutType.Sliding));

                    return true;
                }

                return false;
            }

            return _sitemapXmlGeneratorFactory.GetSitemapXmlGenerator(sitemapData).Generate(sitemapData, !SitemapSettings.Instance.EnableRealtimeSitemap, out entryCount);
        }
    }
}