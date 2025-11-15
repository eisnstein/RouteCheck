![PackCheck-Logo](https://github.com/eisnstein/PackCheck/blob/main/src/Assets/icon.png)

# RouteCheck

[![License](https://img.shields.io/github/license/eisnstein/PackCheck)](https://github.com/eisnstein/PackCheck/blob/main/LICENSE)

Check your API routes in your Terminal.

---

RouteCheck is a dotnet tool for checking / viewing your API routes in your terminal. The `check` command (default) shows you all routes in a nice table with some basic information. RouteCheck basically starts your application and calls the `openapi/v1.json` endpoint and displays the returned JSON.

> Currently this only works if you are using the `Microsoft.OpenApi`.

## Installation

You can install PackCheck as a dotnet tool via NuGet:

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

![PackCheck check example](https://github.com/eisnstein/PackCheck/blob/main/src/Assets/packcheck-check.png)

For help run:

```sh
routecheck -h
```

## LICENSE

MIT
