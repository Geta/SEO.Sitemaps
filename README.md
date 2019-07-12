# SEO.Sitemaps

![](<http://tc.geta.no/app/rest/builds/buildType:(id:TeamFrederik_Sitemap_Debug)/statusIcon>)
[![Platform](https://img.shields.io/badge/Platform-.NET%204.6.1-blue.svg?style=flat)](https://msdn.microsoft.com/en-us/library/w0x726c2%28v=vs.110%29.aspx)
[![Platform](https://img.shields.io/badge/Episerver-%2011-orange.svg?style=flat)](http://world.episerver.com/cms/)

Search engine sitemaps.xml for EPiServer CMS 11 and Commerce 13

## Description

This tool allows you to generate xml sitemaps for search engines to better index your EPiServer sites. Although there are several EPiServer sitemap tools available like [SearchEngineSitemaps](https://www.coderesort.com/p/epicode/wiki/SearchEngineSitemaps) and [EPiSiteMap](http://episitemap.codeplex.com/) which have inspired this project this tool gives you some additional specific features.

## Features

- sitemap generation as a scheduled job
- filtering pages by virtual directories
- ability to include pages that are in a different branch than the one of the start page
- ability to generate sitemaps for mobile pages
- it also supports multi-site and multi-language environments

See the [editor guide](docs/editor-guide.md) for more information.

## Latest release

The latest version is available on the EPiServer NuGet feed. You can find it by searching the term Geta.SEO.Sitemaps.

## Download

From nuget.episerver.com feed.

## How to get started?

1. Install Sitemap plugin via NuGet in Visual Studio. Ensure that you also install the required dependencies.

```
  Install-Package Geta.SEO.Sitemaps
  Install-Package Geta.SEO.Sitemaps.Commerce
```

2. Rebuild your solution.
3. Configure sitemap settings and schedule the sitemap generation process. Configuration available at CMS -> Admin Mode -> Search engine sitemap settings. See the [editor guide](docs/editor-guide.md)

#### Enabling multi language support

Add this to your web.config file:

```xml
<configuration>
<configSections>
<section name="Geta.SEO.Sitemaps" type="Geta.SEO.Sitemaps.Configuration.SitemapConfigurationSection, Geta.SEO.Sitemaps"/>
</configSections>

  <Geta.SEO.Sitemaps>
    <settings enableLanguageDropDownInAdmin="true" />
  </Geta.SEO.Sitemaps>
</configuration>
```

### Dynamic property for specific pages

You can specify page specific sitemap properties (like change frequency, priority or include/disinclude the specific page in any sitemap) for each EPiServer page by defining a dynamic property with type SEOSitemaps (and the same name):
![Create dynamic property](docs/images/SitemapDynamicPropertyDefine.png?raw=true)

and specify values for the dynamic property:
![Set value for the dynamic property](docs/images/SitemapDynamicPropertyOnPage.PNG?raw=true)

### Adding Sitemap Properties to all content pages

Credits to [jarihaa](https://github.com/jarihaa) for [contributing](https://github.com/Geta/SEO.Sitemaps/pull/87) this.

```
[UIHint("SeoSitemap")]
[BackingType(typeof(PropertySEOSitemaps))]
public virtual string SEOSitemaps { get; set; }
```

## Limitations

- Each sitemap will contain max 50k entries (according to [sitemaps.org protocol](http://www.sitemaps.org/protocol.html#index)) so if the site in which you are using this plugin contains more active pages then you should split them over multiple sitemaps (by specifying a different root page or include/avoid paths for each).

## Local development setup

In order to debug or contribute to the package, the QuickSilver demo site which is included in the repository can be used.

### Prerequisites

- Checkout the repository
- Install Docker on your local machine: https://docs.docker.com/get-started/

### Get started

Set the `docker-compose` as default project (if not already by default). Now, the required images are downloaded (windows server and sql server), this will take some time. Note, this only happens the first time after that it cached on your local machine. See Output window (source Docker) to follow the progress. After you see the line 'Docker containers are ready', you are ready to run the project.

![Docker output](docs/images/docker-output.PNG)

After the images are downloaded just run the project and start debugging the code. The frontend and backend code can be found in the Geta.SEO.Sitemaps and Geta.SEO.Sitemaps.Commerce project. The frontend code is available under the module folder.

When the browser is not automatically opened, go to the docker output window and check the IP addresses.

![Docker debugging output](docs/images/docker-debugging-output.PNG)

You can find the IP addresses also in the `docker-compose.yml` for Quicksilver and manager:
| Site | IP Address |
| ------------- |:-------------:|
| Quicksilver | 172.16.238.11 |
| Manager | 172.16.238.12 |

### QuickSilver login

Use the default admin@example.com user for QuickSilver, see [Installation](https://github.com/episerver/Quicksilver).

## Contributing

See [CONTRIBUTING.md](./CONTRIBUTING.md)

## Package maintainer

https://github.com/frederikvig

## Changelog

[Changelog](CHANGELOG.md)
