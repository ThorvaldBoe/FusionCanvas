param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = "Stop"

$timestamp = "2026-07-27T00:00:00Z"
$sourceUrl = "https://en.wiktionary.org/wiki/Appendix:English_snowclones"
$sourceId = "source-wiktionary-date-unknown-snowclones-batch-004-v2"
$sourceName = "Wiktionary Date Unknown Snowclones Acquisition Batch 004"
$sourcePath = Join-Path $RepositoryRoot "data\phrase-intelligence\sources\acquisition-batch-004.sources.v2.jsonl"
$patternPath = Join-Path $RepositoryRoot "data\phrase-intelligence\patterns\acquisition-batch-004.patterns.v2.jsonl"

# Wiktionary lists 67 entries. "X, and all I got was this lousy Y" is omitted
# because an existing "I Survived ... And All I Got Was ..." record captures it.
$patterns = @(
    "Did someone say [X]?"
    "where [X] goes to die"
    "[X] breeds [Y]"
    "What price [X]?"
    "the [X], the whole [X], and nothing but the [X]"
    "Remember when [X]? Pepperidge Farm remembers."
    "to put the [X] in [Y]"
    "a few [X] short of a [Y]"
    "What is this [X] of which you speak?"
    "[X] called, they want their [Y] back"
    "[X] with a capital [Y]"
    "that's [X] for you"
    "leave [X] [Y]"
    "the great [X] in the sky"
    "gone to be with [X]"
    "[X], they said. It'll be fun, they said."
    "make like [X] and [Y]"
    "welcome to [X](-town), population: [Y]"
    "an [X] that just won't quit"
    "if I had an [X] for every time I [Y]"
    "[X] is [X]"
    "as [X] as it gets"
    "[X].exe has stopped working"
    "the [X] to end all [X]s"
    "the [X] of all [X]s"
    "one likes [X], but [X] doesn't like one"
    "come back [X], all is forgiven"
    "if [X] is not [Y], then I don't know what is"
    "if that's not [X], I don't know what is"
    "[X] will be [X]"
    "I can't believe it's not [X]"
    "if it [W]s like an [X], [Y]s like an [X], and [Z]s like an [X], then it probably is an [X]"
    "[X] is the [Y] of [Z]"
    "if [X] can't [Y], nothing will"
    "it's only [W] if it comes from the [X] region of [Y], otherwise it's sparkling [Z]"
    "there are two types of people in the world, [X] and [Y]"
    "what's [X] among friends?"
    "if you've seen one [X], you've seen them all"
    "[X] is a hell of a drug"
    "show me an [X], I'll show you a [Y]"
    "you can't spell [X] without [Y]"
    "I got your [X] right here"
    "don't [X] me"
    "Is [X] in the room with us right now?"
    "not the [X]-est in the [Y]"
    "once a [X], always a [X]"
    "so [X] I could die"
    ("[X], [Y], and [Z]" + [char]0x2014 + "pick any two")
    "[X] among [X]s"
    "[X] are born, not made"
    "[X] are made, not born"
    "[X] are [X]"
    "[X] as [X] can be"
    "[X]er than thou"
    "[X] is a good servant but a bad master"
    "[X] is not going to [Y] itself"
    "[X] me and call me [Y]"
    "[X] weather for ducks"
    "[X] [Y] is [X]"
    "[X] walked so [Y] could run"
    "[X] is [X]ing"
    "if [X] and [Y] had a baby"
    "all [X] and no [Y]"
    "please give me a whole tray of [X]"
    "[X] killed my family, burned my crops"
    "spoken like [X]"
)

$highRiskPatterns = @(
    "Remember when [X]? Pepperidge Farm remembers."
    "I can't believe it's not [X]"
)

$slotDescriptions = @{
    W = "A replaceable action, category, or label used in the first position."
    X = "The primary replaceable word, phrase, role, object, action, quality, or concept."
    Y = "A secondary replaceable word, phrase, role, object, action, quality, or concept."
    Z = "A third replaceable word, phrase, action, category, or concept."
}

function Get-NormalizedText([string]$text) {
    $normalized = $text.ToLowerInvariant().Replace([char]0x2014, " ")
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
    sourceLicense = "CC-BY-SA-4.0"
    collectionMethod = "user-submission"
    storesExactSourceText = $true
    derivedFromExactPhrase = $true
    sourceTier = "tier-2"
    recommendedUsageMode = "collect-with-review"
    collectionRisk = "low"
    commercialUseRisk = "medium"
    transformationPotential = "very-high"
    patternExtractionPotential = "very-high"
    structuralExtractionPotential = "very-high"
    directUseAllowed = $false
    requiresReviewBeforeUse = $true
    requiresAttribution = $true
    reviewStatus = "candidate"
    createdAt = $timestamp
    updatedAt = $timestamp
    id = $sourceId
    sourceCategory = "snowclone"
    sourceName = $sourceName
    normalizedText = (Get-NormalizedText $sourceName)
    sourceUrl = $sourceUrl
    categories = @("snowclone", "wiktionary", "user-submission", "batch-004", "date-unknown")
    creativeIntents = @("wordplay", "humor", "comparison", "identity")
    templateFamilies = @("Snowclone")
    provenance = [ordered]@{
        origin = "User-submitted excerpt from the Date unknown section of Wiktionary's Appendix:English snowclones."
        retrievedAt = $timestamp
        evidence = @(
            "The source page lists 67 Date unknown snowclone structures."
            "One source structure was excluded as already represented in the repository, leaving 66 new records."
        )
        confidence = 0.95
    }
    notes = "Source text is attribution-required. Patterns are candidates for structural reuse only and are not approved product copy."
}

$patternRecords = for ($index = 0; $index -lt $patterns.Count; $index++) {
    $text = $patterns[$index]
    $isHighRisk = $highRiskPatterns -contains $text
    $recordNumber = ($index + 1).ToString("0000")
    [ordered]@{
        recordType = "pattern-record"
        sourceLicense = "CC-BY-SA-4.0"
        collectionMethod = "user-submission"
        storesExactSourceText = $true
        derivedFromExactPhrase = $true
        sourceTier = "tier-2"
        recommendedUsageMode = $(if ($isHighRisk) { "pattern-extraction-only" } else { "collect-with-review" })
        collectionRisk = $(if ($isHighRisk) { "medium" } else { "low" })
        commercialUseRisk = $(if ($isHighRisk) { "high" } else { "medium" })
        transformationPotential = "very-high"
        patternExtractionPotential = "very-high"
        structuralExtractionPotential = "very-high"
        directUseAllowed = $false
        requiresReviewBeforeUse = $true
        requiresAttribution = $true
        reviewStatus = "candidate"
        createdAt = $timestamp
        updatedAt = $timestamp
        id = "pattern-batch-004-wiktionary-$recordNumber"
        patternText = $text
        normalizedText = (Get-NormalizedText $text)
        sourceCategory = "snowclone"
        sourceId = $sourceId
        sourceName = $sourceName
        sourceUrl = $sourceUrl
        categories = @("snowclone", "batch-004", "cross-niche", "slot-template", "wiktionary")
        creativeIntents = @("wordplay", "adaptation")
        templateFamilies = @("Snowclone")
        slots = @(Get-Slots $text)
        provenance = [ordered]@{
            origin = "User-submitted Wiktionary snowclone structure, normalized to bracketed slots."
            retrievedAt = $timestamp
            evidence = @("Listed in the Date unknown section of Appendix:English snowclones.")
            confidence = 0.95
        }
        notes = $(if ($isHighRisk) {
            "Brand-associated source cadence; retain for pattern extraction only and require heightened review before any adaptation."
        } else {
            "Source-backed snowclone structure; review transformations for originality, attribution, and marketplace suitability."
        })
    }
}

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$sourceLines = @($sourceRecord | ConvertTo-Json -Depth 10 -Compress)
$patternLines = @($patternRecords | ForEach-Object { $_ | ConvertTo-Json -Depth 10 -Compress })
[System.IO.File]::WriteAllLines($sourcePath, $sourceLines, $utf8NoBom)
[System.IO.File]::WriteAllLines($patternPath, $patternLines, $utf8NoBom)

Write-Output "Wrote 1 source record to $sourcePath"
Write-Output "Wrote $($patternRecords.Count) pattern records to $patternPath"
