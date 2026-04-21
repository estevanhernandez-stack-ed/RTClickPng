# Engine Test Fixtures

Minimal test images for round-trip decoder/encoder tests.

## Running

```powershell
pwsh ./fetch-fixtures.ps1           # populate missing fixtures
pwsh ./fetch-fixtures.ps1 -Force    # regenerate / re-fetch all
```

## Inventory

| File | Source | Purpose |
|---|---|---|
| `sample.png`            | generated (hand-rolled encoder)  | 8x8 RGBA gradient; PNG decoder round-trip baseline |
| `sample.bmp`            | generated                        | 8x8 24-bit; BmpDecoder path |
| `sample-animated.gif`   | generated (2-frame GIF89a)       | GifDecoder (first-frame-only semantics) |
| `sample-multipage.tiff` | generated (2-IFD little-endian)  | TiffDecoder (first-page-only semantics) |
| `sample.webp`           | gstatic WebP gallery             | WebpDecoder |
| `sample.avif`           | libavif test data (kodim03)      | AvifDecoder |
| `sample.heic`           | libheif example images           | HeifDecoder |
| `sample-with-icc.jpg`   | color.org ICC profile samples    | ICC-profile preservation test (item 4) |

All upstream samples are redistributed under their respective permissive
licenses. See `docs/third-party-notices.txt` for attribution.
