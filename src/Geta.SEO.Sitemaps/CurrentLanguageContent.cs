// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Globalization;
using EPiServer.Core;

namespace Geta.SEO.Sitemaps
{
    public class CurrentLanguageContent
    {
        public IContent Content { get; set; }
        public CultureInfo CurrentLanguage { get; set; }
        public CultureInfo MasterLanguage { get; set; }
    }
}