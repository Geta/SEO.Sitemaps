using System.IO.Compression;
using System.Reflection;
using System.Web;
using System.Web.UI;
using Geta.SEO.Sitemaps.Services;
using log4net;

namespace Geta.SEO.Sitemaps.Modules.Geta.SEO.Sitemaps
{
    public partial class SitemapHandler : Page
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ISitemapRepository sitemapRepository = new SitemapRepository();


        protected override void OnLoad(System.EventArgs e)
        {
            base.OnLoad(e);

            var sitemapData = sitemapRepository.GetSitemapData(GetRouteUrl(RouteData.Values));

            if (sitemapData == null || sitemapData.Data == null)
            {
                Log.Error("Xml sitemap data not found!");
                return;
            }

            Response.Filter = new GZipStream(Response.Filter, CompressionMode.Compress);
            Response.AppendHeader("Content-Encoding", "gzip");
            Response.ContentType = "text/xml";
            Response.BinaryWrite(sitemapData.Data);

            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
       
    }
}