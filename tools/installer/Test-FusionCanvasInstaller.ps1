[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$InstallerPath,

    [Parameter(Mandatory)]
    [string]$InstallDirectory,

    [Parameter(Mandatory)]
    [string]$ExpectedVersion,

    [switch]$AllowLocalUserDataMutation
)

$ErrorActionPreference = "Stop"

function Invoke-Installer {
    param(
        [Parameter(Mandatory)]
        [string]$Executable,

        [Parameter(Mandatory)]
        [string[]]$Arguments
    )

    $process = Start-Process -FilePath $Executable -ArgumentList $Arguments -Wait -PassThru
    if ($process.ExitCode -ne 0) {
        throw "Installer command '$Executable $($Arguments -join ' ')' failed with exit code $($process.ExitCode)."
    }
}

function Invoke-InstallerExpectFailure {
    param(
        [Parameter(Mandatory)]
        [string]$Executable,

        [Parameter(Mandatory)]
        [string[]]$Arguments
    )

    $process = Start-Process -FilePath $Executable -ArgumentList $Arguments -Wait -PassThru
    if ($process.ExitCode -eq 0) {
        throw "Installer command was expected to fail but returned success."
    }
}

if (-not (Test-Path -LiteralPath $InstallerPath -PathType Leaf)) {
    throw "Installer was not found at '$InstallerPath'."
}

$root = Join-Path $env:RUNNER_TEMP "FusionCanvas-InstallerSmoke"
$shortcutDirectory = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\FusionCanvas"
$startMenuShortcut = Join-Path $shortcutDirectory "FusionCanvas.lnk"
$desktopShortcut = Join-Path ([Environment]::GetFolderPath("Desktop")) "FusionCanvas.lnk"
$dataDirectory = Join-Path $env:LOCALAPPDATA "FusionCanvas"
$sentinelPath = Join-Path $dataDirectory "installer-smoke-sentinel.txt"
$uninstallerPath = Join-Path $InstallDirectory "Uninstall.exe"
$applicationPath = Join-Path $InstallDirectory "FusionCanvas.App.exe"
$previousSentinel = $null
$hadSentinel = Test-Path -LiteralPath $sentinelPath -PathType Leaf
$dataDirectoryCreated = $false

if (-not $AllowLocalUserDataMutation) {
    throw "Installer smoke checks modify a temporary sentinel below %LOCALAPPDATA%\FusionCanvas. Pass -AllowLocalUserDataMutation only in an isolated test environment."
}

try {
    if (Test-Path -LiteralPath $root) {
        Remove-Item -LiteralPath $root -Recurse -Force
    }
    New-Item -ItemType Directory -Force -Path $root | Out-Null
    if (-not (Test-Path -LiteralPath $dataDirectory)) {
        New-Item -ItemType Directory -Force -Path $dataDirectory | Out-Null
        $dataDirectoryCreated = $true
    }
    if ($hadSentinel) {
        $previousSentinel = [IO.File]::ReadAllBytes($sentinelPath)
    }
    [IO.File]::WriteAllText($sentinelPath, "preserve-$ExpectedVersion")

    $installerArguments = @('/S', "/D=$InstallDirectory")
    Invoke-Installer -Executable $InstallerPath -Arguments $installerArguments

    if (-not (Test-Path -LiteralPath $applicationPath -PathType Leaf)) {
        throw "Fresh installation did not place FusionCanvas.App.exe in '$InstallDirectory'."
    }
    if (-not (Test-Path -LiteralPath $uninstallerPath -PathType Leaf)) {
        throw "Fresh installation did not create the uninstaller."
    }
    if (-not (Test-Path -LiteralPath $startMenuShortcut -PathType Leaf)) {
        throw "Fresh installation did not create the selected Start Menu shortcut."
    }
    if (-not (Test-Path -LiteralPath $desktopShortcut -PathType Leaf)) {
        throw "Fresh installation did not create the selected desktop shortcut."
    }

    Invoke-Installer -Executable $InstallerPath -Arguments $installerArguments
    if (-not (Test-Path -LiteralPath $applicationPath -PathType Leaf)) {
        throw "Upgrade rerun did not leave the application installed."
    }
    if ([IO.File]::ReadAllText($sentinelPath) -ne "preserve-$ExpectedVersion") {
        throw "Upgrade rerun changed the user-data sentinel."
    }

    $processStub = Join-Path $root "FusionCanvas.App.exe"
    Copy-Item -LiteralPath (Join-Path $env:WINDIR "System32\timeout.exe") -Destination $processStub
    $stub = Start-Process -FilePath $processStub -ArgumentList '/t', '60' -PassThru
    try {
        Start-Sleep -Milliseconds 500
        Invoke-InstallerExpectFailure -Executable $InstallerPath -Arguments $installerArguments
    }
    finally {
        if (-not $stub.HasExited) {
            Stop-Process -Id $stub.Id -Force
        }
    }

    Invoke-Installer -Executable $uninstallerPath -Arguments @('/S')
    if (Test-Path -LiteralPath $InstallDirectory) {
        throw "Uninstall left the application directory behind."
    }
    if (Test-Path -LiteralPath $startMenuShortcut) {
        throw "Uninstall left the Start Menu shortcut behind."
    }
    if (Test-Path -LiteralPath $desktopShortcut) {
        throw "Uninstall left the desktop shortcut behind."
    }
    if ([IO.File]::ReadAllText($sentinelPath) -ne "preserve-$ExpectedVersion") {
        throw "Uninstall changed the user-data sentinel."
    }
}
finally {
    if (Test-Path -LiteralPath $root) {
        Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
    }
    if ($hadSentinel) {
        [IO.File]::WriteAllBytes($sentinelPath, $previousSentinel)
    }
    elseif (Test-Path -LiteralPath $sentinelPath) {
        Remove-Item -LiteralPath $sentinelPath -Force -ErrorAction SilentlyContinue
    }
    if ($dataDirectoryCreated -and (Test-Path -LiteralPath $dataDirectory)) {
        $remaining = Get-ChildItem -LiteralPath $dataDirectory -Force -ErrorAction SilentlyContinue
        if (-not $remaining) {
            Remove-Item -LiteralPath $dataDirectory -Force -ErrorAction SilentlyContinue
        }
    }
}

Write-Host "FusionCanvas installer smoke checks passed for version $ExpectedVersion."

