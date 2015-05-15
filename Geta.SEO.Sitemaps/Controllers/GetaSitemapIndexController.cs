using System.IO;
using System.IO.Compression;
using System.Text;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Linq;
using Geta.SEO.Sitemaps.Repositories;

namespace Geta.SEO.Sitemaps.Controllers
{
    public class GetaSitemapIndexController : Controller
    {
        private readonly ISitemapRepository _sitemapRepository;

        protected XNamespace SitemapXmlNamespace
        {
            get { return @"http://www.sitemaps.org/schemas/sitemap/0.9"; }
        }

        public GetaSitemapIndexController(ISitemapRepository sitemapRepository)
        {
            _sitemapRepository = sitemapRepository;
        }

        public ActionResult Index()
        {
            var doc = new XDocument(new XDeclaration("1.0", "utf-8", null));

            var indexElement = new XElement(SitemapXmlNamespace + "sitemapindex");

            foreach (var sitemapData in _sitemapRepository.GetAllSitemapData())
            {
                var sitemapElement = new XElement(
                    SitemapXmlNamespace + "sitemap",
                    new XElement(SitemapXmlNamespace + "loc", _sitemapRepository.GetSitemapUrl(sitemapData))
                );

                indexElement.Add(sitemapElement);
            }

            doc.Add(indexElement);

            Response.Filter = new GZipStream(Response.Filter, CompressionMode.Compress);
            Response.AppendHeader("Content-Encoding", "gzip");

            byte[] sitemapIndexData;

            using (var ms = new MemoryStream())
            {
                var xtw = new XmlTextWriter(ms, Encoding.UTF8);
                doc.Save(xtw);
                xtw.Flush();
                sitemapIndexData = ms.ToArray();
            }

            return new FileContentResult(sitemapIndexData, "text/xml");
        }
    }
}