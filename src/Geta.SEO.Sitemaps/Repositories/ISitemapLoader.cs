// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using EPiServer.Data;
using Geta.SEO.Sitemaps.Entities;

namespace Geta.SEO.Sitemaps.Repositories
{
    public interface ISitemapLoader
    {
        void Delete(Identity id);
        SitemapData GetSitemapData(Identity id);
        IList<SitemapData> GetAllSitemapData();
        void Save(SitemapData sitemapData);
    }
}