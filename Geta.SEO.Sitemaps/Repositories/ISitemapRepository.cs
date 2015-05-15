using System.Collections.Generic;
using EPiServer.Data;
using Geta.SEO.Sitemaps.Entities;

namespace Geta.SEO.Sitemaps.Repositories
{
    public interface ISitemapRepository
    {
        void Delete(Identity id);

        IList<SitemapData> GetAllSitemapData();

        SitemapData GetSitemapData(Identity id);

        SitemapData GetSitemapData(string requestUrl);

        string GetSitemapUrl(SitemapData sitemapData);

        string GetHostWithLanguage(SitemapData sitemapData);

        void Save(SitemapData sitemapData);
    }
}