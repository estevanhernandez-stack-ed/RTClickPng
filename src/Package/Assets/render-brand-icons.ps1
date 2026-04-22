#requires -Version 7.0
<#
.SYNOPSIS
    Render the Right Click PNG brand icon set at all required MSIX tile sizes.

.DESCRIPTION
    Uses a tiny inline C# / System.Drawing helper (PowerShell + System.Drawing paths get tangled
    on array-vs-scalar inference).  Produces the standard MSIX set plus splash:
        Square44x44Logo.png    44x44
        StoreLogo.png          50x50
        Square150x150Logo.png  150x150
        Square310x310Logo.png  310x310
        Wide310x150Logo.png    310x150   (glyph-left + wordmark-right composition)
        SplashScreen.png       620x300   (same wide composition)

    Brand tokens aligned with the 626Labs design skill:
      bg:           #0F1F31 (brand-navy-deep)
      cyan duo:     #17D4FA
      magenta duo:  #F22F89
      text primary: #E6EBF3
      eyebrow:      #8E9BAD
#>
[CmdletBinding()]
param([switch]$Force)

$ErrorActionPreference = 'Stop'
$assets = $PSScriptRoot

Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;

public static class RtClickIcon
{
    static readonly Color Bg      = Color.FromArgb(255, 0x0F, 0x1F, 0x31);
    static readonly Color Cyan    = Color.FromArgb(255, 0x17, 0xD4, 0xFA);
    static readonly Color Magenta = Color.FromArgb(255, 0xF2, 0x2F, 0x89);
    static readonly Color TextPri = Color.FromArgb(255, 0xE6, 0xEB, 0xF3);
    static readonly Color Eyebrow = Color.FromArgb(180, 0x8E, 0x9B, 0xAD);
    static readonly Color DocBody = Color.FromArgb(220, 0x19, 0x2E, 0x44);

    public static void RenderSquare(int size, string path, bool showEyebrow)
    {
        using var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.Clear(Bg);
        DrawGlow(g, size, size);
        DrawGlyph(g, size, size);
        if (showEyebrow && size >= 150) DrawEyebrow(g, size, size);
        bmp.Save(path, ImageFormat.Png);
    }

    public static void RenderWide(int w, int h, string path)
    {
        using var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.Clear(Bg);
        DrawGlowWide(g, w, h);

        // Glyph inset on the left — 85% of height, margin on all sides.
        float glyphSize = h * 0.85f;
        float glyphX = h * 0.08f;
        float glyphY = (h - glyphSize) / 2f;
        var prevClip = g.Clip;
        var state = g.Save();
        g.TranslateTransform(glyphX, glyphY);
        DrawGlyph(g, (int)glyphSize, (int)glyphSize);
        g.Restore(state);

        // Wordmark on the right.
        float wmSize = h * 0.23f;
        using var wmFont  = new Font("Segoe UI Variable Display", wmSize, FontStyle.Regular, GraphicsUnit.Pixel);
        using var wmFont2 = new Font("Segoe UI Variable Display", wmSize, FontStyle.Bold,    GraphicsUnit.Pixel);
        using var wmBrush = new SolidBrush(TextPri);

        const string line1 = "Right Click";
        const string line2 = "PNG";
        var line1Size = g.MeasureString(line1, wmFont);
        var wmX = glyphX + glyphSize + h * 0.10f;
        var totalH = line1Size.Height * 2f - line1Size.Height * 0.2f;
        var wmY = (h - totalH) / 2f;

        g.DrawString(line1, wmFont, wmBrush, wmX, wmY);

        var line2Y = wmY + line1Size.Height * 0.9f;
        using var grad = new LinearGradientBrush(
            new PointF(wmX, line2Y),
            new PointF(wmX + 260f, line2Y + wmSize),
            Cyan, Magenta);
        g.DrawString(line2, wmFont2, grad, wmX, line2Y);

        bmp.Save(path, ImageFormat.Png);
    }

    static void DrawGlow(Graphics g, int w, int h)
    {
        using var p1 = new GraphicsPath();
        p1.AddEllipse(-w * 0.3f, -h * 0.3f, w * 0.9f, h * 0.9f);
        using var b1 = new PathGradientBrush(p1) {
            CenterColor = Color.FromArgb(70, Cyan),
            SurroundColors = new[] { Color.FromArgb(0, Cyan) }
        };
        g.FillEllipse(b1, -w * 0.3f, -h * 0.3f, w * 0.9f, h * 0.9f);

        using var p2 = new GraphicsPath();
        p2.AddEllipse(w * 0.4f, h * 0.4f, w * 0.9f, h * 0.9f);
        using var b2 = new PathGradientBrush(p2) {
            CenterColor = Color.FromArgb(50, Magenta),
            SurroundColors = new[] { Color.FromArgb(0, Magenta) }
        };
        g.FillEllipse(b2, w * 0.4f, h * 0.4f, w * 0.9f, h * 0.9f);
    }

    static void DrawGlowWide(Graphics g, int w, int h)
    {
        using var p1 = new GraphicsPath();
        p1.AddEllipse(-h * 0.3f, -h * 0.3f, h * 1.1f, h * 1.1f);
        using var b1 = new PathGradientBrush(p1) {
            CenterColor = Color.FromArgb(50, Cyan),
            SurroundColors = new[] { Color.FromArgb(0, Cyan) }
        };
        g.FillEllipse(b1, -h * 0.3f, -h * 0.3f, h * 1.1f, h * 1.1f);
    }

    static void DrawGlyph(Graphics g, int w, int h)
    {
        float cx = w / 2f, cy = h / 2f;
        float scale = Math.Min(w, h) * 0.48f;
        float docW = scale * 0.82f;
        float docH = scale * 1.0f;
        float fold = docW * 0.32f;

        // Nudge document up-left so the cursor has room bottom-right.
        float dx = cx - docW * 0.55f;
        float dy = cy - docH * 0.55f;

        using var docPath = new GraphicsPath();
        docPath.AddLines(new[] {
            new PointF(dx, dy),
            new PointF(dx + docW - fold, dy),
            new PointF(dx + docW, dy + fold),
            new PointF(dx + docW, dy + docH),
            new PointF(dx, dy + docH)
        });
        docPath.CloseFigure();

        using var docBrush = new SolidBrush(DocBody);
        g.FillPath(docBrush, docPath);

        using var cyanPen = new Pen(Cyan, Math.Max(1.5f, scale * 0.05f)) {
            LineJoin = LineJoin.Round,
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        g.DrawPath(cyanPen, docPath);

        // Magenta fold triangle.
        using var foldPath = new GraphicsPath();
        foldPath.AddLines(new[] {
            new PointF(dx + docW - fold, dy),
            new PointF(dx + docW - fold, dy + fold),
            new PointF(dx + docW,        dy + fold)
        });
        foldPath.CloseFigure();
        using var magentaBrush = new SolidBrush(Magenta);
        g.FillPath(magentaBrush, foldPath);

        // "PNG" inside the document — only when the tile is big enough.
        if (w >= 50)
        {
            float pngSize = scale * 0.30f;
            using var pngFont = new Font("Segoe UI", pngSize, FontStyle.Bold, GraphicsUnit.Pixel);
            using var pngBrush = new SolidBrush(Cyan);
            const string png = "PNG";
            var sz = g.MeasureString(png, pngFont);
            float px = dx + (docW - sz.Width) / 2f;
            float py = dy + docH * 0.48f - sz.Height / 2f;
            g.DrawString(png, pngFont, pngBrush, px, py);
        }

        // Cursor arrow (cyan, navy outline) bottom-right of document.
        float curX = cx + scale * 0.22f;
        float curY = cy + scale * 0.22f;
        float curS = scale * 0.58f;
        using var cur = new GraphicsPath();
        cur.AddLines(new[] {
            new PointF(curX,                 curY),
            new PointF(curX,                 curY + curS * 1.00f),
            new PointF(curX + curS * 0.28f,  curY + curS * 0.72f),
            new PointF(curX + curS * 0.50f,  curY + curS * 1.10f),
            new PointF(curX + curS * 0.64f,  curY + curS * 1.04f),
            new PointF(curX + curS * 0.42f,  curY + curS * 0.66f),
            new PointF(curX + curS * 0.72f,  curY + curS * 0.60f)
        });
        cur.CloseFigure();
        using var cursorFill = new SolidBrush(Cyan);
        g.FillPath(cursorFill, cur);
        using var cursorStroke = new Pen(Bg, Math.Max(1.0f, scale * 0.04f)) { LineJoin = LineJoin.Round };
        g.DrawPath(cursorStroke, cur);
    }

    static void DrawEyebrow(Graphics g, int w, int h)
    {
        float sz = h * 0.055f;
        using var font = new Font("Cascadia Mono", sz, FontStyle.Bold, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(Eyebrow);
        const string eb = "RIGHT CLICK PNG";
        var m = g.MeasureString(eb, font);
        g.DrawString(eb, font, brush, (w - m.Width) / 2f, h - m.Height - h * 0.05f);
    }
}
'@ -ReferencedAssemblies System.Drawing, System.Drawing.Common, System.Drawing.Primitives

$items = @(
    @{ Name='Square44x44Logo.png';   W=44;  H=44;  Kind='square' }
    @{ Name='StoreLogo.png';         W=50;  H=50;  Kind='square' }
    @{ Name='Square150x150Logo.png'; W=150; H=150; Kind='square' }
    @{ Name='Square310x310Logo.png'; W=310; H=310; Kind='square' }
    @{ Name='Wide310x150Logo.png';   W=310; H=150; Kind='wide' }
    @{ Name='SplashScreen.png';      W=620; H=300; Kind='wide' }
)

foreach ($i in $items) {
    $p = Join-Path $assets $i.Name
    if ((Test-Path $p) -and -not $Force) { Write-Host "    skip: $($i.Name)"; continue }
    if ($i.Kind -eq 'wide') {
        [RtClickIcon]::RenderWide($i.W, $i.H, $p)
    } else {
        $eyebrow = ($i.W -ge 150)
        [RtClickIcon]::RenderSquare($i.W, $p, $eyebrow)
    }
    $bytes = (Get-Item $p).Length
    Write-Host "    created: $($i.Name) ($($i.W)x$($i.H), $([int]($bytes/1024)) KB)"
}

Write-Host ""
Write-Host "==> icon set ready at $assets"
