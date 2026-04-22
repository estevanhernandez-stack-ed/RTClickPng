#requires -Version 7.0
<#
.SYNOPSIS
    Create a self-signed certificate for dev-installing the MSIX package.

.DESCRIPTION
    Running the MSIX locally (Add-AppxPackage) requires the package to be signed by a certificate
    the local machine trusts.  This script generates a self-signed cert, exports it to
    src/Package/RTClickPng_TemporaryKey.pfx, and installs it to the current user's trusted root.
    Run once per dev machine.  CI uses a GitHub Secret instead (items 11-12).

    The Subject must match the Publisher in Package.appxmanifest exactly, or signing will fail.
#>
[CmdletBinding()]
param(
    # Must match the Publisher in src/Package/Package.appxmanifest exactly. Partner-Center-issued
    # CN after the 2026-04-22 identity swap; override if you're regenerating against a different
    # manifest (e.g. for a fork or a pre-submit manifest rewrite).
    [string]$Subject = 'CN=177BCE59-0966-4975-9962-10E36652141F',
    [string]$OutputPath = (Join-Path $PSScriptRoot '..\src\Package\RTClickPng_TemporaryKey.pfx'),
    [securestring]$Password = (ConvertTo-SecureString -String 'RTClickPng-Dev' -AsPlainText -Force)
)

$ErrorActionPreference = 'Stop'

Write-Host "==> creating self-signed cert with Subject: $Subject"
$cert = New-SelfSignedCertificate `
    -Subject $Subject `
    -KeyUsage DigitalSignature `
    -FriendlyName "Right Click PNG Dev" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -Type CodeSigningCert `
    -NotAfter (Get-Date).AddYears(3)

$thumbprint = $cert.Thumbprint
Write-Host "==> thumbprint: $thumbprint"

Write-Host "==> exporting to $OutputPath"
Export-PfxCertificate -Cert "Cert:\CurrentUser\My\$thumbprint" -FilePath $OutputPath -Password $Password | Out-Null

Write-Host "==> installing to trusted root (requires admin elevation)"
$pfxPath = (Resolve-Path $OutputPath).Path
try {
    Import-PfxCertificate -FilePath $pfxPath -CertStoreLocation Cert:\LocalMachine\Root -Password $Password | Out-Null
    Write-Host "    installed to LocalMachine\Root"
} catch {
    Write-Warning "Could not install to LocalMachine\Root — run PowerShell as Administrator and re-run."
    Write-Warning "Until the cert is trusted, Add-AppxPackage will reject the signed MSIX."
}

Write-Host ""
Write-Host "Next steps:"
Write-Host "  1. Build the MSIX: msbuild src/Package/Package.wapproj /p:Configuration=Release /p:Platform=x64"
Write-Host "  2. Install it:    Add-AppxPackage -Path src/Package/AppPackages/.../RTClickPng.msix"
