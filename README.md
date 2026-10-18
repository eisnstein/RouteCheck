![RouteCheck-Logo](https://github.com/eisnstein/RouteCheck/blob/main/src/Assets/icon.png)

# RouteCheck

[![NuGet Package](https://img.shields.io/nuget/vpre/RouteCheck)](https://nuget.org/packages/RouteCheck)
[![License](https://img.shields.io/badge/license-MIT-yellow)](https://github.com/eisnstein/RouteCheck/blob/main/LICENSE)

Check your API routes in your Terminal.

---

RouteCheck is a dotnet tool for checking/viewing your API routes in your terminal. The `check` command (default) shows you all routes in a nice table with some basic information. RouteCheck builds and starts your application in the `Development` environment, calls the `/openapi/v1.json` endpoint and displays the returned document.

> Currently this only works if you are using the `Microsoft.AspNetCore.OpenApi` package and expose the generated JSON via an endpoint (`builder.Services.AddOpenApi()` and `app.MapOpenApi()`).

## Installation

You can install RouteCheck as a dotnet tool via NuGet:

```sh
# Install
dotnet tool install --global RouteCheck

# Update
dotnet tool update --global RouteCheck
```

## Usage

In your terminal `cd` into a ASP.NET project and run:

```sh
routecheck
```

This should give you something like this:

![RouteCheck check example](https://raw.githubusercontent.com/eisnstein/RouteCheck/main/src/Assets/routecheck-check.png)

You can also provide a path to the web app's directory or project file via the `path` option:

```sh
routecheck --path /absolute/path/to/webapp

# or

routecheck -p relative/path/to/webapp/WebApp.csproj
```

If you use a custom OpenApi endpoint, you can set the `openapi-endpoint` option:

```sh
routecheck --openapi-endpoint /some/other/endpoint
```

RouteCheck waits 20 seconds for your app to start (after building it). If your app needs longer, e.g. because it runs migrations on startup, set the `timeout` option:

```sh
routecheck --timeout 60
```

The app is started with the environment `Development`, unless your launch profile or the `ASPNETCORE_ENVIRONMENT` environment variable sets another one.

For help run:

```sh
routecheck -h
```

## LICENSE

MIT
