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

### Filter routes

The routes are sorted by path, and routes with the same path by method. To show only routes with certain HTTP methods, use the `method` option. It can be used multiple times or with comma-separated values:

```sh
routecheck --method GET -m post

# or

routecheck -m get,post
```

To show only routes whose path contains a text, use the `filter` option. With a wildcard (`*` or `%`), the pattern has to match the whole path. It can be used multiple times, a route has to match one of them:

```sh
# /pets, /pets/{id}, /users/{id}/pets, ...
routecheck --filter pets

# /pets, /pets/{id}, but not /users/{id}/pets
routecheck -f "/pets*"
```

Matching is case-insensitive, like ASP.NET Core routing. `method` and `filter` can be combined.

### Output for scripts and LLM agents

With `--output json` or `--output markdown`, RouteCheck prints only the result, as one document: no status lines and no update notice.

```sh
routecheck --output json
```

```json
{
  "command": "check",
  "project": "src/Api/Api.csproj",
  "routes": [
    {
      "method": "GET",
      "path": "/pets"
    }
  ],
  "error": null
}
```

- `project` is the project file relative to the current directory.
- On an error, `error` contains the message and the exit code is `1`. Otherwise `error` is `null` and the exit code is `0`.
- `--output markdown` prints the same as a Markdown table, e.g. for pull request comments or job summaries.

### Environment

The app is started with the environment `Development`, unless your launch profile or the `ASPNETCORE_ENVIRONMENT` environment variable sets another one.

For help run:

```sh
routecheck -h
```

## LICENSE

MIT
