// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;

namespace Geta.SEO.Sitemaps.Utils
{
    public static class UriComparer
    {
        public static bool SchemeAndServerEquals(Uri first, Uri second)
        {
            return Uri.Compare(
                       first,
                       second,
                       UriComponents.SchemeAndServer,
                       UriFormat.SafeUnescaped,
                       StringComparison.OrdinalIgnoreCase) == 0;
        }
    }
}