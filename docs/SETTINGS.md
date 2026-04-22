# Settings

Open the Settings window from the Start menu tile or via **Right Click PNG → settings…** on
any supported image.  Toggles persist to a JSON file the shell extension reads on every
invocation; hand-editing the file works too and the UI picks up external changes on next open.

## File location

```text
%LOCALAPPDATA%\Packages\626LabsLLC.RightClicktoPNG_wz1chhb2h2v4a\LocalState\settings.json
```

If the folder doesn't exist yet, the UI and the shell extension both create it on first use.
The shell extension uses safe defaults when the file is missing.

## Schema

```json
{
  "schemaVersion": 1,
  "showJpegVariants": false,
  "confirmBeforeOverwrite": true
}
```

| Field | Default | Effect |
| --- | --- | --- |
| `showJpegVariants` | `false` | When `true`, adds *Convert to JPEG* and *Copy as JPEG* to the right-click menu. |
| `confirmBeforeOverwrite` | `true` | When `true`, the extension prompts before replacing an existing file.  When `false`, overwrites silently. |

Changes take effect on the next right-click — no reboot, no restart.
