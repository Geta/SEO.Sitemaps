using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Data;
using EPiServer.Data.Dynamic;
using Geta.SEO.Sitemaps.Entities;

namespace Geta.SEO.Sitemaps.Services
{
    public class SitemapRepository : ISitemapRepository
    {
        private static DynamicDataStore SitemapStore
        {
            get
            {
                return typeof(SitemapData).GetStore();
            }
        }

        public void Delete(Identity id)
        {
            SitemapStore.Delete(id);
        }

        public SitemapData GetSitemapData(Identity id)
        {
            return SitemapStore.Items<SitemapData>().FirstOrDefault(sitemap => sitemap.Id == id);
        }

        public SitemapData GetSitemapData(string requestUrl)
        {
            var url = new Url(requestUrl); 
            
            var host = url.Path.TrimStart('/').ToLower();

            return SitemapStore.Items<SitemapData>().FirstOrDefault(x => x.Host.ToLower() == host && (x.SiteUrl == null || x.SiteUrl.Contains(url.Host)));
        }

        public IList<SitemapData> GetAllSitemapData()
        {
            return SitemapStore.Items<SitemapData>().ToList();
        }

        public void Save(SitemapData sitemapData)
        {
            if (sitemapData == null)
            {
                return;
            }

            SitemapStore.Save(sitemapData);
        }
    }
}