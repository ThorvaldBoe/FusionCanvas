param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = "Stop"

$timestamp = "2026-07-27T00:00:00Z"
$sourceUrl = "https://github.com/NSweed/CATCHPHRASE"
$paperUrl = "https://aclanthology.org/2021.acl-short.1/"
$sourceId = "source-catchphrase-snowclones-batch-005-v2"
$sourceName = "CATCHPHRASE Pop-Culture Snowclone Acquisition Batch 005"
$sourcePath = Join-Path $RepositoryRoot "data\phrase-intelligence\sources\acquisition-batch-005.sources.v2.jsonl"
$patternPath = Join-Path $RepositoryRoot "data\phrase-intelligence\patterns\acquisition-batch-005.patterns.v2.jsonl"

# The upstream extension file contains 21 seed/pattern rows. This batch retains
# 13 new slot-bearing structures after duplicate and usefulness screening.
$patterns = @(
    "They may take our [X], but they'll never take our [Y]"
    "Nobody puts Baby in [X]"
    "Say hello to my [X]"
    "To [X] and beyond"
    "Tonight we [X] in hell"
    "I [X] that I am up to no [Y]"
    "The [X] send their [Y]"
    "A [X] always pays his [Y]"
    "What do we say to the god of [X]?"
    "By [X] of the [Y]"
    "This [X] will not stand, man"
    "You don't mess with [X]"
    "You had my [X], now you have my [Y]"
)

$slotDescriptions = @{
    X = "The primary replaceable word, phrase, role, object, action, quality, or concept."
    Y = "A secondary replaceable word, phrase, role, object, action, quality, or concept."
}

function Get-NormalizedText([string]$text) {
    $normalized = $text.ToLowerInvariant()
    $normalized = $normalized -replace "[,:;.!?'""()-]", " "
    return ($normalized -replace "\s+", " ").Trim()
}

function Get-Slots([string]$text) {
    $matches = [regex]::Matches($text, "\[([A-Z])\]")
    $names = @($matches | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique)
    return @($names | ForEach-Object {
        [ordered]@{
            name = $_
            type = "generic"
            required = $true
            description = $slotDescriptions[$_]
        }
    })
}

$sourceRecord = [ordered]@{
    recordType = "source-record"
    sourceLicense = "unknown"
    collectionMethod = "research-dataset-extraction"
    storesExactSourceText = $false
    derivedFromExactPhrase = $true
    sourceTier = "tier-3"
    recommendedUsageMode = "pattern-extraction-only"
    collectionRisk = "high"
    commercialUseRisk = "high"
    transformationPotential = "very-high"
    patternExtractionPotential = "very-high"
    structuralExtractionPotential = "very-high"
    directUseAllowed = $false
    requiresReviewBeforeUse = $true
    requiresAttribution = $true
    reviewStatus = "needs-review"
    createdAt = $timestamp
    updatedAt = $timestamp
    id = $sourceId
    sourceCategory = "snowclone"
    sourceName = $sourceName
    normalizedText = (Get-NormalizedText $sourceName)
    sourceUrl = $sourceUrl
    categories = @("snowclone", "catchphrase", "research-dataset", "pop-culture", "batch-005")
    creativeIntents = @("wordplay", "humor", "recognition", "transformation")
    templateFamilies = @("Snowclone")
    provenance = [ordered]@{
        origin = "Slot-bearing patterns extracted from the public CATCHPHRASE research repository associated with an ACL-IJCNLP 2021 paper."
        retrievedAt = $timestamp
        evidence = @(
            "The repository README describes datasets for snowclone-form tagging and snowclone-reference detection."
            "The chrome_extension/patterns file contains 21 pop-culture seed/pattern rows."
            "Screening removed four existing structures, three fixed phrases without replaceable slots, and one overly broad form, leaving 13 new structures."
            "Associated paper: $paperUrl"
        )
        confidence = 0.95
    }
    notes = "The upstream repository does not declare a dataset license. Retain these structures for reviewed pattern extraction only; do not treat their presence as approval to reproduce source quotations or franchise-associated wording."
}

$patternRecords = for ($index = 0; $index -lt $patterns.Count; $index++) {
    $text = $patterns[$index]
    $recordNumber = ($index + 1).ToString("0000")
    [ordered]@{
        recordType = "pattern-record"
        sourceLicense = "unknown"
        collectionMethod = "research-dataset-extraction"
        storesExactSourceText = $false
        derivedFromExactPhrase = $true
        sourceTier = "tier-3"
        recommendedUsageMode = "pattern-extraction-only"
        collectionRisk = "high"
        commercialUseRisk = "high"
        transformationPotential = "very-high"
        patternExtractionPotential = "very-high"
        structuralExtractionPotential = "very-high"
        directUseAllowed = $false
        requiresReviewBeforeUse = $true
        requiresAttribution = $true
        reviewStatus = "needs-review"
        createdAt = $timestamp
        updatedAt = $timestamp
        id = "pattern-batch-005-catchphrase-$recordNumber"
        patternText = $text
        normalizedText = (Get-NormalizedText $text)
        sourceCategory = "snowclone"
        sourceId = $sourceId
        sourceName = $sourceName
        sourceUrl = $sourceUrl
        categories = @("snowclone", "batch-005", "cross-niche", "slot-template", "catchphrase", "pop-culture")
        creativeIntents = @("wordplay", "recognition", "transformation")
        templateFamilies = @("Snowclone")
        slots = @(Get-Slots $text)
        provenance = [ordered]@{
            origin = "Generalized from a wildcard-bearing row in the CATCHPHRASE chrome_extension/patterns dataset."
            retrievedAt = $timestamp
            evidence = @("The upstream dataset pairs a recognizable pop-culture seed with a wildcard pattern.")
            confidence = 0.95
        }
        notes = "Recognizable pop-culture-derived cadence with unknown dataset licensing. Use only for structural extraction after provenance, rights, trademark, originality, and marketplace review."
    }
}

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$sourceLines = @($sourceRecord | ConvertTo-Json -Depth 10 -Compress)
$patternLines = @($patternRecords | ForEach-Object { $_ | ConvertTo-Json -Depth 10 -Compress })
[System.IO.File]::WriteAllLines($sourcePath, $sourceLines, $utf8NoBom)
[System.IO.File]::WriteAllLines($patternPath, $patternLines, $utf8NoBom)

Write-Output "Wrote 1 source record to $sourcePath"
Write-Output "Wrote $($patternRecords.Count) pattern records to $patternPath"
