using System.Collections.Generic;
using System.Globalization;
using EPiServer.DataAbstraction;
using EPiServer.Web;
using Geta.SEO.Sitemaps.Entities;
using Geta.SEO.Sitemaps.Repositories;
using Moq;
using Xunit;

namespace Tests
{
    public class SitemapRepositoryTests
    {
        private readonly Mock<ILanguageBranchRepository> _languageBranchRepository;

        public SitemapRepositoryTests()
        {
            _languageBranchRepository = new Mock<ILanguageBranchRepository>();
            _languageBranchRepository
                .Setup(x => x.Load(It.IsAny<CultureInfo>()))
                .Returns(new LanguageBranch(new CultureInfo("en")));
        }

        [Fact]
        public void Can_Retrieve_SiteMapData_By_URL()
        {
            var requestUrl = "https://www.domain.com/en/sitemap.xml";
            var expectedSitemapData = new SitemapData
                { Language = "en", Host = "Sitemap.xml", SiteUrl = null };
            
            var hostDefinition = new HostDefinition();
            var siteDefinition = new SiteDefinition();
            siteDefinition.Hosts = new List<HostDefinition>
            {
                new HostDefinition {Name = "www.domain.com"}
            };

            var siteDefinitionResolver = new Mock<ISiteDefinitionResolver>();
            siteDefinitionResolver
                .Setup(x => x.GetByHostname(It.IsAny<string>(), It.IsAny<bool>(), out hostDefinition))
                .Returns(siteDefinition);

            var sitemapDataList = new List<SitemapData>
            {
                expectedSitemapData
            };

            var sitemapLoader = new Mock<ISitemapLoader>();
            sitemapLoader
                .Setup(x => x.GetAllSitemapData())
                .Returns(sitemapDataList);

            var siteMapService = new SitemapRepository(_languageBranchRepository.Object, siteDefinitionResolver.Object, sitemapLoader.Object);

            var siteMapData = siteMapService.GetSitemapData(requestUrl);

            Assert.True(siteMapData.Equals(expectedSitemapData));
        }

        [Fact]
        public void Can_Retrieve_SiteMapData_By_URL_When_SiteMapData_SiteUrl_Is_Null()
        {
            var requestUrl = "https://www.domain.com/en/sitemap.xml";
            var expectedSitemapData = new SitemapData
                { Language = "en", Host = "Sitemap.xml", SiteUrl = null };

            var hostDefinition = new HostDefinition();
            var siteDefinition = new SiteDefinition();
            siteDefinition.Hosts = new List<HostDefinition>
            {
                new HostDefinition {Name = "www.domain.com"}
            };

            var siteDefinitionResolver = new Mock<ISiteDefinitionResolver>();
            siteDefinitionResolver
                .Setup(x => x.GetByHostname(It.IsAny<string>(), It.IsAny<bool>(), out hostDefinition))
                .Returns(siteDefinition);

            var sitemapDataList = new List<SitemapData>
            {
                expectedSitemapData
            };

            var sitemapLoader = new Mock<ISitemapLoader>();
            sitemapLoader
                .Setup(x => x.GetAllSitemapData())
                .Returns(sitemapDataList);

            var siteMapService = new SitemapRepository(_languageBranchRepository.Object, siteDefinitionResolver.Object, sitemapLoader.Object);
            
            var siteMapData = siteMapService.GetSitemapData(requestUrl);

            Assert.True(siteMapData.Equals(expectedSitemapData));
        }

        [Fact]
        public void One_Host_And_Multiple_Sitemaps_Can_Retrieve_Correct_SiteMap()
        {
            var requestUrl = "https://xyz.com/en/sitemap.xml";
            var expectedSitemapData = new SitemapData
                {Language = "en", Host = "Sitemap.xml", SiteUrl = "https://xyz.com/"};

            var hostDefinition = new HostDefinition();
            var siteDefinition = new SiteDefinition();
            siteDefinition.Hosts = new List<HostDefinition>
            {
                new HostDefinition {Name = "xyz.com"}
            };

            var siteDefinitionResolver = new Mock<ISiteDefinitionResolver>();
            siteDefinitionResolver
                .Setup(x => x.GetByHostname(It.IsAny<string>(), It.IsAny<bool>(), out hostDefinition))
                .Returns(siteDefinition);

            var sitemapDataList = new List<SitemapData>
            {
                new SitemapData { Language = "en", Host = "Sitemap.xml", SiteUrl = "https://abc.xyz.com/" },
                expectedSitemapData
            };

            var sitemapLoader = new Mock<ISitemapLoader>();
            sitemapLoader
                .Setup(x => x.GetAllSitemapData())
                .Returns(sitemapDataList);

            var siteMapService = new SitemapRepository(_languageBranchRepository.Object, siteDefinitionResolver.Object, sitemapLoader.Object);
            
            var siteMapData = siteMapService.GetSitemapData(requestUrl);

            Assert.True(siteMapData.Equals(expectedSitemapData));
        }

        [Fact]
        public void Multiple_Host_And_Multiple_Sitemaps_Can_Retrieve_Correct_SiteMap()
        {
            var requestUrl = "https://abc.xyz.com/en/sitemap.xml";
            var expectedSitemapData = new SitemapData
                { Language = "en", Host = "Sitemap.xml", SiteUrl = "https://xyz.com/" };

            var hostDefinition = new HostDefinition();
            var siteDefinition = new SiteDefinition();
            siteDefinition.Hosts = new List<HostDefinition>
            {
                new HostDefinition {Name = "xyz.com"},
                new HostDefinition {Name = "abc.xyz.com"}
            };

            var siteDefinitionResolver = new Mock<ISiteDefinitionResolver>();
            siteDefinitionResolver
                .Setup(x => x.GetByHostname(It.IsAny<string>(), It.IsAny<bool>(), out hostDefinition))
                .Returns(siteDefinition);
            
            var sitemapDataList = new List<SitemapData>
            {
                expectedSitemapData
            };

            var sitemapLoader = new Mock<ISitemapLoader>();
            sitemapLoader
                .Setup(x => x.GetAllSitemapData())
                .Returns(sitemapDataList);

            var siteMapService = new SitemapRepository(_languageBranchRepository.Object, siteDefinitionResolver.Object, sitemapLoader.Object);

            var siteMapData = siteMapService.GetSitemapData(requestUrl);

            Assert.True(siteMapData.Equals(expectedSitemapData));
        }

        [Fact]
        public void Expect_Sitemap_With_Language_In_URL()
        {
            var sitemapData = new SitemapData
                { Language = "en", Host = "Sitemap.xml", SiteUrl = "https://xyz.com/" };

            var expectedSiteMapUrl = "en/sitemap.xml";

            var siteDefinitionResolver = new Mock<ISiteDefinitionResolver>();
            var sitemapLoader = new Mock<ISitemapLoader>();

            var siteMapService = new SitemapRepository(_languageBranchRepository.Object, siteDefinitionResolver.Object, sitemapLoader.Object);

            var sitemapUrl = siteMapService.GetHostWithLanguage(sitemapData);

            Assert.True(sitemapUrl != null && sitemapUrl.Equals(expectedSiteMapUrl));
        }

        [Fact]
        public void Expect_Sitemap_Without_Language_In_URL()
        {
            var sitemapData = new SitemapData
                { Host = "Sitemap.xml", SiteUrl = "https://xyz.com/" };

            var expectedSiteMapUrl = "sitemap.xml";
            
            var siteDefinitionResolver = new Mock<ISiteDefinitionResolver>();
            var sitemapLoader = new Mock<ISitemapLoader>();

            var siteMapService = new SitemapRepository(_languageBranchRepository.Object, siteDefinitionResolver.Object, sitemapLoader.Object);

            var sitemapUrl = siteMapService.GetHostWithLanguage(sitemapData);

            Assert.True(sitemapUrl != null && sitemapUrl.Equals(expectedSiteMapUrl));
        }
    }
}
