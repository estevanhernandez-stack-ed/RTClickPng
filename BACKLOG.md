# Backlog

## #1 — NEXT STORE PUSH: resolve the crash-log location (not just disclose it)

`Settings/App.xaml.cs:30-42` writes `%LOCALAPPDATA%\RTClickPng.Settings.crash.log` — the raw
`LocalAppData` root, not the package-scoped `Packages\626LabsLLC.RightClicktoPNG_wz1chhb2h2v4a\LocalState\`
folder `settings.json` uses. As of 2026-08-03 this is disclosed in `PRIVACY.md` and `README.md`,
but the underlying behavior is still open:

- Run a real install/uninstall cycle to confirm whether MSIX redirection sandboxes this path,
  or whether the file survives uninstall as a stray file outside the package folder.
- If it survives, move the crash log into the package-scoped `LocalState` folder alongside
  `settings.json` so uninstall removes it automatically — closing the gap instead of just
  disclosing it.
- On ship, update `PRIVACY.md`, the `## Privacy` section of `README.md`, and the matching
  `privacy-rtclickpng` section on `626labs.dev/privacy.html`.

Source: `.superpowers/sdd/privacy-findings-2026-08-03.md` (626labs-hub repo), NEEDS-ESTE item 3.
