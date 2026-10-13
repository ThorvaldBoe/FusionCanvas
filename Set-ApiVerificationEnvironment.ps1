[CmdletBinding()]
param(
    [ValidateSet('OpenRouter', 'Printify', 'Both')]
    [string] $Provider = 'Both'
)

$ErrorActionPreference = 'Stop'

function Read-ApiVerificationKey {
    param([Parameter(Mandatory)][string] $Prompt)

    $secureValue = Read-Host -Prompt $Prompt -AsSecureString
    if ($null -eq $secureValue -or $secureValue.Length -eq 0) {
        if ($null -ne $secureValue) { $secureValue.Dispose() }
        throw 'A non-empty key is required.'
    }

    return $secureValue
}

function ConvertFrom-SecureStringValue {
    param([Parameter(Mandatory)][System.Security.SecureString] $SecureValue)

    $pointer = [IntPtr]::Zero
    try {
        $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureValue)
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    }
    finally {
        if ($pointer -ne [IntPtr]::Zero) {
            [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
        }
    }
}

function Read-PositiveDecimal {
    param([Parameter(Mandatory)][string] $Prompt)

    while ($true) {
        $text = Read-Host -Prompt $Prompt
        [decimal] $value = 0
        $valid = [decimal]::TryParse(
            $text,
            [Globalization.NumberStyles]::AllowDecimalPoint,
            [Globalization.CultureInfo]::InvariantCulture,
            [ref] $value)
        if ($valid -and $value -gt 0) {
            return $value
        }

        Write-Host 'Enter a positive amount using a dot for the decimal separator, for example 0.25.' -ForegroundColor Yellow
    }
}

$openRouterSecure = $null
$printifySecure = $null
$openRouterKey = $null
$printifyKey = $null

try {
    $openRouterBudget = $null

    if ($Provider -in @('OpenRouter', 'Both')) {
        $openRouterSecure = Read-ApiVerificationKey 'Paste the dedicated OpenRouter test API key (input is hidden)'
        $openRouterBudget = Read-PositiveDecimal 'Maximum OpenRouter spend for each session in USD'
    }

    if ($Provider -in @('Printify', 'Both')) {
        $printifySecure = Read-ApiVerificationKey 'Paste the Printify FusionCanvas DevTest API key (input is hidden)'
    }

    # Persist selected settings for this Windows user and also populate this process for immediate use.
    # Windows User environment variables are readable by processes running as this user.
    if ($null -ne $openRouterSecure) {
        $openRouterKey = ConvertFrom-SecureStringValue $openRouterSecure
        [Environment]::SetEnvironmentVariable('FUSIONCANVAS_OPENROUTER_TEST_API_KEY', $openRouterKey, [EnvironmentVariableTarget]::User)
        [Environment]::SetEnvironmentVariable('FUSIONCANVAS_OPENROUTER_TEST_API_KEY', $openRouterKey, [EnvironmentVariableTarget]::Process)
        $openRouterBudgetText = $openRouterBudget.ToString([Globalization.CultureInfo]::InvariantCulture)
        [Environment]::SetEnvironmentVariable(
            'FUSIONCANVAS_OPENROUTER_SESSION_MAX_COST_USD',
            $openRouterBudgetText,
            [EnvironmentVariableTarget]::User)
        [Environment]::SetEnvironmentVariable(
            'FUSIONCANVAS_OPENROUTER_SESSION_MAX_COST_USD',
            $openRouterBudgetText,
            [EnvironmentVariableTarget]::Process)
    }

    if ($null -ne $printifySecure) {
        $printifyKey = ConvertFrom-SecureStringValue $printifySecure
        [Environment]::SetEnvironmentVariable('FUSIONCANVAS_PRINTIFY_DEVTEST_API_KEY', $printifyKey, [EnvironmentVariableTarget]::User)
        [Environment]::SetEnvironmentVariable('FUSIONCANVAS_PRINTIFY_DEVTEST_API_KEY', $printifyKey, [EnvironmentVariableTarget]::Process)
        # Older setup versions asked for a manual shop ID; the runner now resolves it by exact shop title.
        [Environment]::SetEnvironmentVariable('FUSIONCANVAS_PRINTIFY_DEVTEST_SHOP_ID', $null, [EnvironmentVariableTarget]::User)
        [Environment]::SetEnvironmentVariable('FUSIONCANVAS_PRINTIFY_DEVTEST_SHOP_ID', $null, [EnvironmentVariableTarget]::Process)
    }

    Write-Host "Saved the selected $Provider API verification settings to your Windows User environment. Key values were not displayed." -ForegroundColor Green
    Write-Host 'They are also active in this PowerShell window, so you can run dotnet immediately.'
    Write-Host 'Restart Codex and other already-open apps before expecting them to inherit the saved settings.'
    Write-Host 'Windows User environment variables can be read by processes running as your account.' -ForegroundColor Yellow
}
finally {
    if ($null -ne $openRouterSecure) { $openRouterSecure.Dispose() }
    if ($null -ne $printifySecure) { $printifySecure.Dispose() }
    $openRouterKey = $null
    $printifyKey = $null
}

