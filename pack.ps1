$outputDir = "D:\NuGetLocal\"
$build = "Release"
$version = "1.0.0"

nuget.exe pack .\src\Geta.SEO.Sitemaps\Geta.SEO.Sitemaps.csproj -IncludeReferencedProjects -properties Configuration=$build -Version $version -OutputDirectory $outputDir
nuget.exe pack .\src\Geta.SEO.Sitemaps.Commerce\Geta.SEO.Sitemaps.Commerce.csproj -IncludeReferencedProjects -properties Configuration=$build -Version $version -OutputDirectory $outputDir
