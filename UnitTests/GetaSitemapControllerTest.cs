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


        [Fact]
        public void ReturnsHttpNotFoundResultWhenMissingSitemap()
        {
            // Arrange
            var controller = createController(repo, factory);

            // Act
            var actionResult = controller.Index();

            // Assert
            Assert.IsType<HttpNotFoundResult>(actionResult);
        }

        [Fact]
        public void ReturnsSitemapWhenRepoIsNonEmpty()
        {
            // Arrange
            var controller = createController(repo, factory);
            addDummySitemapData(repo);

            // Act
            var actionResult = controller.Index();

            // Assert
            Assert.IsType<FileContentResult>(actionResult);
        }


        [Fact]
        public void ChecksAcceptHeaderBeforeSettingGzipEncoding()
        {
            // Arrange
            var controller = createController(repo, factory);
            addDummySitemapData(repo);

            // Act
            ActionResult result = controller.Index();

            // Assert
            var encoding = controller.Response.Headers.Get("Content-Encoding");
            Assert.NotEqual("gzip", encoding);
        }

        [Fact]
        public void AddsGzipEncodingWhenAccepted()
        {

            // Arrange
            var httpRequestBase = createRequestBase();
            httpRequestBase.Headers.Add("Accept-Encoding", "gzip, deflate, br");
            var requestContext = createRequestContext(httpRequestBase, createResponseBase());

            var controller = createController(repo, factory);
            addDummySitemapData(repo);

            // Act
            ActionResult result = controller.Index();

            // Assert
            var encoding = controller.Response.Headers.Get("Content-Encoding");
            Assert.Equal("gzip", encoding);
        }

        private static ControllerContext createControllerContext()
        {
            var requestContext = createRequestContext(createRequestBase(), createResponseBase());

            return createControllerContext(requestContext);
        }

        private static ControllerContext createControllerContext(RequestContext requestContext)
        {
            var context = new ControllerContext();
            context.RequestContext = requestContext;

            return context;
        }

        private static RequestContext createRequestContext(HttpRequestBase requestBase, HttpResponseBase responseBase)
        {
            var httpContext = createHttpContext(requestBase, responseBase);

            var requestContext = new RequestContext();
            requestContext.HttpContext = httpContext;
            return requestContext;
        }

        private static HttpContextBase createHttpContext(HttpRequestBase requestBase, HttpResponseBase responseBase)
        {
            var httpContext = Substitute.For<HttpContextBase>();
            httpContext.Request.Returns(requestBase);
            httpContext.Response.Returns(responseBase);
            return httpContext;
        }

        private static HttpResponseBase createResponseBase()
        {
            var responseBase = Substitute.For<HttpResponseBase>();
            var collection = new System.Collections.Specialized.NameValueCollection();
            responseBase.Headers.Returns(collection);
            responseBase.When(x => x.AppendHeader(Arg.Any<string>(), Arg.Any<string>()))
                .Do(args => collection.Add((string) args[0], (string) args[1]));

            return responseBase;
        }

        private static HttpRequestBase createRequestBase()
        {
            Uri dummyUri = new Uri("http://foo.bar");
            var requestBase = Substitute.For<HttpRequestBase>();
            requestBase.Url.Returns(dummyUri);
            requestBase.Headers.Returns(new System.Collections.Specialized.NameValueCollection());

            return requestBase;
        }

        private static void addDummySitemapData(ISitemapRepository repo2)
        {
            var sitemapData = new SitemapData();
            sitemapData.Data = new byte[] { 0, 1, 2, 3, 4 };
            repo2.GetSitemapData(Arg.Any<string>()).Returns(sitemapData);
        }

        public static GetaSitemapController createController(ISitemapRepository repo, SitemapXmlGeneratorFactory factory)
        {
            return createController(repo, factory, createControllerContext());
        }

        private static GetaSitemapController createController(ISitemapRepository repo, SitemapXmlGeneratorFactory factory,
            ControllerContext controllerContext)
        {
            var controller = new GetaSitemapController(repo, factory);
            controller.ControllerContext = controllerContext;
            controller.Response.Filter = new MemoryStream();

            return controller;
        }
    }
}