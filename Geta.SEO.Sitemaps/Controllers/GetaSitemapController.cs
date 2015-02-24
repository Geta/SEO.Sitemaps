using System.IO.Compression;
using System.Reflection;
using System.Web.Mvc;
using Geta.SEO.Sitemaps.Entities;
using Geta.SEO.Sitemaps.Repositories;
using log4net;

namespace Geta.SEO.Sitemaps.Controllers
{
    public class GetaSitemapController : Controller
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ISitemapRepository _sitemapRepository;

        public GetaSitemapController(ISitemapRepository sitemapRepository)
        {
            _sitemapRepository = sitemapRepository;
        }

        public ActionResult Index()
        {
            SitemapData sitemapData = _sitemapRepository.GetSitemapData(Request.Url.ToString());

            if (sitemapData == null || sitemapData.Data == null)
            {
                Log.Error("Xml sitemap data not found!");
                return new HttpNotFoundResult();
            }

            Response.Filter = new GZipStream(Response.Filter, CompressionMode.Compress);
            Response.AppendHeader("Content-Encoding", "gzip");

            return new FileContentResult(sitemapData.Data, "text/xml");
        }
    }
}