[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$InstallerPath,

    [Parameter(Mandatory)]
    [string]$InstallDirectory,

    [Parameter(Mandatory)]
    [string]$ExpectedVersion,

    [switch]$AllowLocalUserDataMutation,

    [switch]$AllowMachineWideMutation
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

function Assert-InstalledAt {
    param(
        [Parameter(Mandatory)]
        [string]$Directory
    )

    if (-not (Test-Path -LiteralPath (Join-Path $Directory "FusionCanvas.App.exe") -PathType Leaf)) {
        throw "FusionCanvas.App.exe was not installed in '$Directory'."
    }
    if (-not (Test-Path -LiteralPath (Join-Path $Directory "Uninstall.exe") -PathType Leaf)) {
        throw "Uninstall.exe was not created in '$Directory'."
    }

    $install = Get-ItemProperty -LiteralPath "HKLM:\SOFTWARE\FusionCanvas" -ErrorAction Stop
    $uninstall = Get-ItemProperty -LiteralPath "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\FusionCanvas" -ErrorAction Stop
    if ($install.InstallDir -ne $Directory -or $uninstall.InstallLocation -ne $Directory) {
        throw "Machine-wide registration does not point to '$Directory'."
    }
    if ($uninstall.DisplayVersion -ne $ExpectedVersion) {
        throw "Machine-wide registration has version '$($uninstall.DisplayVersion)', expected '$ExpectedVersion'."
    }
}

function Test-LegacyRegistryKey {
    param([Parameter(Mandatory)][string]$SubKey)

    $hive = [Microsoft.Win32.RegistryKey]::OpenBaseKey(
        [Microsoft.Win32.RegistryHive]::CurrentUser,
        [Microsoft.Win32.RegistryView]::Registry32)
    try {
        $key = $hive.OpenSubKey($SubKey)
        if ($null -eq $key) { return $false }
        $key.Dispose()
        return $true
    }
    finally {
        $hive.Dispose()
    }
}

function Set-LegacyInstallRegistration {
    param([Parameter(Mandatory)][string]$Directory)

    $hive = [Microsoft.Win32.RegistryKey]::OpenBaseKey(
        [Microsoft.Win32.RegistryHive]::CurrentUser,
        [Microsoft.Win32.RegistryView]::Registry32)
    try {
        $install = $hive.CreateSubKey("SOFTWARE\FusionCanvas")
        $install.SetValue("InstallDir", $Directory, [Microsoft.Win32.RegistryValueKind]::String)
        $install.Dispose()
        $uninstall = $hive.CreateSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\FusionCanvas")
        $uninstall.SetValue("DisplayName", "FusionCanvas", [Microsoft.Win32.RegistryValueKind]::String)
        $uninstall.Dispose()
    }
    finally {
        $hive.Dispose()
    }
}

function Remove-LegacyInstallRegistration {
    $hive = [Microsoft.Win32.RegistryKey]::OpenBaseKey(
        [Microsoft.Win32.RegistryHive]::CurrentUser,
        [Microsoft.Win32.RegistryView]::Registry32)
    try {
        $hive.DeleteSubKeyTree("SOFTWARE\FusionCanvas", $false)
        $hive.DeleteSubKeyTree("SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\FusionCanvas", $false)
    }
    finally {
        $hive.Dispose()
    }
}

if (-not (Test-Path -LiteralPath $InstallerPath -PathType Leaf)) {
    throw "Installer was not found at '$InstallerPath'."
}
if (-not $AllowLocalUserDataMutation) {
    throw "Installer smoke checks modify a temporary sentinel below %LOCALAPPDATA%\FusionCanvas. Pass -AllowLocalUserDataMutation only in an isolated test environment."
}
if (-not $AllowMachineWideMutation) {
    throw "Installer smoke checks modify Program Files, HKLM, and shared shortcuts. Pass -AllowMachineWideMutation only in an isolated Windows test environment."
}

$root = Join-Path $env:RUNNER_TEMP "FusionCanvas-InstallerSmoke"
$defaultInstallDirectory = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFiles)) "FusionCanvas"
$legacyInstallDirectory = Join-Path $env:LOCALAPPDATA "Programs\FusionCanvas"
$legacyCustomDirectory = Join-Path $root "legacy-custom"
$commonStartMenu = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::CommonStartMenu)) "Programs\FusionCanvas"
$commonStartMenuShortcut = Join-Path $commonStartMenu "FusionCanvas.lnk"
$commonDesktopShortcut = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::CommonDesktopDirectory)) "FusionCanvas.lnk"
$userStartMenuShortcut = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\FusionCanvas\FusionCanvas.lnk"
$dataDirectory = Join-Path $env:LOCALAPPDATA "FusionCanvas"
$sentinelPath = Join-Path $dataDirectory "installer-smoke-sentinel.txt"
$installRegistryPath = "HKLM:\SOFTWARE\FusionCanvas"
$uninstallRegistryPath = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\FusionCanvas"
$legacyInstallRegistrySubKey = "SOFTWARE\FusionCanvas"
$legacyUninstallRegistrySubKey = "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\FusionCanvas"
$previousSentinel = $null
$hadSentinel = Test-Path -LiteralPath $sentinelPath -PathType Leaf
$dataDirectoryCreated = $false
$testRootCreated = $false
$defaultInstallAttempted = $false
$alternateInstallAttempted = $false
$legacyInstallCreated = $false
$legacyCustomCreated = $false

foreach ($path in @($defaultInstallDirectory, $legacyInstallDirectory, $InstallDirectory)) {
    if (Test-Path -LiteralPath $path) {
        throw "Installer smoke test requires an isolated environment; '$path' already exists."
    }
}
foreach ($registryPath in @($installRegistryPath, $uninstallRegistryPath)) {
    if (Test-Path -LiteralPath $registryPath) {
        throw "Installer smoke test requires an isolated environment; '$registryPath' already exists."
    }
}
if ((Test-LegacyRegistryKey -SubKey $legacyInstallRegistrySubKey) -or
    (Test-LegacyRegistryKey -SubKey $legacyUninstallRegistrySubKey)) {
    throw "Installer smoke test requires an isolated environment; a legacy 32-bit HKCU registration already exists."
}

try {
    New-Item -ItemType Directory -Force -Path $root | Out-Null
    $testRootCreated = $true
    if (-not (Test-Path -LiteralPath $dataDirectory)) {
        New-Item -ItemType Directory -Force -Path $dataDirectory | Out-Null
        $dataDirectoryCreated = $true
    }
    if ($hadSentinel) {
        $previousSentinel = [IO.File]::ReadAllBytes($sentinelPath)
    }
    [IO.File]::WriteAllText($sentinelPath, "preserve-$ExpectedVersion")

    # Fresh installs use the 64-bit Program Files default without /D.
    $defaultInstallAttempted = $true
    Invoke-Installer -Executable $InstallerPath -Arguments @('/S')
    Assert-InstalledAt -Directory $defaultInstallDirectory
    if (-not (Test-Path -LiteralPath $commonStartMenuShortcut -PathType Leaf)) {
        throw "Fresh installation did not create the selected all-users Start Menu shortcut."
    }
    if (-not (Test-Path -LiteralPath $commonDesktopShortcut -PathType Leaf)) {
        throw "Fresh installation did not create the selected all-users desktop shortcut."
    }

    # A repeated install exercises a machine-wide upgrade in place.
    Invoke-Installer -Executable $InstallerPath -Arguments @('/S')
    Assert-InstalledAt -Directory $defaultInstallDirectory
    if ([IO.File]::ReadAllText($sentinelPath) -ne "preserve-$ExpectedVersion") {
        throw "Upgrade rerun changed the user-data sentinel."
    }

    $processStub = Join-Path $root "FusionCanvas.App.exe"
    Copy-Item -LiteralPath (Join-Path $env:WINDIR "System32\timeout.exe") -Destination $processStub
    $stub = Start-Process -FilePath $processStub -ArgumentList '/t', '60' -PassThru
    try {
        Start-Sleep -Milliseconds 500
        Invoke-InstallerExpectFailure -Executable $InstallerPath -Arguments @('/S')
    }
    finally {
        if (-not $stub.HasExited) {
            Stop-Process -Id $stub.Id -Force
        }
    }

    $defaultUninstaller = Join-Path $defaultInstallDirectory "Uninstall.exe"
    Invoke-Installer -Executable $defaultUninstaller -Arguments @('/S')
    if (Test-Path -LiteralPath $defaultInstallDirectory) {
        throw "Machine-wide uninstall left the application directory behind."
    }
    if ((Test-Path -LiteralPath $installRegistryPath) -or (Test-Path -LiteralPath $uninstallRegistryPath)) {
        throw "Machine-wide uninstall left install or uninstall registration behind."
    }
    if ((Test-Path -LiteralPath $commonStartMenuShortcut) -or (Test-Path -LiteralPath $commonDesktopShortcut)) {
        throw "Machine-wide uninstall left a shared shortcut behind."
    }
    if ([IO.File]::ReadAllText($sentinelPath) -ne "preserve-$ExpectedVersion") {
        throw "Uninstall changed the user-data sentinel."
    }

    # The initial installer used this fixed per-user default. Verify safe migration.
    New-Item -ItemType Directory -Force -Path $legacyInstallDirectory | Out-Null
    $legacyInstallCreated = $true
    [IO.File]::WriteAllText((Join-Path $legacyInstallDirectory "legacy-marker.txt"), "legacy")
    Set-LegacyInstallRegistration -Directory $legacyInstallDirectory
    New-Item -ItemType Directory -Force -Path (Split-Path $userStartMenuShortcut) | Out-Null
    [IO.File]::WriteAllText($userStartMenuShortcut, "legacy shortcut")

    Invoke-Installer -Executable $InstallerPath -Arguments @('/S')
    Assert-InstalledAt -Directory $defaultInstallDirectory
    if (Test-Path -LiteralPath $legacyInstallDirectory) {
        throw "Legacy per-user application files were not removed after successful migration."
    }
    if ((Test-LegacyRegistryKey -SubKey $legacyInstallRegistrySubKey) -or
        (Test-LegacyRegistryKey -SubKey $legacyUninstallRegistrySubKey)) {
        throw "Legacy per-user registration remains after successful migration."
    }
    if (Test-Path -LiteralPath $userStartMenuShortcut) {
        throw "Legacy per-user shortcut remains after successful migration."
    }
    if ([IO.File]::ReadAllText($sentinelPath) -ne "preserve-$ExpectedVersion") {
        throw "Legacy migration changed the user-data sentinel."
    }
    Invoke-Installer -Executable (Join-Path $defaultInstallDirectory "Uninstall.exe") -Arguments @('/S')

    # An HKCU path outside the known legacy default must never be deleted elevated.
    New-Item -ItemType Directory -Force -Path $legacyCustomDirectory | Out-Null
    $legacyCustomCreated = $true
    $legacyCustomMarker = Join-Path $legacyCustomDirectory "keep.txt"
    [IO.File]::WriteAllText($legacyCustomMarker, "preserve")
    Set-LegacyInstallRegistration -Directory $legacyCustomDirectory
    Invoke-InstallerExpectFailure -Executable $InstallerPath -Arguments @('/S')
    if ([IO.File]::ReadAllText($legacyCustomMarker) -ne "preserve") {
        throw "The legacy custom location was modified by an elevated setup."
    }
    if (-not (Test-LegacyRegistryKey -SubKey $legacyInstallRegistrySubKey)) {
        throw "The legacy custom registration was changed even though setup was rejected."
    }
    Remove-LegacyInstallRegistration
    Remove-Item -LiteralPath $legacyCustomDirectory -Recurse -Force

    # User-selected alternate locations remain supported.
    $alternateArguments = @('/S', "/D=$InstallDirectory")
    $alternateInstallAttempted = $true
    Invoke-Installer -Executable $InstallerPath -Arguments $alternateArguments
    Assert-InstalledAt -Directory $InstallDirectory
    Invoke-Installer -Executable (Join-Path $InstallDirectory "Uninstall.exe") -Arguments @('/S')
    if (Test-Path -LiteralPath $InstallDirectory) {
        throw "Uninstall left the selected alternate application directory behind."
    }
    if ([IO.File]::ReadAllText($sentinelPath) -ne "preserve-$ExpectedVersion") {
        throw "Alternate install or uninstall changed the user-data sentinel."
    }
}
finally {
    if ($null -ne $stub -and -not $stub.HasExited) {
        Stop-Process -Id $stub.Id -Force -ErrorAction SilentlyContinue
    }
    $attemptedInstallDirectories = @()
    if ($defaultInstallAttempted) { $attemptedInstallDirectories += $defaultInstallDirectory }
    if ($alternateInstallAttempted) { $attemptedInstallDirectories += $InstallDirectory }
    foreach ($directory in $attemptedInstallDirectories) {
        $uninstaller = Join-Path $directory "Uninstall.exe"
        if (Test-Path -LiteralPath $uninstaller -PathType Leaf) {
            try { Start-Process -FilePath $uninstaller -ArgumentList @('/S') -Wait -ErrorAction Stop } catch { }
        }
    }
    foreach ($registryPath in @($installRegistryPath, $uninstallRegistryPath)) {
        if (Test-Path -LiteralPath $registryPath) {
            Remove-Item -LiteralPath $registryPath -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
    Remove-LegacyInstallRegistration
    $ownedDirectories = @()
    if ($defaultInstallAttempted) { $ownedDirectories += $defaultInstallDirectory }
    if ($alternateInstallAttempted) { $ownedDirectories += $InstallDirectory }
    if ($legacyInstallCreated) { $ownedDirectories += $legacyInstallDirectory }
    if ($legacyCustomCreated) { $ownedDirectories += $legacyCustomDirectory }
    foreach ($path in $ownedDirectories) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
    foreach ($shortcut in @($commonStartMenuShortcut, $commonDesktopShortcut, $userStartMenuShortcut)) {
        if (Test-Path -LiteralPath $shortcut) {
            Remove-Item -LiteralPath $shortcut -Force -ErrorAction SilentlyContinue
        }
    }
    foreach ($directory in @($commonStartMenu, (Split-Path $userStartMenuShortcut))) {
        if (Test-Path -LiteralPath $directory) {
            Remove-Item -LiteralPath $directory -Force -ErrorAction SilentlyContinue
        }
    }
    if ($testRootCreated -and (Test-Path -LiteralPath $root)) {
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

Write-Host "FusionCanvas machine-wide installer smoke checks passed for version $ExpectedVersion."
