#requires -Version 7.0
<#
.SYNOPSIS
    Generate placeholder MSIX asset PNGs so Package.wapproj has content to reference.
    Item 10 replaces these with proper branded icons (Fluent-aligned, dark-theme-friendly).
#>
[CmdletBinding()]
param([switch]$Force)

$ErrorActionPreference = 'Stop'
$assets = $PSScriptRoot

Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
public static class PngGen {
    private static readonly uint[] Crc;
    static PngGen() {
        Crc = new uint[256];
        for (uint i = 0; i < 256; i++) {
            uint c = i;
            for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            Crc[i] = c;
        }
    }
    private static uint Crc32(byte[] d) {
        uint c = 0xFFFFFFFF;
        for (int i = 0; i < d.Length; i++) c = Crc[(c ^ d[i]) & 0xFF] ^ (c >> 8);
        return c ^ 0xFFFFFFFF;
    }
    private static byte[] Be(uint v) => new byte[] { (byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v };
    private static byte[] Concat(params byte[][] arrs) {
        int n = 0; foreach (var a in arrs) n += a.Length;
        var r = new byte[n]; int p = 0;
        foreach (var a in arrs) { Buffer.BlockCopy(a, 0, r, p, a.Length); p += a.Length; }
        return r;
    }
    private static byte[] Chunk(string type, byte[] data) {
        var t = Encoding.ASCII.GetBytes(type);
        var crc = Crc32(Concat(t, data));
        return Concat(Be((uint)data.Length), t, data, Be(crc));
    }
    public static void Write(string path, int w, int h, byte r, byte g, byte b, byte a) {
        var raw = new MemoryStream();
        for (int y = 0; y < h; y++) {
            raw.WriteByte(0);
            for (int x = 0; x < w; x++) {
                raw.WriteByte(r); raw.WriteByte(g); raw.WriteByte(b); raw.WriteByte(a);
            }
        }
        var rawBytes = raw.ToArray();
        var z = new MemoryStream();
        z.WriteByte(0x78); z.WriteByte(0x9C);
        using (var ds = new DeflateStream(z, CompressionLevel.Optimal, true)) ds.Write(rawBytes, 0, rawBytes.Length);
        uint A = 1, B = 0;
        for (int i = 0; i < rawBytes.Length; i++) { A = (A + rawBytes[i]) % 65521u; B = (B + A) % 65521u; }
        z.Write(new byte[] { (byte)(B >> 8), (byte)B, (byte)(A >> 8), (byte)A }, 0, 4);
        var idat = z.ToArray();
        var sig = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var ihdr = Concat(Be((uint)w), Be((uint)h), new byte[] { 8, 6, 0, 0, 0 });
        File.WriteAllBytes(path, Concat(sig, Chunk("IHDR", ihdr), Chunk("IDAT", idat), Chunk("IEND", new byte[0])));
    }
}
'@ -Language CSharp

$items = @(
    @{ Name='Square44x44Logo.png';  W=44;  H=44 }
    @{ Name='Square150x150Logo.png'; W=150; H=150 }
    @{ Name='Wide310x150Logo.png';   W=310; H=150 }
    @{ Name='Square310x310Logo.png'; W=310; H=310 }
    @{ Name='StoreLogo.png';         W=50;  H=50 }
    @{ Name='SplashScreen.png';      W=620; H=300 }
)

# 626 Labs dark-theme friendly: deep blue #1F3A5F, opaque.
foreach ($item in $items) {
    $p = Join-Path $assets $item.Name
    if ((Test-Path $p) -and -not $Force) { Write-Host "    skip: $($item.Name)"; continue }
    [PngGen]::Write($p, $item.W, $item.H, 0x1F, 0x3A, 0x5F, 0xFF)
    Write-Host "    created: $($item.Name) ($($item.W)x$($item.H))"
}
