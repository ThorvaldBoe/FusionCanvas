[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$ManifestPath,

    [Parameter(Mandatory)]
    [string]$InstallerPath,

    [Parameter(Mandatory)]
    [string]$ExpectedVersion,

    [Parameter(Mandatory)]
    [string]$InstallDirectory
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ManifestPath -PathType Leaf)) {
    throw "Release manifest was not found at '$ManifestPath'."
}
if (-not (Test-Path -LiteralPath $InstallerPath -PathType Leaf)) {
    throw "Release installer was not found at '$InstallerPath'."
}
if ($ExpectedVersion -notmatch '^\d+\.\d+\.\d+$') {
    throw "Expected release version '$ExpectedVersion' is not stable SemVer."
}

$manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
$installerName = Split-Path -Leaf $InstallerPath
$expectedInstallerName = "FusionCanvas-$ExpectedVersion-win-x64-Setup.exe"
$expectedInstallerUri = "https://github.com/ThorvaldBoe/FusionCanvas/releases/download/v$ExpectedVersion/$expectedInstallerName"
$expectedReleaseUri = "https://github.com/ThorvaldBoe/FusionCanvas/releases/tag/v$ExpectedVersion"

if ($manifest.schemaVersion -ne 2 -or $manifest.productVersion -ne $ExpectedVersion -or $manifest.platform -ne "win-x64") {
    throw "Release manifest schema, version, or platform does not match the tagged release."
}
if ($installerName -cne $expectedInstallerName -or $manifest.installerUri -cne $expectedInstallerUri -or $manifest.releaseUri -cne $expectedReleaseUri) {
    throw "Release manifest does not identify the canonical installer and release paths."
}
if ($manifest.installerUri -match '[?#]' -or $manifest.releaseUri -match '[?#]') {
    throw "Release manifest contains a query or fragment in a trusted URL."
}

$actualChecksum = (Get-FileHash -LiteralPath $InstallerPath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actualChecksum -cne $manifest.sha256.ToLowerInvariant()) {
    throw "Installer SHA-256 does not match the release manifest."
}

$signature = Get-AuthenticodeSignature -FilePath $InstallerPath
if ($signature.Status -ne "Valid" -or $null -eq $signature.SignerCertificate) {
    throw "Installer Authenticode verification failed with status '$($signature.Status)'."
}
$actualPublisherFingerprint = $signature.SignerCertificate.GetCertHashString([Security.Cryptography.HashAlgorithmName]::SHA256).ToLowerInvariant()
if ($actualPublisherFingerprint -cne $manifest.publisherCertificateSha256.ToLowerInvariant()) {
    throw "Installer publisher certificate does not match the release manifest."
}
if (Test-Path -LiteralPath $InstallDirectory) {
    throw "Disposable install directory '$InstallDirectory' already exists."
}

& (Join-Path $PSScriptRoot "..\installer\Test-FusionCanvasInstaller.ps1") `
    -InstallerPath $InstallerPath `
    -InstallDirectory $InstallDirectory `
    -ExpectedVersion $ExpectedVersion `
    -AllowLocalUserDataMutation

Write-Host "FusionCanvas release trust checks passed for version $ExpectedVersion."
