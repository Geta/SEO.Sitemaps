namespace Tests
{
    using EPiServer.Core;
    using EPiServer.Framework.Web;
    using EPiServer.Security;
    using EPiServer.ServiceLocation;
    using EPiServer.Web;
    using Geta.SEO.Sitemaps.Entities;
    using Geta.SEO.Sitemaps.SpecializedProperties;
    using Geta.SEO.Sitemaps.Utils;
    using NSubstitute;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Principal;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    public class ContentFilterTest
    {
        [Fact]
        public void DontAddPagesWithContentTypeImplementingIExcludeInSitemap()
        {
            // Arrange
            var content = Substitute.For<IContent, IExcludeInSitemap>();
            var filter = new ContentFilter();
            // Act
            var result = filter.ShouldExcludeContent(content);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void AddPagesWithContentTypeDontImplementingIExcludeInSitemap()
        {
            // Arrange
            PageData content = CreatePageToBeIncludedInSitemap();

            var filter = new ContentFilter();

            // Act
            var result = filter.ShouldExcludeContent(content);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void DontAddPagesWithContentTypeImplementingIIncludeInSitemapAndNoIndex()
        {
            // Arrange
            PageData content = CreatePageWithNoIndexNotToBeIncludedInSitemap();

            var filter = new ContentFilter();

            // Act
            var result = filter.ShouldExcludeContent(content);

            // Assert
            Assert.True(result);
        }

        private PageData CreatePageWithNoIndexNotToBeIncludedInSitemap()
        {
            var content = Substitute.For<PageData, IIncludeInSitemap>();
            var visitorPrinciple = new GenericPrincipal(
                new GenericIdentity("visitor"),
                new[] { "Everyone" });

            content.GetSecurityDescriptor().HasAccess(visitorPrinciple, AccessLevel.Read).ReturnsForAnyArgs(true);

            ((IIncludeInSitemap)content).NoIndex = true;
            content.Status = VersionStatus.Published;
            var propCollection = new PropertyDataCollection
            {
                { PropertySEOSitemaps.PropertyName, new PropertySEOSitemaps() { Enabled = true } }
            };
            content.Property.Returns(propCollection);

            // Create a mock service locator
            var mockLocator = Substitute.For<IServiceLocator>();
            var mockTemplateResolver = Substitute.For<TemplateResolver>();
            mockTemplateResolver.HasTemplate(content, TemplateTypeCategories.Page).ReturnsForAnyArgs(true);

            // Setup the service locator to return our mock repository when an IContentRepository is requested
            mockLocator.GetInstance<TemplateResolver>().Returns(mockTemplateResolver);

            // Make use of our mock objects throughout EPiServer
            ServiceLocator.SetLocator(mockLocator);

            var startPageRef = new ContentReference(2);
            content.ContentLink.Returns(startPageRef);
            return content;
        }

        private static PageData CreatePageToBeIncludedInSitemap()
        {
            var content = Substitute.For<PageData>();
            var visitorPrinciple = new GenericPrincipal(
                new GenericIdentity("visitor"),
                new[] { "Everyone" });

            content.GetSecurityDescriptor().HasAccess(visitorPrinciple, AccessLevel.Read).ReturnsForAnyArgs(true);

            content.Status = VersionStatus.Published;
            var propCollection = new PropertyDataCollection
            {
                { PropertySEOSitemaps.PropertyName, new PropertySEOSitemaps() { Enabled = true } }
            };
            content.Property.Returns(propCollection);

            // Create a mock service locator
            var mockLocator = Substitute.For<IServiceLocator>();
            var mockTemplateResolver = Substitute.For<TemplateResolver>();
            mockTemplateResolver.HasTemplate(content, TemplateTypeCategories.Page).ReturnsForAnyArgs(true);

            // Setup the service locator to return our mock repository when an IContentRepository is requested
            mockLocator.GetInstance<TemplateResolver>().Returns(mockTemplateResolver);

            // Make use of our mock objects throughout EPiServer
            ServiceLocator.SetLocator(mockLocator);

            var startPageRef = new ContentReference(2);
            content.ContentLink.Returns(startPageRef);
            return content;
        }
    }
}
