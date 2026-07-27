param(
    [Parameter(Mandatory = $true)]
    [string]$InputPath,
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = "Stop"
$timestamp = "2026-07-27T00:00:00Z"
$outputPath = Join-Path $RepositoryRoot "data\phrase-intelligence\patterns\submitted-snowclones-2026-07-27.patterns.v2.jsonl"
$existingPatternDirectory = Join-Path $RepositoryRoot "data\phrase-intelligence\patterns"

function Convert-ToPatternText([string]$text) {
    $value = $text.Trim()
    $value = $value -replace "^\[Current count,[^\]]+\]\s*", ""
    $value = $value -replace "https?://\S+", ""
    $value = $value -replace "\s+\((?:not sure|French|I think|not a lot of instances|[XYZWMN] is optional|[XYZWMN] usually)[\s\S]*$", ""
    $value = $value -replace "\s+\((?:also )?tentative\)", ""
    $value = $value -replace "\s+\((?:limerick|chiasmus|role-playing|It.s the smell|Wounded Knee|of mice and men|rich or thin|M for Murder|the people|England expects|British|original discussed|GOTO|[XYZWMN] rhymes|tomato|Chinese/Korean|\s*snowclone or idiom|[XYZWMN]0: living|long cat)[\s\S]*$", ""
    $value = $value -replace "\s+\(lie\)(?=\s+in it$)", ""
    $value = $value -replace "\s+\((?:suggested|tentative|not sure|German|French|history|original discussed|see comment|attributed|from |werewolf|modern|species names|morphology|same as|in reference|X only|maybe\??|this seems|this is a young|is this really|other geographical|mostly|2 senses:|Poltergeist|Wounded Knee|GOTO)[^)]*\)\s*$", ""
    $value = $value -replace "\s+\[(?:not there yet|same as|http)[^\]]*\]\s*$", ""
    $value = $value -creplace "X\[[^\]]+\]", "X"
    $value = $value -creplace "Y\[[^\]]+\]", "Y"
    $value = $value -creplace "Z\[[^\]]+\]", "Z"
    $value = $value -creplace "\bWWXD\b", "WW[X]D"
    $value = $value -creplace "\bXy McXerson\b", "[X]y Mc[X]erson"
    $value = $value -creplace "\bLolX\b", "Lol[X]"
    $value = $value -creplace "\bSchadenX\b", "Schaden[X]"
    $value = $value -creplace "\bXings\b", "[X]ings"
    $value = $value -replace "\bx-(?=\[e\|o\]red\b)", "[X]-"
    $value = $value -replace "\s+", " "
    $value = $value -replace "\s+\($", ""
    $value = $value.Trim(" ", ".", ",")

    # Convert the legacy metasyntactic variables into explicit v2 slots.
    $value = $value -creplace "(?<![A-Za-z\[])([XYZWMN])(?=(?:er|est|ed|ing|ness|s|es|core|gate|ville|ware|fest|thon|zilla|fu|wing|shaped|free|lorn|tastic|tacular|onomics|holic|freude)?(?![A-Za-z]))", '[$1]'
    $value = $value -creplace "\bLastName\b", "[LAST_NAME]"
    $value = $value -creplace "\bFirstName\b", "[FIRST_NAME]"
    return $value
}

function Get-CanonicalText([string]$text) {
    $value = $text.ToLowerInvariant()
    $value = $value -replace "\[[a-z0-9_]+\]", "[slot]"
    $value = $value -replace "[^a-z0-9\[\]]+", " "
    return ($value -replace "\s+", " ").Trim()
}

function Get-Slots([string]$text) {
    $matches = [regex]::Matches($text, "\[([A-Z][A-Z0-9_]*)\]")
    return @($matches | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique | ForEach-Object {
        [ordered]@{
            name = $_
            type = $_.ToLowerInvariant()
            required = $true
            description = "A replaceable value appropriate to the snowclone structure and selected niche."
        }
    })
}

$known = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
Get-ChildItem -LiteralPath $existingPatternDirectory -Filter "*.jsonl" | Where-Object {
    $_.FullName -ne $outputPath
} | ForEach-Object {
    Get-Content -LiteralPath $_.FullName -Encoding UTF8 | ForEach-Object {
        if (-not [string]::IsNullOrWhiteSpace($_)) {
            $record = $_ | ConvertFrom-Json
            if ($record.patternText) {
                [void]$known.Add((Get-CanonicalText $record.patternText))
            }
        }
    }
}

$seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$records = [System.Collections.Generic.List[object]]::new()
$duplicateCount = 0
$invalidCount = 0
$invalidLines = [System.Collections.Generic.List[string]]::new()

foreach ($line in Get-Content -LiteralPath $InputPath -Encoding UTF8) {
    if ([string]::IsNullOrWhiteSpace($line) -or $line -match "^\[Current count,") {
        continue
    }

    $patternText = Convert-ToPatternText $line
    $slots = @(Get-Slots $patternText)
    if ([string]::IsNullOrWhiteSpace($patternText) -or $slots.Count -eq 0) {
        $invalidCount++
        $invalidLines.Add($line)
        continue
    }

    $canonical = Get-CanonicalText $patternText
    if ($known.Contains($canonical) -or -not $seen.Add($canonical)) {
        $duplicateCount++
        continue
    }

    $index = $records.Count + 1
    $record = [ordered]@{
        id = "pattern-submitted-snowclone-20260727-$($index.ToString('0000'))"
        recordType = "pattern-record"
        patternText = $patternText
        normalizedText = $canonical
        sourceCategory = "snowclone"
        sourceId = "source-submitted-snowclones-2026-07-27-v2"
        sourceName = "User-Submitted Snowclones 2026-07-27"
        sourceLicense = "unknown"
        collectionMethod = "user-submission"
        storesExactSourceText = $true
        derivedFromExactPhrase = $true
        sourceTier = "tier-4"
        recommendedUsageMode = "pattern-extraction-only"
        collectionRisk = "high"
        commercialUseRisk = "high"
        transformationPotential = "very-high"
        patternExtractionPotential = "very-high"
        structuralExtractionPotential = "very-high"
        directUseAllowed = $false
        requiresReviewBeforeUse = $true
        requiresAttribution = $false
        reviewStatus = "needs-review"
        categories = @("snowclone", "user-submission", "cultural-reference", "slot-template")
        creativeIntents = @("humor", "recognition", "transformation")
        templateFamilies = @("Snowclone")
        slots = $slots
        provenance = [ordered]@{
            origin = "Snowclone list supplied directly by the user on 2026-07-27."
            retrievedAt = $timestamp
            evidence = @("Imported from the user-provided pasted-text attachment and normalized into explicit placeholder syntax.")
            confidence = 0.75
        }
        notes = "Unverified submitted structure. It may derive from a film quote, title, slogan, meme, lyric, proverb, or other culturally recognizable source. Use only for structural extraction after provenance, legal-risk, and originality review."
        createdAt = $timestamp
        updatedAt = $timestamp
    }
    $records.Add($record)
}

$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
$lines = @($records | ForEach-Object { $_ | ConvertTo-Json -Depth 10 -Compress })
[System.IO.File]::WriteAllLines($outputPath, $lines, $utf8NoBom)

Write-Output "Wrote $($records.Count) unique submitted snowclone records to $outputPath"
Write-Output "Skipped $duplicateCount duplicate records and $invalidCount records without recognizable slots"
if ($invalidLines.Count -gt 0) {
    Write-Output "Lines without recognizable slots: $($invalidLines -join ' | ')"
}
