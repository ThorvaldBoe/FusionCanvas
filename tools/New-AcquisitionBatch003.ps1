param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = "Stop"
$timestamp = "2026-07-27T00:00:00Z"
$patternPath = Join-Path $RepositoryRoot "data\phrase-intelligence\patterns\acquisition-batch-003.patterns.v2.jsonl"
$sourcePath = Join-Path $RepositoryRoot "data\phrase-intelligence\sources\acquisition-batch-003.sources.v2.jsonl"

$groups = @(
    [ordered]@{
        Key = "collection"
        SourceId = "source-collection-hobby-excess-batch-003-v2"
        SourceName = "Collection And Hobby Excess Acquisition Batch 003"
        SourceCategory = "everyday-expression"
        Categories = @("everyday-expression", "internal-generation", "batch-003", "collection-hobby-excess", "cross-niche")
        CreativeIntents = @("collection-humor", "identity", "enthusiasm", "hobby")
        TemplateFamilies = @("Collection Count", "Hobby Excess", "Supply Stash", "One More")
        Notes = "Internally authored collection, hobby-excess, supply-stash, project-queue, and one-more pattern structures. No external phrase scraping or exact source text used."
        Patterns = @(
            ,@("Just One More [OBJECT]", "One More", @("collection-humor", "enthusiasm"), @("Just One More Houseplant", "Just One More Board Game"), @("one-more", "object"))
            ,@("My [OBJECT] Collection Has Room For One More", "Collection Capacity", @("collection-humor", "identity"), @("My Mug Collection Has Room For One More", "My Puzzle Collection Has Room For One More"), @("collection", "capacity"))
            ,@("[NUMBER] [OBJECTS] And Counting", "Collection Count", @("collection", "status"), @("Thirty Houseplants And Counting", "Twelve Sketchbooks And Counting"), @("count", "progress"))
            ,@("I Came For [NEED], Left With [EXTRA]", "Acquisition Escalation", @("humor", "shopping"), @("I Came For Thread, Left With Fabric", "I Came For Seeds, Left With Three Pots"), @("shopping", "escalation"))
            ,@("[ACTIVITY] Supplies Are My Love Language", "Supply Affection", @("hobby", "sentiment"), @("Painting Supplies Are My Love Language", "Baking Supplies Are My Love Language"), @("supplies", "sentiment"))
            ,@("Powered By [FUEL] And New [OBJECTS]", "Hobby Fuel", @("routine", "collection-humor"), @("Powered By Tea And New Books", "Powered By Coffee And New Patterns"), @("fuel", "acquisition"))
            ,@("Saving Space For More [OBJECTS]", "Collection Capacity", @("collection", "anticipation"), @("Saving Space For More Records", "Saving Space For More Succulents"), @("space", "collection"))
            ,@("My [ACTIVITY] Hobby Needs Its Own [PLACE]", "Hobby Expansion", @("hobby", "humor"), @("My Sewing Hobby Needs Its Own Room", "My Cycling Hobby Needs Its Own Garage"), @("space", "hobby"))
            ,@("The [OBJECT] Stash Is Never Full", "Supply Stash", @("collection-humor", "abundance"), @("The Yarn Stash Is Never Full", "The Snack Stash Is Never Full"), @("stash", "abundance"))
            ,@("Collecting [OBJECTS] Is A Full-Time Hobby", "Hobby Identity", @("identity", "collection-humor"), @("Collecting Cookbooks Is A Full-Time Hobby", "Collecting Trail Maps Is A Full-Time Hobby"), @("collection", "identity"))
            ,@("I Organize My [OBJECTS] By [METHOD]", "Collection Method", @("organization", "humor"), @("I Organize My Books By Mood", "I Organize My Spices By Adventure Level"), @("organization", "method"))
            ,@("Minimalism, Except For [OBJECTS]", "Selective Excess", @("contrast", "collection-humor"), @("Minimalism, Except For Plants", "Minimalism, Except For Art Supplies"), @("contrast", "exception"))
            ,@("[OBJECTS]: Because One Is A Lonely Number", "Collection Rationale", @("humor", "collection"), @("Cameras: Because One Is A Lonely Number", "Blankets: Because One Is A Lonely Number"), @("rationale", "collection"))
            ,@("My Next [OBJECT] Is Already On The List", "Acquisition Queue", @("anticipation", "collection"), @("My Next Cookbook Is Already On The List", "My Next Fishing Rod Is Already On The List"), @("queue", "anticipation"))
            ,@("[ACTION] First, Count Supplies Later", "Hobby Priority", @("priority", "humor"), @("Create First, Count Supplies Later", "Bake First, Count Supplies Later"), @("priority", "supplies"))
            ,@("[OBJECT] Budget: [STATUS]", "Hobby Budget", @("humor", "status"), @("Book Budget: Creatively Flexible", "Plant Budget: Under Review"), @("budget", "status"))
            ,@("My Cart Has [NUMBER] More [OBJECTS]", "Acquisition Queue", @("shopping", "collection-humor"), @("My Cart Has Three More Patterns", "My Cart Has Five More Seed Packets"), @("cart", "count"))
            ,@("[OBJECTS] Before [OBLIGATION]", "Hobby Priority", @("priority", "humor"), @("Puzzles Before Paperwork", "Plants Before Laundry"), @("priority", "contrast"))
            ,@("Every [OBJECT] Has A Story", "Collection Story", @("sentiment", "collection"), @("Every Postcard Has A Story", "Every Tool Has A Story"), @("story", "collection"))
            ,@("Future [ACTIVITY] Project, Currently Gathering Supplies", "Project Queue", @("anticipation", "hobby"), @("Future Quilting Project, Currently Gathering Supplies", "Future Garden Project, Currently Gathering Supplies"), @("project", "supplies"))
        )
    }
    [ordered]@{
        Key = "routine"
        SourceId = "source-routine-fuel-batch-003-v2"
        SourceName = "Routine And Fuel Acquisition Batch 003"
        SourceCategory = "everyday-expression"
        Categories = @("everyday-expression", "internal-generation", "batch-003", "routine-fuel", "cross-niche")
        CreativeIntents = @("routine", "fuel", "energy", "humor")
        TemplateFamilies = @("Fuel First", "Routine Loop", "Energy Status", "Recharge")
        Notes = "Internally authored morning-routine, beverage-fuel, snack-fuel, recharge, and shift-survival pattern structures. No external phrase scraping or exact source text used."
        Patterns = @(
            ,@("First [FUEL], Then [ACTIVITY]", "Fuel First", @("routine", "priority"), @("First Coffee, Then Coding", "First Tea, Then Gardening"), @("fuel", "sequence"))
            ,@("[FUEL] Before [OBLIGATION]", "Fuel First", @("priority", "humor"), @("Breakfast Before Email", "Coffee Before Meetings"), @("fuel", "obligation"))
            ,@("Today's Plan: [FUEL], [ACTIVITY], Repeat", "Routine Loop", @("routine", "humor"), @("Today's Plan: Tea, Read, Repeat", "Today's Plan: Snacks, Hike, Repeat"), @("plan", "repeat"))
            ,@("Running On [FUEL] And [TRAIT]", "Energy Source", @("identity", "energy"), @("Running On Coffee And Curiosity", "Running On Toast And Determination"), @("fuel", "trait"))
            ,@("[TIME] Mode Requires [FUEL]", "Routine Requirement", @("routine", "humor"), @("Morning Mode Requires Coffee", "Night Shift Mode Requires Snacks"), @("time", "requirement"))
            ,@("Refuel, Reset, [ACTION]", "Recharge", @("energy", "encouragement"), @("Refuel, Reset, Create", "Refuel, Reset, Continue"), @("reset", "action"))
            ,@("[FUEL] Is Part Of The Process", "Process Fuel", @("routine", "identity"), @("Tea Is Part Of The Process", "Trail Mix Is Part Of The Process"), @("process", "fuel"))
            ,@("Fueled For [ACTIVITY]", "Activity Fuel", @("energy", "activity"), @("Fueled For Teaching", "Fueled For Trail Running"), @("fuel", "activity"))
            ,@("One [FUEL] Away From [STATE]", "Fuel Threshold", @("humor", "anticipation"), @("One Coffee Away From Functional", "One Snack Away From Cheerful"), @("threshold", "state"))
            ,@("[TIME] Starts After [FUEL]", "Routine Start", @("routine", "boundary"), @("Morning Starts After Tea", "Planning Starts After Breakfast"), @("time", "sequence"))
            ,@("Sip, [ACTION], Repeat", "Routine Loop", @("routine", "encouragement"), @("Sip, Sketch, Repeat", "Sip, Solve, Repeat"), @("repeat", "action"))
            ,@("[FUEL] In, [OUTPUT] Out", "Input Output", @("cause-effect", "humor"), @("Coffee In, Code Out", "Tea In, Stories Out"), @("input-output", "fuel"))
            ,@("Current Fuel Level: [STATUS]", "Energy Status", @("status", "humor"), @("Current Fuel Level: Nearly Ready", "Current Fuel Level: Refill Requested"), @("status", "fuel"))
            ,@("Add [FUEL] To Continue", "Fuel Prompt", @("instruction", "humor"), @("Add Coffee To Continue", "Add Cookies To Continue"), @("prompt", "fuel"))
            ,@("Emergency [FUEL] Reserve", "Fuel Reserve", @("preparedness", "humor"), @("Emergency Chocolate Reserve", "Emergency Tea Reserve"), @("reserve", "fuel"))
            ,@("Built On [FUEL] And [VALUE]", "Energy Source", @("identity", "value"), @("Built On Coffee And Kindness", "Built On Soup And Patience"), @("fuel", "value"))
            ,@("My Routine Has Three Steps: [STEP_ONE], [STEP_TWO], [STEP_THREE]", "Routine Stack", @("routine", "list"), @("My Routine Has Three Steps: Brew, Plan, Begin", "My Routine Has Three Steps: Stretch, Walk, Recharge"), @("steps", "routine"))
            ,@("Wake, [ACTION], Recharge", "Daily Cycle", @("routine", "sequence"), @("Wake, Create, Recharge", "Wake, Wander, Recharge"), @("cycle", "action"))
            ,@("[ACTIVITY] Runs On [FUEL]", "Activity Fuel", @("cause-effect", "identity"), @("Gardening Runs On Tea", "Game Night Runs On Popcorn"), @("activity", "fuel"))
            ,@("A Little [FUEL], A Lot Of [OUTCOME]", "Fuel Amplifier", @("contrast", "energy"), @("A Little Coffee, A Lot Of Focus", "A Little Cocoa, A Lot Of Cheer"), @("contrast", "outcome"))
        )
    }
    [ordered]@{
        Key = "place"
        SourceId = "source-place-escape-batch-003-v2"
        SourceName = "Place And Escape Acquisition Batch 003"
        SourceCategory = "everyday-expression"
        Categories = @("everyday-expression", "internal-generation", "batch-003", "place-escape", "cross-niche")
        CreativeIntents = @("place", "escape", "belonging", "adventure")
        TemplateFamilies = @("Happy Place", "Escape Plan", "Destination", "Place Identity")
        Notes = "Internally authored happy-place, destination, escape, belonging, outdoors, and hobby-setting pattern structures. No external phrase scraping or exact source text used."
        Patterns = @(
            ,@("Meet Me At The [PLACE]", "Destination", @("place", "invitation"), @("Meet Me At The Library", "Meet Me At The Campsite"), @("invitation", "place"))
            ,@("My Best Ideas Start In The [PLACE]", "Creative Place", @("place", "creativity"), @("My Best Ideas Start In The Garden", "My Best Ideas Start In The Workshop"), @("creativity", "place"))
            ,@("Mentally At The [PLACE]", "Mental Escape", @("escape", "humor"), @("Mentally At The Beach", "Mentally At The Bookshop"), @("escape", "place"))
            ,@("[PLACE] Time Is Well Spent", "Place Value", @("place", "sentiment"), @("Garden Time Is Well Spent", "Studio Time Is Well Spent"), @("time", "place"))
            ,@("Take Me Back To The [PLACE]", "Return Destination", @("nostalgia", "place"), @("Take Me Back To The Lake", "Take Me Back To The Kitchen"), @("return", "place"))
            ,@("[PLACE] Is My Reset Button", "Happy Place", @("escape", "wellbeing"), @("The Trail Is My Reset Button", "The Library Is My Reset Button"), @("reset", "place"))
            ,@("Find Me Where [FEATURE]", "Location Feature", @("place", "discovery"), @("Find Me Where The Pines Begin", "Find Me Where The Bread Is Warm"), @("feature", "discovery"))
            ,@("Home Is Wherever [ACTIVITY]", "Place Belonging", @("belonging", "activity"), @("Home Is Wherever We Cook", "Home Is Wherever Dogs Nap"), @("home", "activity"))
            ,@("Escape Plan: [DESTINATION]", "Escape Plan", @("escape", "anticipation"), @("Escape Plan: Mountain Cabin", "Escape Plan: Corner Bookshop"), @("plan", "destination"))
            ,@("Out Of Office, Into The [PLACE]", "Work Escape", @("escape", "contrast"), @("Out Of Office, Into The Woods", "Out Of Office, Into The Garden"), @("work", "escape"))
            ,@("[PLACE] Bound", "Destination", @("travel", "anticipation"), @("Beach Bound", "Trail Bound"), @("travel", "place"))
            ,@("Next Stop: [DESTINATION]", "Destination", @("travel", "anticipation"), @("Next Stop: The Campsite", "Next Stop: The Farmers Market"), @("travel", "destination"))
            ,@("Less [STRESS], More [PLACE]", "Place Contrast", @("escape", "contrast"), @("Less Traffic, More Trail", "Less Noise, More Garden"), @("contrast", "place"))
            ,@("Leave The [STRESS], Find The [FEATURE]", "Escape Direction", @("escape", "encouragement"), @("Leave The Noise, Find The Pines", "Leave The Rush, Find The River"), @("direction", "feature"))
            ,@("My Compass Points To [PLACE]", "Place Identity", @("place", "identity"), @("My Compass Points To The Coast", "My Compass Points To The Campsite"), @("compass", "place"))
            ,@("Taking The Long Way To [DESTINATION]", "Scenic Route", @("adventure", "travel"), @("Taking The Long Way To The Lake", "Taking The Long Way To Brunch"), @("route", "destination"))
            ,@("[ACTIVITY] Is My Favorite Detour", "Happy Detour", @("activity", "escape"), @("Book Browsing Is My Favorite Detour", "Creek Walking Is My Favorite Detour"), @("detour", "activity"))
            ,@("This Way To [PLACE]", "Directional Sign", @("place", "instruction"), @("This Way To The Garden", "This Way To The Game Room"), @("direction", "place"))
            ,@("Weekend Forecast: [PLACE] With A Chance Of [ACTIVITY]", "Weekend Forecast", @("humor", "anticipation"), @("Weekend Forecast: Kitchen With A Chance Of Baking", "Weekend Forecast: Trail With A Chance Of Picnics"), @("forecast", "activity"))
            ,@("Keep Close To [FEATURE]", "Place Reminder", @("place", "sentiment"), @("Keep Close To The Trees", "Keep Close To The Coffee Pot"), @("feature", "sentiment"))
        )
    }
)

$slotDescriptions = @{
    ACTION = "An action, verb phrase, or preferred behavior."
    ACTIVITY = "A hobby, routine, job task, sport, or recurring activity."
    DESTINATION = "A destination, venue, setting, or imagined getaway."
    EXTRA = "An additional object, supply, purchase, or unexpected quantity."
    FEATURE = "A landscape feature, sensory detail, landmark, or defining quality."
    FUEL = "A beverage, snack, meal, rest ritual, or playful energy source."
    METHOD = "An organizing rule, category, mood, system, or playful method."
    NEED = "The item, supply, errand, or goal that began the trip."
    NUMBER = "A number, count, or quantity."
    OBJECT = "A collectible, tool, supply, comfort item, or niche object."
    OBJECTS = "Plural collectibles, tools, supplies, comfort items, or niche objects."
    OBLIGATION = "A duty, chore, meeting, deadline, or unwanted responsibility."
    OUTCOME = "A result, state, benefit, creation, or playful consequence."
    OUTPUT = "A produced result, creative work, completed task, or behavior."
    PLACE = "A room, venue, landscape, workspace, hobby setting, or destination."
    ROLE = "A job, family identity, hobby identity, or social role."
    STATUS = "A short status, capacity level, condition, or playful assessment."
    STEP_ONE = "The first action or stage in a routine."
    STEP_TWO = "The second action or stage in a routine."
    STEP_THREE = "The third action or stage in a routine."
    STATE = "A mood, energy level, condition, or operating state."
    STRESS = "A pressure, distraction, unwanted condition, or thing to leave behind."
    TIME = "A time of day, shift, occasion, or recurring period."
    TRAIT = "A personal quality, attitude, value, or playful attribute."
    VALUE = "A quality, principle, attitude, or emotional resource."
}

function Get-NormalizedText([string]$text) {
    $normalized = $text.ToLowerInvariant()
    $normalized = $normalized -replace "[,:;.!?']", ""
    return ($normalized -replace "\s+", " ").Trim()
}

function Get-Slots([string]$text) {
    $matches = [regex]::Matches($text, "\[([A-Z][A-Z_]*)\]")
    $names = @($matches | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique)
    return @($names | ForEach-Object {
        $description = $slotDescriptions[$_]
        if (-not $description) { $description = "A replaceable value appropriate to the selected niche." }
        [ordered]@{
            name = $_
            type = $_.ToLowerInvariant()
            required = $true
            description = $description
        }
    })
}

function New-SourceRecord($group) {
    return [ordered]@{
        recordType = "source-record"
        sourceLicense = "internal"
        collectionMethod = "internal-generation"
        storesExactSourceText = $false
        derivedFromExactPhrase = $false
        sourceTier = "tier-1"
        recommendedUsageMode = "direct-collection"
        collectionRisk = "low"
        commercialUseRisk = "low"
        transformationPotential = "very-high"
        patternExtractionPotential = "very-high"
        structuralExtractionPotential = "very-high"
        directUseAllowed = $false
        requiresReviewBeforeUse = $true
        requiresAttribution = $false
        reviewStatus = "candidate"
        createdAt = $timestamp
        updatedAt = $timestamp
        id = $group.SourceId
        sourceCategory = $group.SourceCategory
        sourceName = $group.SourceName
        normalizedText = (Get-NormalizedText $group.SourceName)
        categories = $group.Categories
        creativeIntents = $group.CreativeIntents
        templateFamilies = $group.TemplateFamilies
        provenance = [ordered]@{
            origin = "Internal acquisition batch 003 authored as reusable generic phrase mechanics; no external scraping performed."
            retrievedAt = $timestamp
            evidence = @(
                "Patterns store generalized slot structures only."
                "Examples are original illustrative transformations, not approved product copy."
            )
            confidence = 0.9
        }
        notes = $group.Notes
    }
}

function New-PatternRecord($group, [int]$index, $definition) {
    $text = $definition[0]
    $family = $definition[1]
    $intents = @($definition[2])
    $examples = @($definition[3])
    $tags = @($definition[4])

    return [ordered]@{
        recordType = "pattern-record"
        sourceLicense = "internal"
        collectionMethod = "internal-generation"
        storesExactSourceText = $false
        derivedFromExactPhrase = $false
        sourceTier = "tier-1"
        recommendedUsageMode = "direct-collection"
        collectionRisk = "low"
        commercialUseRisk = "low"
        transformationPotential = "very-high"
        patternExtractionPotential = "very-high"
        structuralExtractionPotential = "very-high"
        directUseAllowed = $false
        requiresReviewBeforeUse = $true
        requiresAttribution = $false
        reviewStatus = "candidate"
        createdAt = $timestamp
        updatedAt = $timestamp
        id = "pattern-batch-003-$($group.Key)-$($index.ToString('0000'))"
        patternText = $text
        normalizedText = (Get-NormalizedText $text)
        sourceCategory = $group.SourceCategory
        sourceId = $group.SourceId
        sourceName = $group.SourceName
        categories = @($group.SourceCategory, "batch-003", "cross-niche", "slot-template") + $tags
        creativeIntents = $intents
        templateFamilies = @($family)
        slots = @(Get-Slots $text)
        exampleTransformations = @($examples | ForEach-Object {
            [ordered]@{
                text = $_
                commercialUseRisk = "low"
                requiresReviewBeforeUse = $true
                notes = "Generated internal example for structure illustration only; not approved product copy."
            }
        })
        provenance = [ordered]@{
            origin = "Internal acquisition batch 003 authored as a reusable generic phrase pattern; no external scraping performed."
            retrievedAt = $timestamp
            evidence = @(
                "Pattern is stored as a generalized slot structure."
                "Examples are original cross-niche transformations for validation only."
            )
            confidence = 0.9
        }
        notes = "Internally authored batch 003 reusable phrase mechanic; review generated adaptations for originality and marketplace suitability before product use."
    }
}

$sourceRecords = @($groups | ForEach-Object { New-SourceRecord $_ })
$patternRecords = @()
foreach ($group in $groups) {
    for ($index = 0; $index -lt $group.Patterns.Count; $index++) {
        $patternRecords += New-PatternRecord $group ($index + 1) $group.Patterns[$index]
    }
}

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$sourceLines = @($sourceRecords | ForEach-Object { $_ | ConvertTo-Json -Depth 10 -Compress })
$patternLines = @($patternRecords | ForEach-Object { $_ | ConvertTo-Json -Depth 10 -Compress })
[System.IO.File]::WriteAllLines($sourcePath, $sourceLines, $utf8NoBom)
[System.IO.File]::WriteAllLines($patternPath, $patternLines, $utf8NoBom)

Write-Output "Wrote $($sourceRecords.Count) source records to $sourcePath"
Write-Output "Wrote $($patternRecords.Count) pattern records to $patternPath"
