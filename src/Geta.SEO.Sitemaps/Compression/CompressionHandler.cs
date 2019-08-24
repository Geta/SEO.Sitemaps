// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Specialized;
using System.IO.Compression;
using System.Web;
using System.Web.Mvc;

namespace Geta.SEO.Sitemaps.Compression
{
    public class CompressionHandler
    {
        public const string ACCEPT_ENCODING_HEADER = "Accept-Encoding";
        public const string CONTENT_ENCODING_HEADER = "Content-Encoding";

        public static void ChooseSuitableCompression(NameValueCollection requestHeaders, HttpResponseBase response)
        {
            if (requestHeaders == null) throw new ArgumentNullException(nameof(requestHeaders));
            if (response == null) throw new ArgumentNullException(nameof(response));


            /// load encodings from header
            QValueList encodings = new QValueList(requestHeaders[ACCEPT_ENCODING_HEADER]);

            /// get the types we can handle, can be accepted and
            /// in the defined client preference
            QValue preferred = encodings.FindPreferred("gzip", "deflate", "identity");

            /// if none of the preferred values were found, but the
            /// client can accept wildcard encodings, we'll default
            /// to Gzip.
            if (preferred.IsEmpty && encodings.AcceptWildcard && encodings.Find("gzip").IsEmpty)
                preferred = new QValue("gzip");

            // handle the preferred encoding
            switch (preferred.Name)
            {
                case "gzip":
                    response.AppendHeader(CONTENT_ENCODING_HEADER, "gzip");
                    response.Filter = new GZipStream(response.Filter, CompressionMode.Compress);
                    break;
                case "deflate":
                    response.AppendHeader(CONTENT_ENCODING_HEADER, "deflate");
                    response.Filter = new DeflateStream(response.Filter, CompressionMode.Compress);
                    break;
                case "identity":
                default:
                    break;
            }
        }
    }
}
