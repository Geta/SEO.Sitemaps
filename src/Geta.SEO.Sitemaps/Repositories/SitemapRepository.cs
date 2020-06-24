// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Data;
using EPiServer.DataAbstraction;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using Geta.SEO.Sitemaps.Entities;

namespace Geta.SEO.Sitemaps.Repositories
{
    [ServiceConfiguration(typeof(ISitemapRepository))]
    public class SitemapRepository : ISitemapRepository
    {
        private readonly ILanguageBranchRepository _languageBranchRepository;
        private readonly ISiteDefinitionResolver _siteDefinitionResolver;
        private readonly ISitemapLoader _sitemapLoader;


        public SitemapRepository(ILanguageBranchRepository languageBranchRepository, ISiteDefinitionResolver siteDefinitionResolver, ISitemapLoader sitemapLoader)
        {
            if (languageBranchRepository == null) throw new ArgumentNullException(nameof(languageBranchRepository));
            if (siteDefinitionResolver == null) throw new ArgumentNullException(nameof(siteDefinitionResolver));
            if (sitemapLoader == null) throw new ArgumentNullException(nameof(sitemapLoader));

            _languageBranchRepository = languageBranchRepository;
            _siteDefinitionResolver = siteDefinitionResolver;
            _sitemapLoader = sitemapLoader;
        }

        public void Delete(Identity id)
        {
            _sitemapLoader.Delete(id);
        }

        public SitemapData GetSitemapData(Identity id)
        {
            return _sitemapLoader.GetSitemapData(id);
        }

        public SitemapData GetSitemapData(string requestUrl)
        {
            var url = new Url(requestUrl); 
            
            // contains the sitemap URL, for example en/sitemap.xml
            var host = url.Path.TrimStart('/').ToLowerInvariant();

            var siteDefinition = _siteDefinitionResolver.GetByHostname(url.Host, true, out _);
            if (siteDefinition == null)
            {
                return null;
            }

            var sitemapData = GetAllSitemapData()?.Where(x =>
                GetHostWithLanguage(x) == host &&
                (x.SiteUrl == null || siteDefinition.Hosts.Any(h => h.Name == new Url(x.SiteUrl).Authority))).ToList();

            if (sitemapData?.Count == 1)
            {
                return sitemapData.FirstOrDefault();
            }

            // Could happen that we found multiple sitemaps when for each host in the SiteDefinition a Sitemap is created.
            // In that case, use the requestURL to get the correct SiteMapData
            return sitemapData?.FirstOrDefault(x => new Url(x.SiteUrl).Authority == url.Authority);
        }

        public string GetSitemapUrl(SitemapData sitemapData)
        {
            return string.Format("{0}{1}", sitemapData.SiteUrl, GetHostWithLanguage(sitemapData));
        }

        /// <summary>
        /// Returns host with language.
        /// For example en/sitemap.xml
        /// </summary>
        /// <param name="sitemapData"></param>
        /// <returns></returns>
        public string GetHostWithLanguage(SitemapData sitemapData)
        {
            if (string.IsNullOrWhiteSpace(sitemapData.Language))
            {
                return sitemapData.Host.ToLowerInvariant();
            }

            var languageBranch = _languageBranchRepository.Load(sitemapData.Language);

            if (languageBranch != null)
            {
                return string.Format("{0}/{1}", languageBranch.CurrentUrlSegment, sitemapData.Host).ToLowerInvariant();
            }
            return sitemapData.Host.ToLowerInvariant();
        }

        public IList<SitemapData> GetAllSitemapData()
        {
            return _sitemapLoader.GetAllSitemapData();
        }

        public void Save(SitemapData sitemapData)
        {
            _sitemapLoader.Save(sitemapData);
        }
    }
}