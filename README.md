# Ngrok Extensions for ASP.NET Core applications

TODO add new build and build status
[![Build status](https://ci.appveyor.com/api/projects/status/mi2kn7oaluldhuyo/branch/master?svg=true)](https://ci.appveyor.com/project/dprothero/ngrokextensions/branch/master)

This extension allows you to start [ngrok](https://ngrok.com) right from your asp.net core application.

## Installation

TODO Add nuget information here

## Configuration

TODO Add documentation for IWebHostBuilder extensions

### Custom ngrok Subdomains

TODO Add back functionality of reading in appsettings

If you have a paid ngrok account, you can make use of custom subdomains with
this extension.

If you are using an ASP.NET Core or Azure Functions project and want to test locally, you can set the
`ngrok.subdomain` key in the `appsettings.json` file like so:

```json
{
  "IsEncrypted": false,
  "Values": {
    "ngrok.subdomain": "my-cool-app",
    ... more app settings omitted ...
  }
}
```

You can also set this value in a `secrets.json` file as [described here](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?tabs=visual-studio).

## Feedback and Contribution

This is a brand new extension and would benefit greatly from your feedback
and even your code contribution.

If you find a bug or would like to request a feature,
[open an issue](https://github.com/dprothero/NgrokExtensions/issues).

To contribute, fork this repo to your own GitHub account. Then, create a
branch on your own fork and perform the work. Push it up to your fork and
then submit a Pull Request to this repo. This is called [GitHub Flow](https://guides.github.com/introduction/flow/).

## Remaining Work

* Populate port number from launchSettings.json
* Add place to configure ngrok.exe path. Consider cross platform compatibility.
* Add IWebHostBuilder
* Add ability to inject IConfiguration into the pipeline
* Changeup unit tests
* Writeout tunnel information to an IOptions<TunnelConfig> or similar
* Come up with strategy to share code back to parent project
	* Split out process management parts to it's own netstandard2.0 library
	* Retain visual studio extension parts in a separate project targeted to netframework541
	* Contain all my changes for asp.net core it's own library. This library should contain just IWebHostBuilder, IAppBuilder, etc extension methods

## Change Log

* v0.9.10 - Allow settings override in secrets.json. Thanks @ChristopherHaws!
* v0.9.9 - Bug fixes. Find projects within Solution folders.
* v0.9.8 - Bug fixes. Automatically install ngrok.exe if not found.
* v0.9.7 - Support for ASP.NET Core projects. Thanks @ahanoff!
* v0.9.6 - Added support for Visual Studio 2017.
* v0.9.5 - Added support for Azure Function projects.
* v0.9.4 - Added support for Node.js projects.
* v0.9.3 - Fix crash when decimal values in ngrok's JSON response.
* v0.9.2 - Allow customizing location of ngrok.exe.
* v0.9.1 - Initial Release

* * *

Licensed under the MIT license. See the LICENSE file in the project root for more information.

Copyright (c) 2017 David Prothero
