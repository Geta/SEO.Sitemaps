// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using EPiServer.Data;
using EPiServer.Data.Dynamic;

namespace Geta.SEO.Sitemaps.Entities
{
    [EPiServerDataStore(AutomaticallyCreateStore = true, AutomaticallyRemapStore = true)]
    public class SitemapData
    {
        public byte[] Data { get; set; }

        [EPiServerDataIndex]
        public Identity Id { get; set; }

        public string SiteUrl { get; set; }

        [EPiServerDataIndex]
        public string Host { get; set; }

        public string Language { get; set; }

        public bool EnableLanguageFallback { get; set; }

        public bool IncludeAlternateLanguagePages { get; set; }
        public bool EnableSimpleAddressSupport { get; set; }
        
        public IList<string> PathsToInclude { get; set; }

        public IList<string> PathsToAvoid { get; set; }

        public bool IncludeDebugInfo { get; set; }

        public SitemapFormat SitemapFormat { get; set; }

        public int RootPageId { get; set; }

        public int EntryCount { get; set; }

        public bool ExceedsMaximumEntryCount { get; set; }
    }
}