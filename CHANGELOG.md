# Changelog

New entries go under "Unreleased". For a release, rename it to `## x.y.z (YYYY-MM-DD)` and add a new empty "Unreleased" section. The release workflow takes the version's section as the GitHub release notes.

Each version lists its changes in this order, leaving out empty sections: Breaking Changes, Enhancements, Bug fixes.

## Unreleased

### Enhancements

- The routes table no longer has a border.

## 0.2.0 (2026-10-19)

### Enhancements

- RouteCheck now reads the routes from the app itself instead of its OpenAPI document. It shows all endpoints: minimal APIs, controllers, Razor Pages and everything else mapped as an endpoint. The `Microsoft.AspNetCore.OpenApi` package is no longer needed.
- New Handler column (and `handler` in `--output json`/`markdown`), e.g. `OrdersController.Get`, `Page /Orders/Details` or `Program (lambda)`.
- Routes accepting any HTTP method, like Razor Pages, are shown as `ANY`, and are included for every `--method`.
- The app no longer listens on a port, and is stopped as soon as its routes are read. A startup hook replaces the web server; startup hooks already set in `DOTNET_STARTUP_HOOKS` are kept.
- `--openapi-endpoint` is no longer needed and is ignored. It is still accepted, so existing scripts keep working.

## 0.1.0 (2026-10-18)

### Enhancements

- The routes are sorted by path, and routes with the same path by method (GET, POST, PUT, PATCH, DELETE, ...).
- New `-m|--method` option to show only routes with the given HTTP methods, e.g. `-m get,post`.
- New `-f|--filter` option to show only routes whose path contains the given text, or matches a wildcard pattern (`*` or `%`), e.g. `-f "/pets*"`.
- New `--output` option (`table`, `json` or `markdown`). With `json` and `markdown`, only the result is printed, as one document, so RouteCheck works well in scripts, CI jobs and with LLM agents. Errors are part of the document.
- Shows "No routes found." instead of an empty table.
- The web app is now built before it is started, and build errors are shown. The startup timeout no longer includes the build.
- New `--timeout` option for the seconds to wait for the web app to start (default 20).
- `--path` can now also be a project file. RouteCheck looks for a `*.csproj` file instead of `Program.cs`, so projects with their entry point in another file work too. If a directory contains several project files, RouteCheck lists them.
- The web app is started in the `Development` environment, so the OpenAPI endpoint is available without a launch profile. A launch profile or `ASPNETCORE_ENVIRONMENT` still takes precedence.
- Clear error messages when the OpenAPI endpoint is missing (with a hint about `AddOpenApi()` and `MapOpenApi()`), fails or returns no OpenAPI document.
- The option `--openApiEndpoint` is now called `--openapi-endpoint`. The old name still works.
- When the web app exits before it is ready, the error now shows its last output lines, e.g. the exception it crashed with.
- Ctrl+C cancels the check and stops the started web app.
- GitHub releases now take their notes from this changelog.

### Bug fixes

- The started web app is now stopped reliably. Before, only the `dotnet run` process was killed, so the web app itself kept running, also after errors.
- The web app's output is now read continuously, so a web app that logs a lot no longer blocks during startup.
- The exit code is now `0` on success and `1` on errors. Before, a successful check returned `1` and errors `-1`.
- RouteCheck no longer fails when offline or when nuget.org is unreachable. The update check is skipped after at most 3 seconds.
- The update check now finds the latest version regardless of the order the feed returns them in.
- The update check now knows the installed version. Before, released packages reported `1.0.0` internally, so no update was ever shown.
- Paths containing `[` or `]` no longer crash RouteCheck.

## 0.0.3 (2025-11-20)

### Enhancements

- Add self version check
- Add test setup

## 0.0.2 (2025-11-17)

### Enhancements

- Add `path` option to be able to give an absolute or relative path to the web app
- Add `openApiEndpoint` option to be able to set a custom endpoint

## 0.0.1 (2025-11-15)

- Initial release
