![RouteCheck-Logo](https://github.com/eisnstein/RouteCheck/blob/main/src/Assets/icon.png)

# RouteCheck

[![NuGet Package](https://img.shields.io/nuget/vpre/RouteCheck)](https://nuget.org/packages/RouteCheck)
[![License](https://img.shields.io/badge/license-MIT-yellow)](https://github.com/eisnstein/RouteCheck/blob/main/LICENSE)

Check your API routes in your Terminal.

---

RouteCheck is a dotnet tool for checking/viewing the routes of your ASP.NET Core app in your terminal, like `php artisan route:list` or `mix phx.routes`. The `check` command (default) shows you all routes in a nice table: minimal APIs, controllers, Razor Pages and everything else that is mapped as an endpoint, together with the handler of each route.

RouteCheck works with ASP.NET Core apps on .NET 8 or later. No OpenAPI package or other changes to your app are needed.

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

```
+--------+--------------------------+-------------------------+
| Method | Path                     | Handler                 |
|--------+--------------------------+-------------------------|
| ANY    | /                        | Page /Index             |
| GET    | /api/orders              | OrdersController.List   |
| GET    | /api/orders/{id:int}     | OrdersController.Get    |
| PUT    | /api/orders/{id:int}     | OrdersController.Update |
| GET    | /health                  | Health.Check            |
| ANY    | /Orders/Details/{id:int} | Page /Orders/Details    |
| GET    | /pets                    | Program (lambda)        |
+--------+--------------------------+-------------------------+
```

- `ANY` means the route accepts every HTTP method, like Razor Pages do.
- For minimal APIs, the handler is the method passed to `MapGet` etc. A lambda is shown as `(lambda)` with the class it is declared in.

You can also provide a path to the web app's directory or project file via the `path` option:

```sh
routecheck --path /absolute/path/to/webapp

# or

routecheck -p relative/path/to/webapp/WebApp.csproj
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

Matching is case-insensitive, like ASP.NET Core routing. `method` and `filter` can be combined. Routes accepting any method (`ANY`) are shown for every `method`.

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
      "path": "/pets",
      "handler": "PetsController.List"
    }
  ],
  "error": null
}
```

- `project` is the project file relative to the current directory.
- `handler` is `null` if RouteCheck can't tell what handles the route.
- On an error, `error` contains the message and the exit code is `1`. Otherwise `error` is `null` and the exit code is `0`.
- `--output markdown` prints the same as a Markdown table, e.g. for pull request comments or job summaries.

### How it works

RouteCheck builds your app and starts it with `dotnet run`, adding a [startup hook](https://github.com/dotnet/runtime/blob/main/docs/design/features/host-startup-hook.md) that replaces the web server. Once the app has mapped its endpoints, the hook reads them from ASP.NET Core's routing and stops the app again. So your app never listens on a port, and nothing has to be added to it.

- Your app's startup code runs as usual, e.g. hosted services or database migrations on startup.
- The app is started with the environment `Development`, unless your launch profile or the `ASPNETCORE_ENVIRONMENT` environment variable sets another one.
- Startup hooks your app already uses (`DOTNET_STARTUP_HOOKS`) are kept.

For help run:

```sh
routecheck -h
```

## LICENSE

MIT
