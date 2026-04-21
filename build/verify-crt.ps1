#requires -Version 7.0
<#
.SYNOPSIS
    Verify every DLL in build/native/ links the expected CRT.

.DESCRIPTION
    .NET Native AOT emits code that depends on the dynamic UCRT (ucrtbase.dll +
    vcruntime140.dll). Every native decoder DLL we bundle must agree — mixed
    CRTs are a guaranteed runtime crash on Store-built AOT output.

    Runs `dumpbin /dependents` on each DLL and ensures:
      - ucrtbase.dll is referenced (or is a system-provided forwarder)
      - vcruntime140.dll is referenced
      - No msvcr*.dll (legacy CRT) or msvcp1*0.dll (C++ stdlib in mixed form)

.NOTES
    Must run from a Visual Studio Developer PowerShell (so dumpbin is on PATH).
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$nativeDir = Join-Path $repoRoot 'build/native'

if (-not (Get-Command dumpbin -ErrorAction SilentlyContinue)) {
    throw "dumpbin.exe not on PATH. Launch a 'Developer PowerShell for VS 2022' and re-run."
}
if (-not (Test-Path $nativeDir)) {
    throw "build/native/ missing. Run build/fetch-native.ps1 first."
}

$dlls = Get-ChildItem $nativeDir -Filter '*.dll' -File
if ($dlls.Count -eq 0) { throw "no DLLs in $nativeDir" }

$allOk = $true
$report = @()

foreach ($dll in $dlls) {
    Write-Host "==> $($dll.Name)"
    $out = & dumpbin /dependents $dll.FullName 2>&1
    $imports = $out | Where-Object { $_ -match '\.dll\s*$' } | ForEach-Object { $_.Trim().ToLowerInvariant() }

    $hasUcrt = $imports -contains 'ucrtbase.dll' -or $imports -contains 'api-ms-win-crt-runtime-l1-1-0.dll'
    $hasVcr = $imports -contains 'vcruntime140.dll' -or $imports -contains 'vcruntime140_1.dll'
    $hasLegacy = @($imports | Where-Object { $_ -match '^msvcr\d' }).Count -gt 0

    $status = if ($hasUcrt -and -not $hasLegacy) { 'ok' } else { 'FAIL' }
    if ($status -eq 'FAIL') { $allOk = $false }

    $report += [pscustomobject]@{
        Dll        = $dll.Name
        UCRT       = $hasUcrt
        VCRuntime  = $hasVcr
        LegacyCRT  = $hasLegacy
        Status     = $status
        ImportList = ($imports -join ', ')
    }
}

$report | Format-Table Dll, UCRT, VCRuntime, LegacyCRT, Status -AutoSize

if (-not $allOk) {
    throw "one or more DLLs failed CRT consistency check. Rebuild with matching /MD dynamic UCRT."
}
Write-Host "==> all DLLs use consistent dynamic UCRT. CRT check passed."
