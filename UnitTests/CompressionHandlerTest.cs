
using Geta.SEO.Sitemaps.Compression;
using NSubstitute;
using System.Collections.Specialized;
using System.IO;
using System.Web;
using Xunit;

namespace Tests
{
    public class CompressionHandlerTest
    {
        [Fact]
        public void DoesNotChangeFilterIfNoSuitableEncodingWasFound()
        {
            // Arrange
            var res = createResponseBase();
            var emptyHeaders = new NameValueCollection();
            var beforeFilter = res.Filter;

            // Act
            CompressionHandler.ChooseSuitableCompression(emptyHeaders, res);

            // Assert
            var afterFilter = res.Filter;
            Assert.Equal(beforeFilter, afterFilter);
        }

        [Fact]
        public void DoesNotChangeContentEncodingIfNoSuitableEncodingWasFound()
        {
            var res = createResponseBase();
            var emptyHeaders = new NameValueCollection();
            CompressionHandler.ChooseSuitableCompression(emptyHeaders, res);

            Assert.True(res.Headers.Get("Content-Encoding") == null);
        }

        [Fact]
        public void ChangesContentEncodingIfSuitableEncodingWasFound()
        {
            var res = createResponseBase();
            var headers = new NameValueCollection();
            headers.Add(CompressionHandler.ACCEPT_ENCODING_HEADER, "gzip");
            CompressionHandler.ChooseSuitableCompression(headers, res);

            var encoding = res.Headers.Get(CompressionHandler.CONTENT_ENCODING_HEADER);
            Assert.True(encoding != null);
            Assert.Equal("gzip",encoding );
        }

        [Fact]
        public void ChoosesMostSuitableEncoding()
        {
            var res = createResponseBase();
            var headers = new NameValueCollection();
            headers.Add(CompressionHandler.ACCEPT_ENCODING_HEADER, "gzip;q=0.3,deflate;q=0.8,foobar;q=0.9");
            CompressionHandler.ChooseSuitableCompression(headers, res);

            var encoding = res.Headers.Get(CompressionHandler.CONTENT_ENCODING_HEADER);
            Assert.Equal("deflate",encoding );
        }


        private static HttpResponseBase createResponseBase()
        {
            var responseBase = Substitute.For<HttpResponseBase>();
            var collection = new NameValueCollection();
            responseBase.Headers.Returns(collection);
            responseBase.When(x => x.AppendHeader(Arg.Any<string>(), Arg.Any<string>()))
                .Do(args => collection.Add((string) args[0], (string) args[1]));

            responseBase.Filter = new MemoryStream();

            return responseBase;
        }
    }
}
