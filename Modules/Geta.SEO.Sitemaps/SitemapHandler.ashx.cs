using System.Reflection;
using System.Web;
using System.IO.Compression;
using System.Web.SessionState;
using Geta.SEO.Sitemaps.Entities;
using log4net;

namespace Geta.SEO.Sitemaps.Modules.Geta.SEO.Sitemaps
{
    public class SitemapHandler : IHttpHandler, IReadOnlySessionState 
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public bool IsReusable
        {
            get
            {
                return true;
            }
        }

        public void ProcessRequest(HttpContext context)
        {
            var sitemapData = HttpContext.Current.Items[SitemapUrlRoutingInit.SitemapSessionKey] as SitemapData;

            if (sitemapData == null || sitemapData.Data == null)
            {
                Log.Error("Xml sitemap data not found!");
                return;
            }

            context.Response.Filter = new GZipStream(context.Response.Filter, CompressionMode.Compress);
            context.Response.AppendHeader("Content-Encoding", "gzip");
            context.Response.ContentType = "text/xml";
            context.Response.BinaryWrite(sitemapData.Data);

            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }

        void IHttpHandler.ProcessRequest(HttpContext context)
        {
            ProcessRequest(context);
        }
    }
}