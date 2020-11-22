using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EPiServer;
using EPiServer.DataAbstraction;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Geta.SEO.Sitemaps.Repositories;
using Geta.SEO.Sitemaps.Utils;
using Geta.SEO.Sitemaps.XML;
using Moq;
using Xunit;

namespace Tests
{
    public class SitemapXmlGeneratorTests
    {
        private readonly Mock<ILanguageBranchRepository> _languageBranchRepository;

        public SitemapXmlGeneratorTests()
        {
            _languageBranchRepository = new Mock<ILanguageBranchRepository>();
            _languageBranchRepository
                .Setup(x => x.Load(It.IsAny<CultureInfo>()))
                .Returns(new LanguageBranch(new CultureInfo("en")));
        }

        [Theory]
        [InlineData(new[] { "http://localhost", "http://localhost:5001" }, "http://localhost:5001")]
        [InlineData(new[] { "http://localhost", "http://localhost:5001" }, "http://localhost")]
        [InlineData(new[] { "https://xyz.com", "https://abc.xyz.com", "http://xyz.nl" }, "https://abc.xyz.com")]
        public void Can_Get_SiteDefinition_By_Site_Url(string[] hostUrls, string url)
        {
            var list = new List<SiteDefinition>()
            {
                new SiteDefinition
                {
                    Hosts = hostUrls.Select(x => new HostDefinition
                    {
                        Name = new Uri(x, UriKind.Absolute).Authority
                    }).ToList()
                }
            };

            var sitemapRepository = new Mock<ISitemapRepository>(); 
            var contentRepository = new Mock<IContentRepository>();
            var urlResolver = new Mock<IUrlResolver>();
            var siteDefinitionRepository = new Mock<ISiteDefinitionRepository>();
            siteDefinitionRepository.Setup(x => x.List())
                .Returns(list);

            var standardSitemapXmlGenerator = new StandardSitemapXmlGenerator(
                sitemapRepository.Object,
                contentRepository.Object,
                urlResolver.Object,
                siteDefinitionRepository.Object,
                _languageBranchRepository.Object,
                new ContentFilter());

            var siteDefinition = standardSitemapXmlGenerator.GetSiteDefinitionFromSiteUri(new Uri(url));

            Assert.NotNull(siteDefinition);
        }
    }
}
