# Changelog

New entries go under "Unreleased". For a release, rename it to `## x.y.z (YYYY-MM-DD)` and add a new empty "Unreleased" section. The release workflow takes the version's section as the GitHub release notes.

Each version lists its changes in this order, leaving out empty sections: Breaking Changes, Enhancements, Bug fixes.

## Unreleased

### Enhancements

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
