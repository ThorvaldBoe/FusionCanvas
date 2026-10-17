$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'FusionCanvasReleaseVersion.psm1') -Force

if ((Get-FusionCanvasReleaseTrack -TagName 'v1.2.3' -PackageVersion '1.2.3') -cne 'stable') {
    throw 'Stable release version preflight returned the wrong track.'
}
if ((Get-FusionCanvasReleaseTrack -TagName 'v1.2.3-preview.4' -PackageVersion '1.2.3-preview.4') -cne 'preview') {
    throw 'Preview release version preflight returned the wrong track.'
}

foreach ($invalid in @(
    @{ TagName = 'v1.2.4'; Version = '1.2.3' },
    @{ TagName = 'v1.2.3-preview.0'; Version = '1.2.3-preview.0' },
    @{ TagName = 'v1.2'; Version = '1.2' },
    @{ TagName = 'v1.2.3-rc.1'; Version = '1.2.3-rc.1' }
)) {
    try {
        Get-FusionCanvasReleaseTrack -TagName $invalid.TagName -PackageVersion $invalid.Version | Out-Null
        throw "Invalid release version was accepted: $($invalid.TagName)"
    }
    catch {
        if ($_.Exception.Message -like 'Invalid release version was accepted:*') { throw }
    }
}

Write-Host 'Stable and Preview release version preflight checks passed.'
