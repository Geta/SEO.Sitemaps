# Development tips

## Fetching dependencies 
The supplied `NuGet.config` adds the EpiServer feed to your list
of feeds, so there should be little point in tinkering with feed
settings.

### Using Visual Studio
Right-click the Solution in the solution explorer and click 
"Restore NuGet Packages".

### Using NuGet on the CLI
```
nuget restore 
```

## Working with Mono
If you have fetched all dependencies using NuGet you can proceed to 
build the solution by issuing `xbuild`.

### Running tests
```
TESTRUNNER=./packages/xunit.runner.console.2.1.0/tools/xunit.console.exe
mono $TESTRUNNER Tests/bin/Debug/Tests.dll
```
