using System;
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using EPiServer.Core;
using EPiServer.Framework.Cache;
using Geta.SEO.Sitemaps.Controllers;
using Geta.SEO.Sitemaps.Entities;
using Geta.SEO.Sitemaps.Repositories;
using Geta.SEO.Sitemaps.Utils;
using NSubstitute;
using Xunit;

namespace Tests
{
    public class GetaSitemapControllerTest
    {
        readonly ISitemapRepository _repo = Substitute.For<ISitemapRepository>();
        readonly SitemapXmlGeneratorFactory _factory = Substitute.For<SitemapXmlGeneratorFactory>();
        readonly IContentCacheKeyCreator _contentCacheKeyCreator = Substitute.For<IContentCacheKeyCreator>();

        private readonly ISynchronizedObjectInstanceCache _synchronizedObjectInstanceCache =
            Substitute.For<ISynchronizedObjectInstanceCache>();

        [Fact]
        public void ReturnsHttpNotFoundResultWhenMissingSitemap()
        {
            // Arrange
            var controller = CreateController(_repo, _factory, _contentCacheKeyCreator, _synchronizedObjectInstanceCache);

            // Act
            var actionResult = controller.Index();

            // Assert
            Assert.IsType<HttpNotFoundResult>(actionResult);
        }

        [Fact]
        public void ReturnsSitemapWhenRepoIsNonEmpty()
        {
            // Arrange
            var controller = CreateController(_repo, _factory, _contentCacheKeyCreator, _synchronizedObjectInstanceCache);
            AddDummySitemapData(_repo);

            // Act
            var actionResult = controller.Index();

            // Assert
            Assert.IsType<FileContentResult>(actionResult);
        }


        [Fact]
        public void ChecksAcceptHeaderBeforeSettingGzipEncoding()
        {
            // Arrange
            var controller = CreateController(_repo, _factory, _contentCacheKeyCreator, _synchronizedObjectInstanceCache);
            AddDummySitemapData(_repo);

            // Act
            controller.Index();

            // Assert
            var encoding = controller.Response.Headers.Get("Content-Encoding");
            Assert.NotEqual("gzip", encoding);
        }

        [Fact]
        public void AddsGzipEncodingWhenAccepted()
        {
            // Arrange
            var httpRequestBase = CreateRequestBase();
            httpRequestBase.Headers.Add("Accept-Encoding", "gzip, deflate, br");
            var requestContext = CreateRequestContext(httpRequestBase, CreateResponseBase());

            var controller = CreateController(_repo, _factory, _contentCacheKeyCreator, _synchronizedObjectInstanceCache, CreateControllerContext(requestContext));
            AddDummySitemapData(_repo);

            // Act
            controller.Index();

            // Assert
            var encoding = controller.Response.Headers.Get("Content-Encoding");
            Assert.Equal("gzip", encoding);
        }

        private static ControllerContext CreateControllerContext()
        {
            var requestContext = CreateRequestContext(CreateRequestBase(), CreateResponseBase());

            return CreateControllerContext(requestContext);
        }

        private static ControllerContext CreateControllerContext(RequestContext requestContext)
        {
            var context = new ControllerContext();
            context.RequestContext = requestContext;

            return context;
        }

        private static RequestContext CreateRequestContext(HttpRequestBase requestBase, HttpResponseBase responseBase)
        {
            var httpContext = CreateHttpContext(requestBase, responseBase);

            var requestContext = new RequestContext();
            requestContext.HttpContext = httpContext;
            return requestContext;
        }

        private static HttpContextBase CreateHttpContext(HttpRequestBase requestBase, HttpResponseBase responseBase)
        {
            var httpContext = Substitute.For<HttpContextBase>();
            httpContext.Request.Returns(requestBase);
            httpContext.Response.Returns(responseBase);
            return httpContext;
        }

        private static HttpResponseBase CreateResponseBase()
        {
            return CompressionHandlerTest.CreateResponseBase();
        }

        private static HttpRequestBase CreateRequestBase()
        {
            Uri dummyUri = new Uri("http://foo.bar");
            var requestBase = Substitute.For<HttpRequestBase>();
            requestBase.Url.Returns(dummyUri);
            requestBase.Headers.Returns(new System.Collections.Specialized.NameValueCollection());

            return requestBase;
        }

        private static void AddDummySitemapData(ISitemapRepository repo)
        {
            var sitemapData = new SitemapData();
            sitemapData.Data = new byte[] {0, 1, 2, 3, 4};
            repo.GetSitemapData(Arg.Any<string>()).Returns(sitemapData);
        }

        public static GetaSitemapController CreateController(
            ISitemapRepository repo,
            SitemapXmlGeneratorFactory factory,
            IContentCacheKeyCreator contentCacheKeyCreator,
            ISynchronizedObjectInstanceCache synchronizedObjectInstanceCache)
        {
            return CreateController(repo, factory, contentCacheKeyCreator, synchronizedObjectInstanceCache, CreateControllerContext());
        }

        private static GetaSitemapController CreateController(
            ISitemapRepository repo,
            SitemapXmlGeneratorFactory factory,
            IContentCacheKeyCreator contentCacheKeyCreator,
            ISynchronizedObjectInstanceCache synchronizedObjectInstanceCache,
            ControllerContext controllerContext)
        {
            var controller =
                new GetaSitemapController(repo, factory, contentCacheKeyCreator, synchronizedObjectInstanceCache);
            controller.ControllerContext = controllerContext;
            controller.Response.Filter = new MemoryStream();

            return controller;
        }
    }
}