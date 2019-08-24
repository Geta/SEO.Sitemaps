// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using EPiServer.ServiceLocation;
using Geta.SEO.Sitemaps.Entities;
using Geta.SEO.Sitemaps.XML;

namespace Geta.SEO.Sitemaps.Utils
{
    [ServiceConfiguration(typeof(SitemapXmlGeneratorFactory))]
    public class SitemapXmlGeneratorFactory
    {
        public virtual ISitemapXmlGenerator GetSitemapXmlGenerator(SitemapData sitemapData)
        {
            ISitemapXmlGenerator xmlGenerator;

            switch (sitemapData.SitemapFormat)
            {
                case SitemapFormat.Mobile:
                    xmlGenerator = ServiceLocator.Current.GetInstance<IMobileSitemapXmlGenerator>();
                    break;
                case SitemapFormat.Commerce:
                    xmlGenerator = ServiceLocator.Current.GetInstance<ICommerceSitemapXmlGenerator>();
                    break;
                case SitemapFormat.StandardAndCommerce:
                    xmlGenerator = ServiceLocator.Current.GetInstance<ICommerceAndStandardSitemapXmlGenerator>();
                    break;
                default:
                    xmlGenerator = ServiceLocator.Current.GetInstance<IStandardSitemapXmlGenerator>();
                    break;
            }

            xmlGenerator.IsDebugMode = sitemapData.IncludeDebugInfo;

            return xmlGenerator;
        } 
    }
}