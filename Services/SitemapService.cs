using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Geta.SEO.Sitemaps.Entities;
using Geta.SEO.Sitemaps.Utils;
using log4net;

namespace Geta.SEO.Sitemaps.Services
{
    internal class SitemapService : ISitemapService
    {
        private readonly ISitemapRepository sitemapRepository;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public SitemapService() : this(new SitemapRepository())
        {
        }

        public SitemapService(ISitemapRepository sitemapRepository)
        {
            this.sitemapRepository = sitemapRepository;
        }

        /// <summary>
        /// Generates a xml sitemap about pages on site
        /// </summary>
        /// <param name="sitemapData">SitemapData object containing configuration info for sitemap</param>
        /// <param name="entryCount">out count of site entries in generated sitemap</param>
        /// <returns>True if sitemap generation successful, false if error encountered</returns>
        public bool Generate(SitemapData sitemapData, out int entryCount)
        {
            try
            {
                XElement sitemap = SitemapContentHelper.CreateSitemapXmlContents(sitemapData, out entryCount);

                var doc = new XDocument(new XDeclaration("1.0", "utf-8", null));
                doc.Add(sitemap);

                using (var ms = new MemoryStream())
                {
                    var xtw = new XmlTextWriter(ms, Encoding.UTF8);
                    doc.Save(xtw);
                    xtw.Flush();
                    sitemapData.Data = ms.ToArray();
                }

                sitemapRepository.Save(sitemapData);
                return true;
            }
            catch (Exception e)
            {
                Log.Error("Error on generating xml sitemap" + Environment.NewLine + e);
                entryCount = 0;
                return false;
            }
        }

        public IList<SitemapData> GetAllSitemapData()
        {
            return sitemapRepository.GetAllSitemapData();
        }

        public void Save(SitemapData sitemapData)
        {
            sitemapRepository.Save(sitemapData);
        }
    }
}