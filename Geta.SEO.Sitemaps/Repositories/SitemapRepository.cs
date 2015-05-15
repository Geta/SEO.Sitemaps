using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Data;
using EPiServer.Data.Dynamic;
using EPiServer.DataAbstraction;
using EPiServer.ServiceLocation;
using Geta.SEO.Sitemaps.Entities;

namespace Geta.SEO.Sitemaps.Repositories
{
    [ServiceConfiguration(typeof(ISitemapRepository))]
    public class SitemapRepository : ISitemapRepository
    {
        private readonly ILanguageBranchRepository _languageBranchRepository;

        public SitemapRepository(ILanguageBranchRepository languageBranchRepository)
        {
            if (languageBranchRepository == null) throw new ArgumentNullException("languageBranchRepository");
            _languageBranchRepository = languageBranchRepository;
        }

        private static DynamicDataStore SitemapStore
        {
            get
            {
                return typeof(SitemapData).GetStore();
            }
        }

        public void Delete(Identity id)
        {
            SitemapStore.Delete(id);
        }

        public SitemapData GetSitemapData(Identity id)
        {
            return SitemapStore.Items<SitemapData>().FirstOrDefault(sitemap => sitemap.Id == id);
        }

        public SitemapData GetSitemapData(string requestUrl)
        {
            var url = new Url(requestUrl); 
            
            var host = url.Path.TrimStart('/').ToLowerInvariant();

            return GetAllSitemapData().FirstOrDefault(x => GetHostWithLanguage(x) == host && (x.SiteUrl == null || x.SiteUrl.Contains(url.Host)));
        }

        public string GetSitemapUrl(SitemapData sitemapData)
        {
            return string.Format("{0}{1}", sitemapData.SiteUrl, GetHostWithLanguage(sitemapData));
        }

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
            return SitemapStore.Items<SitemapData>().ToList();
        }

        public void Save(SitemapData sitemapData)
        {
            if (sitemapData == null)
            {
                return;
            }

            SitemapStore.Save(sitemapData);
        }
    }
}