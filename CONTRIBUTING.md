# Contributing

Thanks for the interest. Right Click PNG is small and specific — we want it to do
its one job fast, right, offline. Contributions that fit that frame are welcome.

## Ground rules

- **No network calls ever.** This is a product commitment. PRs that add a network
  dependency at runtime will be closed. Fetching a new decoder binary at build time
  is fine (that's just `vcpkg`).
- **No telemetry, no analytics, no crash reporting phone-home.** Same reason.
- **Keep the decoder surface minimal.** If you want to add a new source format,
  open an issue first so we can talk about whether bundling another codec is worth
  the MSIX size hit.

## Dev setup

See the *Build from source* section in [README.md](README.md). Short version:

1. VS 2022 Build Tools with Desktop C++ + Universal Windows Platform workloads.
2. .NET 9 SDK.
3. `./build/fetch-native.ps1` once. The vcpkg build is slow the first time; it caches after.
4. `./build/create-dev-cert.ps1` once (elevated PS).

## Coding conventions

- **.NET**: idiomatic C# 13. `internal` by default, `public` only when the type
  crosses an assembly boundary. `nullable enable` on. Source-generated JSON for AOT.
- **C++**: C++20. `snake_case` for locals, `PascalCase` for types + methods. `noexcept`
  on everything in the shell extension (a thrown exception in dllhost tends to end badly).
  `/W4` + `/WX` — warnings are errors.
- **PowerShell scripts**: `pwsh` 7+, `[CmdletBinding()]` on top-level functions,
  `$ErrorActionPreference = 'Stop'`.

## Tests

PRs that change logic without a test usually get asked to add one.

- **Engine changes**: xUnit in `tests/Engine.Tests/`. The fixture set in
  `tests/Engine.Tests/fixtures/` is small + permissive-licensed; extend it rather
  than inventing ad-hoc assets.
- **Shell extension changes**: the C++ test runner in `tests/ShellExtension.Tests/`
  uses a hand-rolled `TEST` macro — no gtest or Catch2 dep. Add cases inline.
- **Visibility / menu matrix changes**: these MUST be covered in
  `FileFilterTests.cpp`.

## PR checklist

- [ ] Tests green locally (`dotnet test` + `ShellExtensionTests.exe`).
- [ ] `./build/verify-crt.ps1` still passes.
- [ ] MSIX builds cleanly: `msbuild src/Package/Package.wapproj /p:Configuration=Release /p:Platform=x64`.
- [ ] If touching native P/Invoke: every buffer length is checked; every allocation is freed.
- [ ] If adding a CLSID or verb: `docs/spec.md` + `src/ShellExtension/README.md` reflect it.
- [ ] If touching the manifest: tested a full uninstall → rebuild → reinstall on Win11 22H2.

## Reporting bugs

Issue templates live in `.github/ISSUE_TEMPLATE/`. Please include:

- Source file format + a tiny reproducible sample (permissively-licensed) if you can attach one.
- Windows build number (`winver`).
- The engine's exit code if you can capture it (run from a command line:
  `RTClickPng.Engine.exe convert <src> <dst>` and report `$LASTEXITCODE`).

## License

By contributing you agree your changes are under the MIT license (see [LICENSE](LICENSE)).
