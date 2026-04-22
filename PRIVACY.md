# Privacy Policy

**Effective date:** 2026-04-22

## Short version

Right Click PNG collects no data, makes no network calls, and has no account
system. What you do with the app stays on your computer.

## What the app does with your files

When you right-click a supported image and pick **Convert to PNG**, **Copy as PNG**,
**Convert to JPEG**, or **Copy as JPEG**, the engine reads the source file you
selected, decodes it into raw pixels in memory, and either:

- writes a new `.png` / `.jpeg` file next to the source (Convert flows), or
- places encoded bytes on the Windows clipboard (Copy flows).

The original file is never modified, moved, or deleted.

## What the app stores on your computer

- **Settings JSON** at `%LOCALAPPDATA%\Packages\626LabsLLC.RightClicktoPNG_wz1chhb2h2v4a\LocalState\settings.json`.
  Contains two boolean toggles (show JPEG variants, confirm before overwrite) and
  a schema version. Nothing else. Deleted on uninstall.
- **Windows Toast notification history** that Windows itself keeps — each
  Convert / Copy produces a confirmation toast. Managed by Windows, not by us.
  Toasts never contain identifiable user content beyond the file path you
  acted on.

That's the complete list.

## What the app does NOT do

- No network requests of any kind. No telemetry, no crash reports, no analytics,
  no update checks, no A/B test bucketing, no "anonymous usage stats," no phone-home.
- No account, no cloud sync, no login.
- No uploading of images to any server.
- No sharing of file paths, file names, file contents, or any derived metadata
  with anyone.
- No background processes that run when you're not using the app.
- No access to any files other than the ones you explicitly right-click.

## Third-party processors

None. The app has no cloud backend, no SaaS partners, no tracking SDKs.

Windows itself handles: MSIX package installation / updates / uninstall,
toast notification display, clipboard transfer, File Explorer integration.
Those are OS services, governed by Microsoft's privacy policy, not ours.

## Children's privacy

The app doesn't collect data from anyone, including children.

## Changes to this policy

If the app ever starts doing something that affects privacy (e.g., adding
opt-in update checks), the change will be announced in the CHANGELOG, in the
release notes for the version that introduces it, and a revision of this
document. We won't change privacy behavior silently.

## Contact

`estevan.hernandez@gmail.com` — questions, concerns, or requests.
