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
    If dumpbin isn't on PATH, auto-enters the VS 2022 Developer PowerShell via
    vswhere.exe + Microsoft.VisualStudio.DevShell — no manual Dev PS launch needed.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$nativeDir = Join-Path $repoRoot 'build/native'

# Auto-enter VS Developer Shell if dumpbin isn't already on PATH.
if (-not (Get-Command dumpbin -ErrorAction SilentlyContinue)) {
    $vswhere = 'C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe'
    if (-not (Test-Path $vswhere)) {
        throw "dumpbin not on PATH and vswhere.exe not found. Install VS 2022 Build Tools or launch a Developer PowerShell manually."
    }
    $vsInstallDir = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
    if (-not $vsInstallDir) {
        throw "vswhere could not locate a VS 2022 installation with MSBuild component."
    }
    $devShellDll = Join-Path $vsInstallDir 'Common7\Tools\Microsoft.VisualStudio.DevShell.dll'
    if (-not (Test-Path $devShellDll)) {
        throw "Microsoft.VisualStudio.DevShell.dll missing at $devShellDll — VS 2022 install may be incomplete."
    }
    Import-Module $devShellDll
    Enter-VsDevShell -VsInstallPath $vsInstallDir -SkipAutomaticLocation -DevCmdArguments "-arch=x64 -host_arch=x64" | Out-Null
}

if (-not (Get-Command dumpbin -ErrorAction SilentlyContinue)) {
    throw "dumpbin still not on PATH after Enter-VsDevShell — something went wrong."
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

    $hasUcrt = ($imports -contains 'ucrtbase.dll') -or ($imports | Where-Object { $_ -like 'api-ms-win-crt-*.dll' })
    $hasVcr  = ($imports -contains 'vcruntime140.dll') -or ($imports -contains 'vcruntime140_1.dll')
    $legacy  = @($imports | Where-Object { $_ -match '^msvcr\d' })
    $status  = if (($hasUcrt) -and ($legacy.Count -eq 0)) { 'ok' } else { 'FAIL' }
    if ($status -eq 'FAIL') { $allOk = $false }

    $report += [pscustomobject]@{
        Dll       = $dll.Name
        UCRT      = [bool]$hasUcrt
        VCRuntime = [bool]$hasVcr
        LegacyCRT = if ($legacy.Count -gt 0) { ($legacy -join ',') } else { '-' }
        Status    = $status
    }
}

$report | Format-Table Dll, UCRT, VCRuntime, LegacyCRT, Status -AutoSize

if (-not $allOk) {
    throw "one or more DLLs failed CRT consistency check. Rebuild with matching /MD dynamic UCRT."
}
Write-Host "==> all DLLs use consistent dynamic UCRT. CRT check passed."
