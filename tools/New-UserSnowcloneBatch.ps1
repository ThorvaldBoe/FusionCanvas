[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = "Stop"

$timestamp = "2026-07-27T00:00:00Z"
$sourceId = "source-user-snowclones-2026-07-27-v2"
$sourceName = "User Snowclone Submission 2026-07-27"

$patterns = @(
    @{ Text = "A Second [X] Has Hit The [Y]"; Note = "Shock-announcement frame derived from reporting of the September 11 attacks." },
    @{ Text = "Save A [X], [ACTION] A [Z]"; Note = "Imperative substitution frame popularized by Save a Horse (Ride a Cowboy)." },
    @{ Text = "2[TRAIT]4Me"; Note = "Compressed internet form of 'too [trait] for me'; the affectionate variant 3[TRAIT]5Me is related." },
    @{ Text = "[CLAIMANT] Lied, [GROUP] Died"; Note = "Rhyming political accusation frame." },
    @{ Text = "From [PLACE] With [X]"; Note = "Title substitution frame associated with From Russia with Love." },
    @{ Text = "[THING]y Mc[THING]face"; Note = "Comic naming frame popularized by Boaty McBoatface." },
    @{ Text = "I'm Looking At You, [X]"; Note = "Parenthetical callout used to identify a salient example." },
    @{ Text = "Make [X] [STATE] Again"; Note = "Campaign-slogan substitution frame." },
    @{ Text = "The Real [X] Is The Friends We Made Along The Way"; Note = "Anticlimactic journey-and-friendship reveal frame." },
    @{ Text = "There's A [X] For That"; Note = "Availability claim derived from the advertising slogan 'There's an app for that'." },
    @{ Text = "[X] Is Love, [X] Is Life"; Note = "Repetitive devotion frame popularized by a 2013 internet meme." },
    @{ Text = "[X]? On My [Y]?"; Note = "Incredulous question frame, often followed by 'it's more likely than you think'." },
    @{ Text = "You Wouldn't [ACTION] A [THING]"; Note = "Anti-piracy parody frame; includes the 'you wouldn't download [thing]' variant." },
    @{ Text = "It's The [X] For Me"; Note = "Evaluative social-media frame popularized by the For Me Challenge." },
    @{ Text = "[SUPERLATIVE] [TRAIT] [SUBJECT] In [PLACE]"; Note = "Sarcastic 'most/least [trait] [subject]' ranking frame; the place clause may be omitted." },
    @{ Text = "[QUESTION] - The Greatest Thread In The History Of Forums, Locked By A Moderator After [COUNT] Pages Of Heated Debate"; Note = "Mock forum-history frame derived from a dril post." },
    @{ Text = "You Do Not, Under Any Circumstances, Gotta Hand It To [X]"; Note = "Categorical refusal-to-credit frame derived from a dril post." },
    @{ Text = "My ""[DENIAL]"" T-Shirt Has People Asking A Lot Of Questions Already Answered By My Shirt"; Note = "Over-specific denial frame derived from a Mike Ginn post." },
    @{ Text = "Well, Well, Well, Not So Easy To [ACTION] A [THING] That Doesn't Suck, Huh?"; Note = "Vindicated-predecessor frame associated with The Onion." },
    @{ Text = "[PERSON] Is Not Suicidal"; Note = "Pre-emptive denial frame used around controversial disclosures." },
    @{ Text = "She [ACTION] On My [NOUN] Till I [RESULT]"; Note = "Innuendo frame built by segmenting a familiar name or phrase." },
    @{ Text = "Who Up [ACTION_PROGRESSIVE] They [NOUN]?"; Note = "Deliberately ungrammatical innuendo question frame." },
    @{ Text = "Hippity Hoppity [X] Is Property"; Note = "Rhyming ownership frame derived from 'hippity hoppity, get off my property'." },
    @{ Text = "Close Enough, Welcome Back [PERSON]"; Note = "Similarity callout frame that treats one person as another's return." },
    @{ Text = "The Lion Does Not Concern Himself With The Opinion Of [GROUP]"; Note = "Status-dismissal frame associated with Game of Thrones." },
    @{ Text = "[X] Is The [QUALIFIER] [CATEGORY] Of All Time"; Note = "Damning-with-faint-praise frame; QUALIFIER may be 'most' or omitted." },
    @{ Text = "You Just Created One Million [GROUP]s"; Note = "Backfire claim that an attack has multiplied support for its target." },
    @{ Text = "Everyone I Don't Like Is [LABEL]"; Note = "Mock reductive-labeling frame, originally circulated with Hitler as the label." },
    @{ Text = "Can You Feel The [X]?"; Note = "Rhetorical sensation frame; also appears as the imperative 'Feel the [X]'." },
    @{ Text = "Somehow, [PERSON] Returned"; Note = "Inexplicable-return frame from Star Wars: The Rise of Skywalker." },
    @{ Text = "[PERSON] Isn't Going To Sleep With You"; Note = "Retort to perceived obsessive praise or sycophancy." },
    @{ Text = "The [NOUN] I [VERB_PAST]"; Note = "Humorous intensifier whose noun and invented irregular past-tense verb share a root." },
    @{ Text = "Everyone Has [X], It Came Free With Your [Y]"; Note = "Argument frame popularized by a viral dispute about UNO on Xbox 360." },
    @{ Text = "[X] At A Cheap Price? Satisfactory"; Note = "Deadpan value-assessment frame." },
    @{ Text = "[X] Was Invented By [FIRST_NAME] [X] When He Tried To [ACTION] Twice At The Same Time"; Note = "Mock etymology frame in which an action is invented by doubling a simpler action." },
    @{ Text = "Yo Dawg, I Heard You Like [X], So I Put A [Y] In Your [Z] So You Can [ACTION] While You [ACTION_2]"; Note = "Recursive customization frame associated with Pimp My Ride." }
)

function Get-NormalizedText {
    param([Parameter(Mandatory)][string]$Text)

    return (($Text.ToLowerInvariant() -replace "'", "" -replace "\s+", " ").Trim())
}

function Get-Slots {
    param([Parameter(Mandatory)][string]$Text)

    $names = [regex]::Matches($Text, "\[([A-Z0-9_]+)\]") |
        ForEach-Object { $_.Groups[1].Value } |
        Select-Object -Unique

    $slots = @($names | ForEach-Object {
        [ordered]@{
            name = $_
            type = $_
            required = $true
            description = "A value substituted for the $_ slot."
        }
    })

    return ,$slots
}

$source = [ordered]@{
    id = $sourceId
    recordType = "source-record"
    sourceCategory = "snowclone"
    sourceName = $sourceName
    normalizedText = "user snowclone submission 2026-07-27"
    sourceUrl = "https://en.wiktionary.org/wiki/Appendix:Snowclones"
    sourceLicense = "external-reference"
    collectionMethod = "user-submission"
    storesExactSourceText = $false
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
    reviewStatus = "candidate"
    categories = @("snowclone", "user-submission", "internet-meme", "cultural-reference", "slot-template")
    creativeIntents = @("humor", "parody", "commentary", "identity")
    provenance = [ordered]@{
        origin = "User-provided excerpt from the Wiktionary Appendix:Snowclones 21st-century section."
        retrievedAt = $timestamp
        evidence = @(
            "The submission supplied linked template names and explanatory notes.",
            "Only generalized slot structures are retained; explanatory source prose is summarized."
        )
        confidence = 0.9
    }
    notes = "Recognizable meme, slogan, quote, and social-post structures. Use for pattern extraction only and review every adaptation for cultural, commercial, and platform risk."
    createdAt = $timestamp
    updatedAt = $timestamp
}

$records = for ($index = 0; $index -lt $patterns.Count; $index++) {
    $definition = $patterns[$index]
    [ordered]@{
        id = "pattern-user-snowclone-20260727-{0:D4}" -f ($index + 1)
        recordType = "pattern-record"
        patternText = $definition.Text
        normalizedText = Get-NormalizedText $definition.Text
        sourceCategory = "snowclone"
        sourceId = $sourceId
        sourceName = $sourceName
        sourceUrl = "https://en.wiktionary.org/wiki/Appendix:Snowclones"
        sourceLicense = "external-reference"
        collectionMethod = "user-submission"
        storesExactSourceText = $false
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
        reviewStatus = "candidate"
        categories = @("snowclone", "user-submission", "internet-meme", "cultural-reference", "slot-template")
        creativeIntents = @("humor", "parody", "commentary")
        templateFamilies = @("Snowclone", "Internet Culture")
        slots = Get-Slots $definition.Text
        provenance = [ordered]@{
            origin = "User-provided excerpt from the Wiktionary Appendix:Snowclones 21st-century section."
            retrievedAt = $timestamp
            evidence = @("Generalized pattern and origin note supplied by the user.")
            confidence = 0.85
        }
        notes = "$($definition.Note) Pattern-extraction candidate only; not approved product copy."
        createdAt = $timestamp
        updatedAt = $timestamp
    }
}

$sourcePath = Join-Path $RepositoryRoot "data/phrase-intelligence/sources/user-snowclones-2026-07-27.sources.v2.jsonl"
$patternPath = Join-Path $RepositoryRoot "data/phrase-intelligence/patterns/user-snowclones-2026-07-27.patterns.v2.jsonl"
$utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)

[System.IO.File]::WriteAllLines($sourcePath, @(($source | ConvertTo-Json -Depth 20 -Compress)), $utf8WithoutBom)
[System.IO.File]::WriteAllLines($patternPath, @($records | ForEach-Object { $_ | ConvertTo-Json -Depth 20 -Compress }), $utf8WithoutBom)

Write-Host "Wrote 1 source record to $sourcePath"
Write-Host "Wrote $($records.Count) pattern records to $patternPath"
