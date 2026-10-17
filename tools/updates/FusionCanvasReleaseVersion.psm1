function Get-FusionCanvasReleaseTrack {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$TagName,
        [Parameter(Mandatory)][string]$PackageVersion
    )

    if ($PackageVersion -notmatch '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-preview\.[1-9]\d*)?$') {
        throw "Application version '$PackageVersion' is not a supported Stable or Preview version."
    }
    if ($TagName -cne "v$PackageVersion") {
        throw "Release tag '$TagName' does not match application version '$PackageVersion'."
    }
    if ($PackageVersion -match '-preview\.[1-9]\d*$') { return 'preview' }
    return 'stable'
}

Export-ModuleMember -Function Get-FusionCanvasReleaseTrack
