// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

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