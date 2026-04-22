# Changelog

All notable changes to Right Click PNG. Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Versioning: [SemVer](https://semver.org/).

## [0.1.1.0] — 2026-04-22

First public preview. Feature-complete for the v1 scope.

The 4-part MSIX Identity.Version reflects Microsoft Store submission mechanics,
not product change — 0.1.0.0 was consumed in the submission queue during
DisplayName-reservation + uniqueness troubleshooting, and 0.1.0.1 failed the
Store policy that requires a revision digit of 0 (`X.Y.Z.0`). 0.1.1.0 is the
first build that actually ships.

### Added

- **Convert to PNG** right-click verb on `.webp`, `.avif`, `.heic`, `.heif`,
  `.jpg`, `.jpeg`, `.bmp`, `.tif`, `.tiff`, `.gif` (first frame). Writes a
  `.png` next to the source file; original is never modified.
- **Convert to JPEG** right-click verb (opt-in via Settings) on the same
  formats plus `.png`. Same file-next-to-source behavior.
- **Copy as PNG** — puts PNG-encoded bytes on the clipboard in `CF_DIB`,
  `CF_DIBV5`, and raw `PNG` formats. Pastes into Teams, Slack, Discord,
  Figma, Photoshop, Outlook, Word, PowerPoint, OneNote, Paint, Snipping Tool.
- **Copy as JPEG** (opt-in) — same clipboard flow.
- **Batch selection** — multi-file right-click runs the verb against every
  supported file in the selection, with a single summary toast on completion.
- **Windows toast notifications** confirm each operation; failures produce
  a separate error toast.
- **Branded WPF Settings window** launched from the Start menu tile or via
  `Right Click PNG → settings…` verb on any supported image. Dark title bar,
  two toggles: *Show JPEG variants*, *Confirm before overwriting*.
- **ICC color profile preservation** when the source has one and the target
  clipboard/file format supports it.
- **EXIF metadata strip** with orientation applied to pixel data so rotation
  isn't lost.

### Product commitments

- **Offline-only.** No network calls of any kind. No telemetry, no crash
  reports, no update checks, no analytics.
- **No account.** Install and use, that's the entire surface.
- **Original files never modified.** Every verb produces a *new* file or a
  clipboard payload.

### Limitations

- **x64 only.** ARM64 native binaries deferred to a later release; x64 runs
  under emulation on ARM64 Windows with a performance hit.
- **GIF is first-frame only.** No animated-PNG output.
- **No pre-set size cap on source files.** Very large sources (multi-hundred
  MB) will consume corresponding memory during decode.

### Known issues

- Sideload installation requires Windows 11 Developer Mode and
  `Add-AppxPackage -AllowUnsigned`. Microsoft Store install will be the
  preferred path once certification passes.

## Unreleased

- Microsoft Store submission in flight. Certification outcome will be noted
  here when it lands.
