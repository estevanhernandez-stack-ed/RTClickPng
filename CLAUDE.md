# Right Click PNG

Windows 11 context-menu image converter, live on the Microsoft Store. Three processes in one MSIX: a C++/WinRT shell extension hosted in `dllhost.exe`, a .NET 9 Native AOT engine exe, and a WPF Settings app. Build, test, and architecture are in `README.md`; CI mirrors them in `.github/workflows/build.yml`.

## Product commitments (non-negotiable)

- **No network calls at runtime, ever.** No telemetry, crash reporting, update checks, or analytics. Build-time fetching via vcpkg is fine. PRs that add runtime network get closed.
- **Originals are never modified.** Every verb writes a new file or a clipboard payload.
- **Any change to what the app writes locally ships with disclosure in three places together:** `PRIVACY.md`, the `## Privacy` section of `README.md`, and the `privacy-rtclickpng` section of `626labs.dev/privacy.html`, which lives in the `626labs-hub` repo.

## Gotchas

- **`InvariantGlobalization=true` is per-project (Engine, Engine.Tests), never in `Directory.Build.props`.** WPF dies silently on first text layout under it: no WER, no log, before any handler runs. `Settings.csproj` pins it to `false` explicitly as a guard; don't "clean up" that line.
- **Packaging mistakes fail silently.** A wrong CLSID format, DLL path, or manifest namespace makes the verbs vanish with no error. The only proof is install, restart Explorer, right-click a real file.
- **Opaque packaged-app failures are environment, not code.** Walk the build-config chain (props cascade, publisher identity, cert trust) before rewriting a layer. Never recommend de-scoping a visible surface without naming what the user loses. This exact miss cost the v0.1 build a mid-project handoff (`docs/reflection.md`).
- **The Package Family Name is hardcoded twice:** `src/Shared/Paths.cs` and `src/ShellExtension/SettingsReader.cpp`. Both move in lockstep with the manifest `Identity`, or Settings and the shell extension read different `settings.json` files.
- **The version lives in four places:** `Version`, `AssemblyVersion`, `FileVersion` in `Directory.Build.props`, plus `Identity.Version` in `Package.appxmanifest`. Bump the third digit (`0.1.1.0` to `0.1.2.0`). The revision digit stays `0` forever (Store policy). Every upload burns its version, rejected ones included.
- **`DisplayName` "Right Click to PNG" exactly matches the Partner Center reservation.** The friendly "Right Click PNG" is the tile `ShortName`. The mismatch is intentional, so don't unify them.
- **`EntryPointProjectUniqueName` in `Package.wapproj` overrides the manifest `Executable`.** Both must point at Settings, or the tile launches the Engine console.
- **CLSIDs are permanent ABI.** One parent verb (`ParentCommand`) is the only verb registered per file type; the five commands are its submenu, built in `SubCommandOrder.h` order. A new command gets a new CLSID in the manifest `ComServer`, a `SubCommand` entry, a case in `ParentCommand.cpp`'s `MakeSubCommand`, and a `dllmain.cpp` factory branch, with `docs/spec.md` and `src/ShellExtension/README.md` updated. Never reuse or renumber one, and never drop a shipped one from the `ComServer`.
- **Engine stdout is a binary channel.** Copy streams a 4-byte big-endian length prefix plus PNG bytes on stdout. Diagnostics go to stderr, or Copy corrupts.
- **The shell extension runs inside Explorer's host.** Keep it thin and `noexcept`; decode work belongs in the Engine, across the process boundary, so a bad file crashes the Engine and not Explorer.
- **libheif must keep `default-features: false`** in `vcpkg.json`. The default `hevc` feature pulls GPL-2.0 x265.
- **Building the MSIX outside a VS Developer shell fails with `MSB3073` at the Engine's Native AOT link step.** The ILCompiler targets call `vswhere.exe`. Prepend `${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer` to `PATH` first.
- **Local sideload under the production publisher:** `Add-AppxPackage -AllowUnsigned` fails with `0x80073D2C`. Use `Add-AppxPackage -Register <loose-layout>\AppxManifest.xml` instead.
- **"Verbs missing" reports:** check for the classic-menu registry tweak before debugging code (README, Troubleshooting).

## Docs that look current but aren't

`docs/scope.md`, `prd.md`, `spec.md`, `checklist.md`, `reflection.md`, and `process-notes.md` are Vibe Cartographer build artifacts from April 2026. `spec.md` still describes .NET 10 and WinUI 3. The csproj files win; the shipped stack is .NET 9 and WPF. When adding a CLSID or verb, update `spec.md` anyway (CONTRIBUTING requires it).

## Releasing

Store mechanics, reviewer notes, and every identity trap hit so far: `docs/ms-store-submission-playbook.md`. **The GitHub Release is cut only after the Store update is live**, titled `vX.Y.Z — Live on the Microsoft Store`; the 626 Discord feed announces Releases, so cutting one at submission sends people to an update they can't get yet. Tags are three-part (`v0.1.2`) even though the package version is four-part (`0.1.2.0`). Per-release reviewer copy lives in `docs/release/`. Open product work is in `BACKLOG.md` and on the dashboard.
