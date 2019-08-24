// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using Geta.SEO.Sitemaps.Entities;

namespace Geta.SEO.Sitemaps.XML
{
    public interface ISitemapXmlGenerator
    {
        bool IsDebugMode { get; set; }
        bool Generate(SitemapData sitemapData, bool persistData, out int entryCount);
        void Stop();
    }
}
