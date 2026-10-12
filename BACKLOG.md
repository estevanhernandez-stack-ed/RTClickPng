# Backlog

_Empty. Open product work is tracked on the 626 Labs dashboard and in GitHub issues._

## Resolved

- **#1, crash-log location (2026-10-11).** The real issue was larger than the backlog note: the
  "crash log" was a startup trace written on every Settings launch, to the raw `LocalAppData`
  root, outside the package folder. It now logs only failures, to `LocalState\settings-crash.log`
  beside `settings.json`, so uninstall removes it. Settings deletes the legacy file on first run.
  Disclosure updated in `PRIVACY.md`, `README.md`, and `626labs.dev/privacy.html`.
