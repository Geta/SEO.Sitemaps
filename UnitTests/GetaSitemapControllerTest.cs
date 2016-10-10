using Geta.SEO.Sitemaps.Controllers;
using Geta.SEO.Sitemaps.Entities;
using Geta.SEO.Sitemaps.Repositories;
using Geta.SEO.Sitemaps.Utils;
using Geta.SEO.Sitemaps.XML;
using NSubstitute;
using System;
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Xunit;

namespace Tests
{
    public class GetaSitemapControllerTest
    {
        ISitemapRepository repo = Substitute.For<ISitemapRepository>();
        SitemapXmlGeneratorFactory factory = Substitute.For<SitemapXmlGeneratorFactory>();
        ISitemapXmlGenerator sitemapXmlGenerator = Substitute.For<ISitemapXmlGenerator>();

        public GetaSitemapController createController(ISitemapRepository repo, SitemapXmlGeneratorFactory factory)
        {
            var controller = new GetaSitemapController(repo,factory);
            controller.ControllerContext = createControllerContext();

            return controller;
        }

        [Fact]
        public void ReturnsHttpNotFoundResultWhenMissingSitemap()
        {
            var controller = createController(repo, factory);

            Assert.IsType<HttpNotFoundResult>(controller.Index());
        }

        [Fact]
        public void ReturnsSitemapWhenRepoIsNonEmpty()
        {
            var controller = createController(repo, factory);
            controller.Response.Filter = new MemoryStream();

            var sitemapData = new SitemapData();
            sitemapData.Data = new byte[] {0, 1, 2, 3, 4};
            
            repo.GetSitemapData(Arg.Any<string>()).Returns(sitemapData);

            Assert.IsType<FileContentResult>(controller.Index());
        }

        private static ControllerContext createControllerContext()
        {
            Uri dummyUri = new Uri("http://foo.bar");

            var context = new ControllerContext();

            var requestBase = Substitute.For<HttpRequestBase>();
            requestBase.Url.Returns(dummyUri);
            requestBase.ServerVariables.Returns(new System.Collections.Specialized.NameValueCollection());

            var responseBase = Substitute.For<HttpResponseBase>();
            responseBase.Headers.Returns(new System.Collections.Specialized.NameValueCollection());

            var httpContext = Substitute.For<HttpContextBase>();
            httpContext.Request.Returns(requestBase);
            httpContext.Response.Returns(responseBase);

            var requestContext = new RequestContext();
            requestContext.HttpContext = httpContext;

            context.RequestContext = requestContext;
            return context;
        }
    }
}