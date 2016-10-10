using Geta.SEO.Sitemaps.Controllers;
using Geta.SEO.Sitemaps.Repositories;
using Geta.SEO.Sitemaps.Utils;
using NSubstitute;
using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Xunit;

namespace Tests
{
    public class GetaSitemapControllerTest
    {

        [Fact]
        public void ReturnsHttpNotFoundResultWhenMissingSitemap()
        {
            var repo = Substitute.For<ISitemapRepository>();
            var factory = Substitute.For<SitemapXmlGeneratorFactory>();

        Uri dummyUri = new Uri("http://foo.bar");

            var controller = new GetaSitemapController(repo, factory);
            controller.ControllerContext = createControllerContext(dummyUri);

            Assert.IsType<HttpNotFoundResult>(controller.Index());
        }

        private static ControllerContext createControllerContext(Uri dummyUri)
        {
            var context = new ControllerContext();
            var requestBase = Substitute.For<HttpRequestBase>();
            requestBase.Url.Returns(dummyUri);

            var httpContext = Substitute.For<HttpContextBase>();
            httpContext.Request.Returns(requestBase);

            var requestContext = new RequestContext();
            requestContext.HttpContext = httpContext;

            context.RequestContext = requestContext;
            return context;
        }
    }
}