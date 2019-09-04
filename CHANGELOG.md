# Changelog

All notable changes to this project will be documented in this file.

## [3.0.3]

- Removed Episerver.Packaging from NuGet package dependency

## [3.0.2]

- Use ZIP file for client resources

## [3.0.1]

- Thanks [@jarihaa](https://github.com/jarihaa) for the [PR #94](https://github.com/Geta/SEO.Sitemaps/pull/94)
- Fixes [#93](https://github.com/Geta/SEO.Sitemaps/issues/93)
- Fixes enabled checkbox being checked even if the value is false.

## [3.0.0]

- Adds support for Episerver Commerce 13. [Pull request #91](https://github.com/Geta/SEO.Sitemaps/pull/91)

## [2.0.13]

- New DOJO editor for sitemap property. [Pull request #87](https://github.com/Geta/SEO.Sitemaps/pull/87)

## [2.0.12]

- Setting current site definition when generating URLs to get correct URLs.

## [2.0.11]

- Added language details, site definition and sitemap data for ContentFilter to be able to filter in different contexts.

## [2.0.8]

- [#60](https://github.com/Geta/SEO.Sitemaps/issues/60) Fixed hosts which differ by scheme not added to the site hosts.
- [#80](https://github.com/Geta/SEO.Sitemaps/pull/80) Checking if SEOSitemaps property is not empty when generating XML.

## [2.0.7]

- [#73](https://github.com/Geta/SEO.Sitemaps/issues/73) - Fixed sitemap XML validation issues.
- Upgraded Commerce project to Episerver Commerce 12

## [2.0.4]

- Use InvariantCulture when writing lastmod

## [2.0.3]

- Update documentation and dependencies

## [2.0.2]

- Added support for Episerver 11

## [1.6.1]

- Added support for Episerver 10

## [1.5.0]

- Added support for EPiServer 9
- Removed depedency on log4net

## [1.4.1]

- Added support for alternate language pages. See details at https://support.google.com/webmasters/answer/2620865?hl=en.
- Added sitemap index support (/sitemapindex.xml) that might be useful if you have more than one sitemap on your site.
- Added a new sitemap format, Standard and Commerce, including both standard and commerce pages in one single sitemap (requires the Geta.Seo.Sitemaps.Commerce NuGet package).
- Added ability to create language specific sitemaps.

## [1.0.0]

- Initial version
