# Reflection — Right Click PNG

**Ship date:** 2026-04-22 (Microsoft Store, approved same day submission)
**From `/onboard` to Store listing:** roughly 24 hours wall-clock, across two agent sessions.

---

## One-sentence summary

Right Click PNG shipped end-to-end with 85 passing tests, a signed MSIX on the Store, and the full brand polish — **and the finish line required a mid-project agent handoff because I mishandled the one opaque blocker I hit.**

---

## What landed

- **Engine** — .NET 9 Native AOT CLI. 7 decoders (libwebp, libavif[dav1d], libheif[libde265], libspng, libjpeg-turbo, pure-C# BMP/GIF/TIFF), 2 encoders (PngEncoder with iCCP, JpegEncoder with APP2 ICC splicing). 963 KB binary, ~20 ms cold start. EXIF orientation applied + stripped. Overwrite policy honored per-invocation.
- **Shell Extension** — C++/WinRT DLL with five `IExplorerCommand` classes: Convert-PNG, Copy-PNG, Convert-JPEG, Copy-JPEG, and a context-menu Settings verb. Full visibility matrix via `FileFilter`. Three-format clipboard writer (CF_DIBV5 / CF_DIB / registered PNG) so paste works in Figma, Photoshop, Teams, Paint.
- **Settings** — WPF window, dark-titlebar via `DwmSetWindowAttribute`, two `ToggleSwitch`-styled checkboxes bound to the Shared `SettingsSchema`.
- **MSIX** — wapproj, signed with real Partner Center publisher identity, icons rendered via C# + GDI+.
- **CI** — GitHub Actions windows-latest, Debug + Release matrix, vcpkg GitHub-Actions binary cache for decoder builds, signed-artifact upload.
- **Docs** — README, CONTRIBUTING, LICENSE (MIT), per-library licenses.md with formal libheif LGPL-3.0 compliance reasoning, Store-submission playbook, `docs/spec.md` kept in sync.
- **85 passing tests** — 40 xUnit (decoders, encoders, ICC, EXIF, overwrite policy) + 45 C++ (FileFilter visibility matrix).

---

## Process retro — peer style

### What worked

- **Autonomous build mode with checkpoints at items 3, 6, 10 was right for this scope.** Fast where it could be, human-in-loop where it had to be (MSIX install verification, right-click menu spot-check, Store submission). The checkpoint at 6 caught real MSIX deployment issues (CLSID registration silently dropping, `desktop5:` vs `desktop4:` namespace).
- **Parallelizing long-running jobs** — kicked off vcpkg bootstrap + .NET 10 SDK install concurrently while writing license doc + fetch-native script. Saved an hour of wall-clock.
- **Commit-per-item discipline** — 44 commits in the log, each one a coherent revert point. When I broke AOT publishing during the .NET 10 → .NET 9 retarget, the previous commit was clean to bisect against.
- **Native library integration**. Navigated five vcpkg builds, caught a GPL-2.0 leak (libheif's default-`hevc` feature pulling x265) before it shipped, tracked struct layout drift across libavif versions (the `avoidLibYUV` field added in 1.1), wrote hand-rolled pure-C# TIFF / GIF / BMP / EXIF parsers. Decoder round-trips all passed first try on every fixture after the `SPNG_CTX_ENCODER=2` / `SPNG_FMT_PNG=256` constant fixes.

### What I got wrong

- **I abandoned the Settings UI at the wrong layer.** Both WinUI 3 and WPF exited silently during packaged-app activation — no WER, no crash log, no Application Error event. I kept retreating through the tech stack (WinUI 3 → WPF → shell-extension-spawned → Notepad-on-settings.json) and eventually recommended shipping without a real UI.
  - **The actual root cause was the self-signed dev cert + Publisher CN mismatch** with what packaged .NET activation trusts. Once a different agent swapped to Este's real Partner Center identity (`CN=177BCE59-0966-4975-9962-10E36652141F`, PFN `626LabsLLC.RightClicktoPNG_wz1chhb2h2v4a`), the WPF code I'd already written ran on first launch. My Notepad workaround was reverted in the same PR.
  - **What I should have asked:** "Do you have a Partner Center publisher identity we can use instead of this self-signed dev chain?" One question, hours saved.
- **I treated "ship without it" as a neutral fallback.** It isn't. De-scoping a visible product surface reshapes the product story in a way switching tech stacks or cert contexts doesn't. For a user-facing feature, keep debugging or hand off with a specific theory — don't recommend cutting.
- **Diagnostic gap on opaque failures.** Silent-exit-with-no-WER on a .NET exe while C++ shell extension works in the same MSIX is a highly specific signature. It's almost never the code; it's the environment. I didn't form that hypothesis.

### What I'd do differently next time

1. When packaged .NET activation silent-exits, **check cert trust and publisher identity before changing tech stacks.** Ask the builder about their Partner Center setup early.
2. **Normalize the identity during `/spec`, not at ship time.** If the project is for the Store, get the real Partner Center publisher CN into `Package.appxmanifest` during the spec phase. Ship-blocking identity swaps at item 9 are a false economy.
3. **Build a "this is broken at the environment layer, not the code layer" diagnostic bundle** as a reusable skill — event log grep patterns, cert-chain reads, WER archive inspection, `Get-StartApps` checks. Faster triage when opaque activation fails.
4. **Name the de-scoping cost explicitly before offering it.** "If we defer this, the Start-menu tile still shows and looks broken" — that was actually the user's objection, and I should have been the one raising it.

---

## Qualitative review of artifacts

### `docs/scope.md`

Tight. Triple outcome (ship Store / open source / solve a real pain) anchored the whole build. The "stack might surprise us" line turned out prescient — the stack didn't surprise, but the packaging identity interaction did.

### `docs/prd.md`

Epics 1-5 mapped 1:1 to checklist items 7-9, which made the build phase easy to plan. The visibility-matrix language in Epic 3 translated directly into the `FileFilter` predicates and the C++ unit tests. One gap: the PRD didn't call out cert + identity as a first-class dependency — and that's the thing that blocked ship.

### `docs/spec.md`

34 KB, accurate. The `Open Issues` section (libheif LGPL, decoder binary acquisition, BMP/TIFF/GIF choice, icons) caught every real architectural decision I'd face. Pre-spec'd the return-code scheme for the engine, which made the shell extension integration a paint-by-numbers later.

What was missing from spec: **the publisher identity contract.** Partner Center registration, Publisher CN, Package Family Name — these are ship-blocking facts that weren't in spec. v2 of the `/spec` template should include an "Identity & Signing" section for any Store-bound project.

### `docs/checklist.md`

12 items, checkpoints at 3/6/10, autonomous mode. Right shape for this build. Item 9 was the one that ate time out of proportion to its surface area. In hindsight, item 9 should have had a "pre-flight: confirm publisher identity is production-ready" sub-step, not just "build the window."

### Code

- **`src/Engine/`** — clean. P/Invoke via `[LibraryImport]` + `[ModuleInitializer]` that preloads native DLLs in dependency order (once .NET 9 revealed it doesn't automatically search the DLL's own directory for transitive loads). `AvifDecoder` eventually used `avifDecoderReadMemory` into a caller-owned `avifImage`, which is the more stable path across libavif versions — but I got there after a painful detour reading `decoder->image` at a fixed offset and eating a NullReferenceException because the `avifRGBImage` struct was missing the `avoidLibYUV` field.
- **`src/ShellExtension/`** — CRTP base class for `IExplorerCommand`, five command classes, clean `FileFilter` + `SettingsReader`. Notable miss early on: had the CLSIDs and path wrong in the manifest, and the MSIX schema *silently dropped* COM registration for bare GUIDs vs the expected format — took 3 reinstall cycles to catch because the deployment didn't surface any error. Now flagged in memory.
- **`src/Shared/SettingsSchema.cs`** — source-gen JSON context keeps AOT happy. Two-field schema with `schemaVersion` carries future migration hooks.
- **`tests/`** — 85 passing. The xUnit `[ModuleInitializer]` trick (native DLL preload runs via assembly-load) made decoder tests portable between the engine exe and the test runner. C++ tests use a plain TEST macro + `wmain` exit code — less flash than gtest, zero deps.

### Icons (`src/Package/Assets/`)

Fired up the 626Labs design skill, landed a document + cursor glyph on navy with a cyan/magenta radial glow. Wide tile auto-fits the wordmark to avoid clipping. 626Labs voice held up: no emoji, sentence case, SETTINGS FILE as a mono uppercase eyebrow. Deferred to `System.Drawing` through a tiny scratch C# tool because PowerShell's GDI+ handling kept throwing `op_Subtraction` errors on `[System.Object[]]` — two pivots to get there.

### CI

Single workflow, matrix Debug+Release, vcpkg binary cache wired through `VCPKG_BINARY_SOURCES=x-gha,readwrite`. Secrets documented in `.github/workflows/README.md`. Will be seconds after the first run, ~30-60 min on the first.

### Docs

README tight with a visibility-matrix table, privacy note, build-from-source that mirrors CI, license summary. `docs/licenses.md` has a real formal LGPL-3.0 compliance paragraph. `docs/ms-store-submission-playbook.md` was added post-handoff and captures everything about Partner Center identity + Store policy I didn't know to ask about.

---

## What's saved to memory (for future Carts)

- **`memory/feedback_packaged_dotnet_silent_exit.md`** — the elimination target. When a packaged MSIX .NET/WinUI/WPF exe silent-exits during activation with no WER and no log, suspect cert trust / publisher identity before de-scoping or switching tech stacks. Packaged-C++ DLLs in the same MSIX can work fine while the .NET exe path fails; that's not the code, that's the environment.

---

## Scoreboard

| Dimension | Rating | Why |
|---|---|---|
| Functional completeness | Shipped | All 12 items landed; Store-approved |
| Code quality | High | 85 tests green, warnings-as-errors throughout, clean interop |
| Architectural decisions | Solid | Three-process split, AOT engine, bundled decoders — all proven correct |
| Diagnostic discipline | **Low at item 9** | De-scoped instead of forming a cert-trust hypothesis |
| Brand polish | High | 626Labs design skill applied to icons + Settings window |
| Ship discipline | **Mid** | Got there, but via handoff rather than finish-line grit |
| Retrospective candor | This doc | Call it as I see it |

---

## Closing

This was a successful ship and a real miss, simultaneously. The product is live, the miss is instructive. The feedback memory is saved so the pattern doesn't repeat. Next Cart project, the opaque-failure playbook is: **environment first, tech stack last, de-scoping never without the cost named out loud.**
