[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$ManifestPath,

    [Parameter(Mandatory)]
    [string]$InstallerPath,

    [Parameter(Mandatory)]
    [string]$BundlePath,

    [Parameter(Mandatory)]
    [string]$ExpectedVersion,

    [Parameter(Mandatory)]
    [string]$ExpectedSourceCommit,

    [Parameter(Mandatory)]
    [string]$InstallDirectory,

    [switch]$AllowMachineWideMutation
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ManifestPath -PathType Leaf)) {
    throw "Release manifest was not found at '$ManifestPath'."
}
if (-not (Test-Path -LiteralPath $InstallerPath -PathType Leaf)) {
    throw "Release installer was not found at '$InstallerPath'."
}
if (-not (Test-Path -LiteralPath $BundlePath -PathType Leaf)) {
    throw "Release provenance bundle was not found at '$BundlePath'."
}
if ($ExpectedVersion -notmatch '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-preview\.[1-9]\d*)?$') {
    throw "Expected release version '$ExpectedVersion' is not a supported FusionCanvas release version."
}

$manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
$installerName = Split-Path -Leaf $InstallerPath
$expectedInstallerName = "FusionCanvas-$ExpectedVersion-win-x64-Setup.exe"
$tagName = "v$ExpectedVersion"
$expectedInstallerUri = "https://github.com/ThorvaldBoe/FusionCanvas/releases/download/$tagName/$expectedInstallerName"
$expectedReleaseUri = "https://github.com/ThorvaldBoe/FusionCanvas/releases/tag/$tagName"
$expectedTrack = if ($ExpectedVersion -match '-preview\.\d+$') { "preview" } else { "stable" }

if ($manifest.schemaVersion -ne 3 -or $manifest.productVersion -ne $ExpectedVersion -or $manifest.platform -ne "win-x64" -or $manifest.track -ne $expectedTrack -or $manifest.tagName -ne $tagName -or $manifest.sourceCommit -cne $ExpectedSourceCommit) {
    throw "Release manifest schema, version, or platform does not match the tagged release."
}
if ($installerName -cne $expectedInstallerName -or $manifest.installerUri -cne $expectedInstallerUri -or $manifest.releaseUri -cne $expectedReleaseUri -or $manifest.bundleUri -cne "$expectedInstallerUri.sigstore.json") {
    throw "Release manifest does not identify the canonical installer and release paths."
}
if ($manifest.installerUri -match '[?#]' -or $manifest.releaseUri -match '[?#]') {
    throw "Release manifest contains a query or fragment in a trusted URL."
}

$actualChecksum = (Get-FileHash -LiteralPath $InstallerPath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actualChecksum -cne $manifest.sha256.ToLowerInvariant()) {
    throw "Installer SHA-256 does not match the release manifest."
}

$signerWorkflow = "ThorvaldBoe/FusionCanvas/.github/workflows/release-windows.yml"
& gh attestation verify $InstallerPath --bundle $BundlePath --repo "ThorvaldBoe/FusionCanvas" --signer-workflow $signerWorkflow --source-ref "refs/tags/$tagName" --format json | Out-Null
if ($LASTEXITCODE -ne 0) { throw "GitHub installer attestation verification failed." }
if (Test-Path -LiteralPath $InstallDirectory) {
    throw "Disposable install directory '$InstallDirectory' already exists."
}

& (Join-Path $PSScriptRoot "..\installer\Test-FusionCanvasInstaller.ps1") `
    -InstallerPath $InstallerPath `
    -InstallDirectory $InstallDirectory `
    -ExpectedVersion $ExpectedVersion `
    -AllowLocalUserDataMutation `
    -AllowMachineWideMutation:$AllowMachineWideMutation

Write-Host "FusionCanvas release trust checks passed for version $ExpectedVersion."
