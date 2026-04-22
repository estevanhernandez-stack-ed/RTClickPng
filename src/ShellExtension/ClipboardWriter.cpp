#include "pch.h"
#include "ClipboardWriter.h"
#include <wincodec.h>
#include <wrl/client.h>

#pragma comment(lib, "windowscodecs.lib")

namespace rtclick {

using Microsoft::WRL::ComPtr;

namespace {
    struct DecodedBitmap
    {
        uint32_t width{0};
        uint32_t height{0};
        std::vector<std::byte> bgraPremul;   // CF_DIBV5 path (premultiplied alpha)
        std::vector<std::byte> bgrFlattened; // CF_DIB path (flattened over white)
        std::vector<std::byte> icc;          // optional ICC profile from PNG iCCP chunk
    };

    /// <summary>
    /// Decode PNG bytes to a DecodedBitmap via WIC.  All paths use BGRA 32bpp as the working buffer
    /// and derive both the premultiplied-for-DIBV5 and flattened-for-DIB variants from the same decode.
    /// </summary>
    HRESULT DecodePngWithWIC(const std::vector<std::byte>& pngBytes, DecodedBitmap& out) noexcept
    {
        ComPtr<IWICImagingFactory> factory;
        auto hr = CoCreateInstance(CLSID_WICImagingFactory, nullptr, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&factory));
        if (FAILED(hr)) return hr;

        ComPtr<IWICStream> stream;
        if (FAILED(hr = factory->CreateStream(&stream))) return hr;
        if (FAILED(hr = stream->InitializeFromMemory(
                reinterpret_cast<BYTE*>(const_cast<std::byte*>(pngBytes.data())),
                static_cast<DWORD>(pngBytes.size())))) return hr;

        ComPtr<IWICBitmapDecoder> decoder;
        if (FAILED(hr = factory->CreateDecoderFromStream(stream.Get(), nullptr,
                WICDecodeMetadataCacheOnLoad, &decoder))) return hr;

        ComPtr<IWICBitmapFrameDecode> frame;
        if (FAILED(hr = decoder->GetFrame(0, &frame))) return hr;

        UINT w = 0, h = 0;
        if (FAILED(hr = frame->GetSize(&w, &h))) return hr;
        out.width = w;
        out.height = h;

        // Convert to 32bpp BGRA.
        ComPtr<IWICFormatConverter> conv;
        if (FAILED(hr = factory->CreateFormatConverter(&conv))) return hr;
        if (FAILED(hr = conv->Initialize(frame.Get(), GUID_WICPixelFormat32bppBGRA,
                WICBitmapDitherTypeNone, nullptr, 0.0, WICBitmapPaletteTypeCustom))) return hr;

        const UINT stride = w * 4;
        std::vector<std::byte> bgra(static_cast<size_t>(stride) * h);
        if (FAILED(hr = conv->CopyPixels(nullptr, stride, static_cast<UINT>(bgra.size()),
                reinterpret_cast<BYTE*>(bgra.data())))) return hr;

        // Build premultiplied copy (B' = B*A/255 etc.) for CF_DIBV5 with PREMULTIPLIED alpha flag.
        out.bgraPremul = bgra;  // start with straight copy
        for (size_t i = 0; i < out.bgraPremul.size(); i += 4)
        {
            auto a = static_cast<uint8_t>(out.bgraPremul[i + 3]);
            if (a == 255) continue;
            out.bgraPremul[i + 0] = std::byte{static_cast<uint8_t>((static_cast<uint16_t>(std::to_integer<uint8_t>(out.bgraPremul[i + 0])) * a + 127) / 255)};
            out.bgraPremul[i + 1] = std::byte{static_cast<uint8_t>((static_cast<uint16_t>(std::to_integer<uint8_t>(out.bgraPremul[i + 1])) * a + 127) / 255)};
            out.bgraPremul[i + 2] = std::byte{static_cast<uint8_t>((static_cast<uint16_t>(std::to_integer<uint8_t>(out.bgraPremul[i + 2])) * a + 127) / 255)};
        }

        // Build flattened-BGR copy for CF_DIB (alpha composited over white).  Row stride padded to 4.
        const UINT bgrRowBytes = ((w * 3 + 3) / 4) * 4;
        out.bgrFlattened.assign(static_cast<size_t>(bgrRowBytes) * h, std::byte{0});
        for (UINT y = 0; y < h; ++y)
        {
            const auto* srcRow = bgra.data() + static_cast<size_t>(y) * stride;
            auto* dstRow = out.bgrFlattened.data() + static_cast<size_t>(y) * bgrRowBytes;
            for (UINT x = 0; x < w; ++x)
            {
                auto b = std::to_integer<uint8_t>(srcRow[x * 4 + 0]);
                auto g = std::to_integer<uint8_t>(srcRow[x * 4 + 1]);
                auto rr = std::to_integer<uint8_t>(srcRow[x * 4 + 2]);
                auto a = std::to_integer<uint8_t>(srcRow[x * 4 + 3]);
                if (a == 255)
                {
                    dstRow[x * 3 + 0] = std::byte{b};
                    dstRow[x * 3 + 1] = std::byte{g};
                    dstRow[x * 3 + 2] = std::byte{rr};
                }
                else
                {
                    // over white: out = src*a + 255*(255-a), /255
                    auto inv = 255 - a;
                    dstRow[x * 3 + 0] = std::byte{static_cast<uint8_t>((b  * a + 255 * inv) / 255)};
                    dstRow[x * 3 + 1] = std::byte{static_cast<uint8_t>((g  * a + 255 * inv) / 255)};
                    dstRow[x * 3 + 2] = std::byte{static_cast<uint8_t>((rr * a + 255 * inv) / 255)};
                }
            }
        }

        // Attempt to extract ICC from the PNG (iCCP chunk).  Non-fatal if absent.
        ComPtr<IWICColorContext> iccCtx;
        if (SUCCEEDED(factory->CreateColorContext(&iccCtx)))
        {
            IWICColorContext* ctxPtr = iccCtx.Get();
            UINT ctxCount = 0;
            if (SUCCEEDED(frame->GetColorContexts(1, &ctxPtr, &ctxCount)) && ctxCount > 0)
            {
                UINT bufLen = 0;
                if (SUCCEEDED(iccCtx->GetProfileBytes(0, nullptr, &bufLen)) && bufLen > 0)
                {
                    out.icc.resize(bufLen);
                    if (FAILED(iccCtx->GetProfileBytes(bufLen, reinterpret_cast<BYTE*>(out.icc.data()), &bufLen)))
                        out.icc.clear();
                }
            }
        }
        return S_OK;
    }

    /// <summary>
    /// Allocate an HGLOBAL of the given size, populate it via <paramref name="fill"/>, and return
    /// it for SetClipboardData.  On failure returns nullptr and the caller skips that format.
    /// </summary>
    template <typename FillFn>
    HGLOBAL AllocAndFill(size_t bytes, FillFn&& fill) noexcept
    {
        HGLOBAL h = GlobalAlloc(GMEM_MOVEABLE, bytes);
        if (!h) return nullptr;
        auto* p = GlobalLock(h);
        if (!p) { GlobalFree(h); return nullptr; }
        fill(p);
        GlobalUnlock(h);
        return h;
    }
}

bool ClipboardWriter::SetClipboardFromPng(const std::vector<std::byte>& pngBytes) noexcept
{
    if (pngBytes.empty()) return false;

    DecodedBitmap bmp{};
    if (FAILED(DecodePngWithWIC(pngBytes, bmp))) return false;
    if (bmp.width == 0 || bmp.height == 0) return false;

    // CF_DIBV5 payload: BITMAPV5HEADER + (optional ICC bytes) + BGRA premultiplied pixels.
    const DWORD pxBytesV5 = static_cast<DWORD>(bmp.width) * bmp.height * 4;
    const DWORD iccBytes  = static_cast<DWORD>(bmp.icc.size());
    HGLOBAL hDibV5 = AllocAndFill(sizeof(BITMAPV5HEADER) + iccBytes + pxBytesV5, [&](void* p) {
        auto* h = static_cast<BITMAPV5HEADER*>(p);
        *h = {};
        h->bV5Size          = sizeof(BITMAPV5HEADER);
        h->bV5Width         = static_cast<LONG>(bmp.width);
        h->bV5Height        = -static_cast<LONG>(bmp.height);  // top-down
        h->bV5Planes        = 1;
        h->bV5BitCount      = 32;
        h->bV5Compression   = BI_BITFIELDS;
        h->bV5RedMask       = 0x00FF0000;
        h->bV5GreenMask     = 0x0000FF00;
        h->bV5BlueMask      = 0x000000FF;
        h->bV5AlphaMask     = 0xFF000000;
        h->bV5SizeImage     = pxBytesV5;
        h->bV5AlphaMask     = 0xFF000000;
        if (iccBytes > 0)
        {
            h->bV5CSType    = PROFILE_EMBEDDED;
            h->bV5Intent    = LCS_GM_IMAGES;
            h->bV5ProfileData = sizeof(BITMAPV5HEADER);
            h->bV5ProfileSize = iccBytes;
        }
        else
        {
            h->bV5CSType    = LCS_sRGB;
            h->bV5Intent    = LCS_GM_IMAGES;
        }
        auto* bytes = reinterpret_cast<uint8_t*>(h) + sizeof(BITMAPV5HEADER);
        if (iccBytes > 0)
        {
            std::memcpy(bytes, bmp.icc.data(), iccBytes);
            bytes += iccBytes;
        }
        // CF_DIBV5 expects bottom-up OR top-down via negative height; we set negative so copy as-is.
        std::memcpy(bytes, bmp.bgraPremul.data(), pxBytesV5);
    });

    // CF_DIB payload: BITMAPINFOHEADER + BGR rows (4-byte aligned).
    const DWORD bgrRowBytes = ((bmp.width * 3 + 3) / 4) * 4;
    const DWORD dibPxBytes = bgrRowBytes * bmp.height;
    HGLOBAL hDib = AllocAndFill(sizeof(BITMAPINFOHEADER) + dibPxBytes, [&](void* p) {
        auto* h = static_cast<BITMAPINFOHEADER*>(p);
        *h = {};
        h->biSize         = sizeof(BITMAPINFOHEADER);
        h->biWidth        = static_cast<LONG>(bmp.width);
        h->biHeight       = -static_cast<LONG>(bmp.height); // top-down
        h->biPlanes       = 1;
        h->biBitCount     = 24;
        h->biCompression  = BI_RGB;
        h->biSizeImage    = dibPxBytes;
        std::memcpy(reinterpret_cast<uint8_t*>(h) + sizeof(BITMAPINFOHEADER),
                    bmp.bgrFlattened.data(), dibPxBytes);
    });

    // CF_PNG payload: raw PNG bytes verbatim.
    HGLOBAL hPng = AllocAndFill(pngBytes.size(), [&](void* p) {
        std::memcpy(p, pngBytes.data(), pngBytes.size());
    });

    UINT cfPng = RegisterClipboardFormatW(L"PNG");
    if (cfPng == 0) { /* should never happen, but continue with the other two */ }

    if (!OpenClipboard(nullptr))
    {
        if (hDibV5) GlobalFree(hDibV5);
        if (hDib)   GlobalFree(hDib);
        if (hPng)   GlobalFree(hPng);
        return false;
    }
    EmptyClipboard();

    bool anyOk = false;
    if (hDibV5)
    {
        if (SetClipboardData(CF_DIBV5, hDibV5)) anyOk = true;
        else GlobalFree(hDibV5);
    }
    if (hDib)
    {
        if (SetClipboardData(CF_DIB, hDib)) anyOk = true;
        else GlobalFree(hDib);
    }
    if (hPng && cfPng != 0)
    {
        if (SetClipboardData(cfPng, hPng)) anyOk = true;
        else GlobalFree(hPng);
    }

    CloseClipboard();
    return anyOk;
}

} // namespace rtclick
