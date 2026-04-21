#requires -Version 7.0
<#
.SYNOPSIS
    Fetch x64 Windows native decoder DLLs for Right Click PNG.

.DESCRIPTION
    Bootstraps vcpkg into build/vcpkg/ if absent, reads the repo-root vcpkg.json
    manifest (libwebp, libavif, libheif, libspng, libjpeg-turbo), builds the
    x64-windows triplet (dynamic CRT / /MD), then copies every .dll from the
    vcpkg installed tree into build/native/.

    Running this end-to-end from a cold machine takes 30-60 minutes the first
    time (libheif pulls in libde265 + x265 + aom/dav1d). Subsequent runs are
    near-instant if build/vcpkg/ is retained.

.PARAMETER Clean
    Remove build/native/ before copying.
#>
[CmdletBinding()]
param(
    [switch]$Clean
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$vcpkgDir = Join-Path $repoRoot 'build/vcpkg'
$nativeDir = Join-Path $repoRoot 'build/native'

Write-Host "==> repo root: $repoRoot"
Write-Host "==> vcpkg dir: $vcpkgDir"
Write-Host "==> native dir: $nativeDir"

if (-not (Test-Path $vcpkgDir)) {
    Write-Host "==> cloning vcpkg..."
    git clone --depth 1 https://github.com/microsoft/vcpkg.git $vcpkgDir
    Write-Host "==> bootstrapping vcpkg..."
    & (Join-Path $vcpkgDir 'bootstrap-vcpkg.bat') -disableMetrics
    if ($LASTEXITCODE -ne 0) { throw "vcpkg bootstrap failed" }
} else {
    Write-Host "==> vcpkg already present, skipping bootstrap"
}

$vcpkgExe = Join-Path $vcpkgDir 'vcpkg.exe'
if (-not (Test-Path $vcpkgExe)) { throw "vcpkg.exe not found at $vcpkgExe" }

Push-Location $repoRoot
try {
    Write-Host "==> installing decoder libraries (x64-windows)..."
    & $vcpkgExe install --triplet x64-windows --x-manifest-root=$repoRoot
    if ($LASTEXITCODE -ne 0) { throw "vcpkg install failed" }
} finally {
    Pop-Location
}

if ($Clean -and (Test-Path $nativeDir)) {
    Write-Host "==> cleaning $nativeDir"
    Get-ChildItem $nativeDir -Include '*.dll','*.pdb' -Recurse | Remove-Item -Force
}

if (-not (Test-Path $nativeDir)) { New-Item -ItemType Directory -Path $nativeDir | Out-Null }

$binDir = Join-Path $vcpkgDir 'installed/x64-windows/bin'
if (-not (Test-Path $binDir)) { throw "expected vcpkg bin dir missing: $binDir" }

Write-Host "==> copying DLLs from $binDir to $nativeDir"
Get-ChildItem $binDir -Filter '*.dll' | ForEach-Object {
    Copy-Item $_.FullName (Join-Path $nativeDir $_.Name) -Force
    Write-Host "    $($_.Name)"
}

# Sanity check: all five top-level DLLs present.
$expected = @('libwebp.dll', 'avif.dll', 'heif.dll', 'spng.dll', 'turbojpeg.dll')
$missing = @()
foreach ($name in $expected) {
    $primary = Join-Path $nativeDir $name
    if (Test-Path $primary) { continue }
    # Some distros ship with alternate naming (webp.dll, libavif.dll). Accept either.
    $alt = Get-ChildItem $nativeDir -Filter "*$($name -replace '^lib|\.dll$','')*" -File | Select-Object -First 1
    if (-not $alt) { $missing += $name }
}
if ($missing.Count -gt 0) {
    Write-Warning "missing expected DLLs (check vcpkg naming conventions): $($missing -join ', ')"
}

Write-Host ""
Write-Host "==> done. Native DLLs in build/native/:"
Get-ChildItem $nativeDir -Filter '*.dll' | Format-Table Name, Length
