# Settings

v1 ships without an in-app settings window.  The toggles live in a JSON file that the shell
extension reads on every invocation — edit it directly.

## File location

```
%LOCALAPPDATA%\Packages\626labs.RTClickPng_3fjztnatnmz7a\LocalState\settings.json
```

If the folder doesn't exist yet, create it.  The shell extension uses safe defaults when the
file is missing.

## Schema

```json
{
  "schemaVersion": 1,
  "showJpegVariants": false,
  "confirmBeforeOverwrite": true
}
```

| Field | Default | Effect |
|---|---|---|
| `showJpegVariants` | `false` | When `true`, adds *Convert to JPEG* and *Copy as JPEG* to the right-click menu. |
| `confirmBeforeOverwrite` | `true` | When `true`, the extension prompts before replacing an existing file.  When `false`, overwrites silently. |

Changes take effect on the next right-click — no reboot, no restart.

## Why not a UI?

A WinUI 3 settings window is staged at `src/Settings/` and will ship in v1.1.  The current
Windows App SDK 1.6 packaged build silent-exits before .NET starts on our build config, and
we didn't want to block v1 on a cosmetic feature.  The shell extension does all its real work
without it.
