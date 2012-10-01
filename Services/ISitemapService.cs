using System.Collections.Generic;
using Geta.SEO.Sitemaps.Entities;

namespace Geta.SEO.Sitemaps.Services
{
    internal interface ISitemapService
    {
        bool Generate(SitemapData sitemapData, out int entryCount);

        IList<SitemapData> GetAllSitemapData();

        void Save(SitemapData sitemapData);
    }
}