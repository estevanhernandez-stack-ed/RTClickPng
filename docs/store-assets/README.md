# Store art

Microsoft Store poster art (9:16) and box art (1:1) for Right Click PNG. First made 2026-10-14
for the 0.1.2.0 submission, in the same family as Sanduhr and RoRoRo: brand navy, the app glyph
in a glowing tile, Space Grotesk name, cyan-to-magenta rule, JetBrains Mono meta lines.

| File | Partner Center slot |
|---|---|
| `store-poster-1440x2160.png` | Store logos: 9:16 Poster art |
| `store-poster-720x1080.png` | 9:16 Poster art, standard size |
| `store-boxart-2160x2160.png` | Store logos: 1:1 Box art |
| `store-boxart-1080x1080.png` | 1:1 Box art, standard size |

The same PNGs live in `store-listing-console/apps/rtclickpng/assets/graphics/`.

## Regenerating

`store-art.source.html` is the single source. It picks its layout from the viewport's aspect
ratio (narrower than 85:100 gets the poster layout with the feature list), so every size comes from
one file and they can't drift apart. The glyph is vector SVG mirroring `DrawGlyph` in
`src/Package/Assets/render-brand-icons.ps1`. Fonts are local in `fonts/`, so no network is needed.

Serve over HTTP (headless capture of a `file://` page won't load the fonts), then screenshot
each size with headless Edge:

```powershell
cd docs/store-assets
Start-Process python -ArgumentList '-m','http.server','8741','--bind','127.0.0.1'
$edge = "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe"
foreach ($s in 'poster:1440:2160','poster:720:1080','boxart:2160:2160','boxart:1080:1080') {
  $k, $w, $h = $s -split ':'
  & $edge --headless=new --disable-gpu --hide-scrollbars --force-device-scale-factor=1 `
    --virtual-time-budget=4000 --window-size=$w,$h `
    --screenshot="$PWD\store-$k-${w}x$h.png" "http://127.0.0.1:8741/store-art.source.html"
}
```

Check one export by eye before shipping: a font that fails to load falls back silently and changes
the letterforms. Bump the version in the box-art footer (`v0.1.2`) when the listing's version moves.
