using Geta.SEO.Sitemaps.Modules.Geta.SEO.Sitemaps;
using NUnit.Framework;
using Should;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geta.SEO.Sitemap.Modules.Geta.SEO.Sitemaps
{
    public class AdminManageSitemapTests
    {
        [Test]
        [TestCase("SiTeMaP.XML", "")]
        [TestCase("SITEMAP.XML", "")]
        [TestCase("sitemap.xml", "")]
        [TestCase("excellent_sitemap.xml", "excellent_")]
        [TestCase("AWESOME_SITEMAP.xml", "AWESOME_")]
        public void When_getting_postfix_for_a_host_then_case_is_ignored(string hostName, string expectedEditPart)
        {
            AdminManageSitemap.GetHostNameEditPart(hostName).ShouldEqual(expectedEditPart);
        }
    }
}
