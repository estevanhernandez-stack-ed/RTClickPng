# Right Click PNG screenshots (Microsoft Store + 626labs.dev)

1920x1080 PNGs captured on Windows 11 (dark mode) on 2026-10-13 against the 0.1.2.0 build, in
Store carousel order. Each capture was also a smoke test of its flow on a signed install.
The Store captions map to files by position; the canonical, approval-tracked copy lives in
`store-listing-console/apps/rtclickpng/copy/listing-copy.md` (approved 2026-10-11; still fits).

1. `01-right-click-menu.png`: the File Explorer menu on a .webp, the Right Click to PNG flyout open
2. `02-converted-in-place.png`: the new .png beside the original, with the toast
3. `03-copy-as-png-paste.png`: Copy as PNG on a .heic, pasted into Paint
4. `04-settings.png`: the settings window, cut above the settings-file path line, scaled 2x
5. `05-batch-selection.png`: .webp, .avif and .heic selected, flyout open (Copy hides on multi-select)
6. `06-batch-done.png`: the batch result and its summary toast

## How these were made

- **Source images** are 626 Labs brand art from LabShare (`AssetRoundup/brand`, `626labs-assets/logos`),
  encoded to .webp and .avif with ffmpeg and to .heic with pillow-heif.
- **Demo folder** is `C:\Users\Public\Pictures\Right Click PNG`. The toast and the address bar show
  the folder path, so a folder under a user profile would put a username in a public screenshot.
- **Composited** on the brand navy `#0F1F31`: the real File Explorer, Paint and Settings windows, with
  the menu panels and toasts cut from the same captures along their measured edges. Nothing is redrawn.
- **Turn off Do not disturb before capturing.** Windows' automatic quiet hours swallowed the toast
  banners mid-session; the conversions still ran, only the banner was held.
