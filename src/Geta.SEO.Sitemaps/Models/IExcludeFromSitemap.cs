using EPiServer.Core;

namespace Geta.SEO.Sitemaps.Models
{
    /// <summary>
    /// Apply this interface to pagetypes you do not want to include in the index
    /// </summary>
    public interface IExcludeFromSitemap : IContent
    {
    }
}
