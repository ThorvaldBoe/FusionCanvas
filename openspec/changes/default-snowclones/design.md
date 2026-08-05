## Context

The `snowclone-library` module (active, complete) already delivers the mechanism for bundled starter content: an embedded UTF-8 `starter-snowclones.csv`, one-time atomic initialization on first open of a fresh data store (via `SnowcloneLibraryService.InitializeAsync`, invoked at app startup in `AppWorkspaceFactory`), permanent deletion, and explicit bundled re-import. That resource currently contains a single row: `Easily distracted by {X}`.

Issue 117 asks to deliver "a few" snowclones by default so generation is useful immediately. There is no migration/upgrade path — every installation initializes fresh — so changing the starter content is a pure content change on the existing path.

## Goals / Non-Goals

**Goals:**
- Ship a curated default set of 31 snowclones in the bundled starter library.
- Each default appears in the Snowclone Library as if imported, on the already-existing one-time initialization.
- Each default remains individually deletable permanently (no resurrection on later launches/builds).
- Explicit "Import bundled library" still merges the current build's set uniquely without overwriting local guidance.

**Non-Goals:**
- No schema or migration change; no new SQLite version.
- No new API, contract, service, or view-model change.
- No change to CSV interchange format or the management dialog.
- No changes to how ideation consumes snowclones, or to the OpenRouter/AI integration.
- No upgrade path that would push new defaults onto an already-initialized store (all stores are fresh).

## Decisions

**D1 — Starter content becomes a 31-record curated set (issue-authoritative).**
The default set is the list supplied in GitHub issue 117. Each row is authored under the same `Phrase,Guidance` contract and every phrase already satisfies `SnowcloneTemplatePolicy.Validate` (≥1 brace placeholder, no newlines, matched non-nested braces, non-empty placeholders).

The exact content to ship in `starter-snowclones.csv` (phrases and guidance):

| Phrase | Guidance |
|---|---|
| Keep Calm and {Action} | Replace {Action} with a specific, mildly obsessive activity or habit, such as `Keep Calm and Bake`, `Keep Calm and Game`, or `Keep Calm and Knit`. |
| Keep Calm, I'm a {Profession} | Replace {Profession} with a trade, hobby, or role, such as `mom`, `software engineer`, `dog dad`, or `home cook`. |
| I Paused My {Activity} to Be Here | Replace {Activity} with an absorbing activity or appointment, such as `Netflix`, `gaming`, or `gym`. |
| I'm Not Arguing, I'm a {Profession} | Replace {Profession} with a role known for pedantry, such as `engineer`, `lawyer`, `teacher`, or `accountant`. |
| I'm Not Arguing, I'm Just Explaining Why {Opinion} | Replace {Opinion} with a strongly held, humorous stance, such as `cereal is a soup` or `your way is wrong`. |
| I Can't, I Have {Activity} | Replace {Activity} with a plan, obligation, or absurd task, such as `plans`, `a date with my couch`, or `to pet my cat`. |
| Easily Distracted By {Interest} | Replace {Interest} with something the target audience is enthusiastically obsessed with, such as dogs, books, coffee, or gardening. |
| World's Okayest {Role} | Replace {Role} with a role or title, such as `Mom`, `Dad`, `Employee`, or `Golfer`. |
| Fueled By {Thing} and {Emotion} | Replace {Thing} with a consumable and {Emotion} with a driving feeling, such as `coffee` and `anxiety` or `tea` and `optimism`. |
| Powered By {Thing} | Replace {Thing} with an energy source or habit, such as `coffee`, `caffeine`, `spite`, or `deadlines`. |
| Professional {HumorousOccupation} | Replace {HumorousOccupation} with an unexpected or self-deprecating job label, such as `snack taster`, `napper`, or `couch athlete`. |
| I Work Hard So My {LovedOneOrPet} Can Have a Better Life | Replace {LovedOneOrPet} with a beloved person or pet, such as `cat`, `dog`, `kids`, or `goldfish`. |
| Straight Outta {PlaceOrTopic} | Replace {PlaceOrTopic} with a place, origin, or niche topic, such as `Campus`, `Deadlines`, or `Small Town`. |
| Born To {DreamActivity}, Forced To {Obligation} | Replace {DreamActivity} with a dream pursuit and {Obligation} with a mundane duty, such as `snooze` and `work` or `travel` and `meetings`. |
| Trust Me, I'm a {Profession} | Replace {Profession} with an authority role, used ironically, such as `mom`, `doctor`, or `certified snack expert`. |
| I'm Silently Correcting Your {MistakeType} | Replace {MistakeType} with a common error, such as `grammar`, `spelling`, or `math`. |
| I'm Not Lazy, I'm {HumorousExcuse} | Replace {HumorousExcuse} with a playful excuse, such as `on energy-saving mode`, `in my rest era`, or `between tasks`. |
| Eat. Sleep. {Activity}. Repeat. | Replace {Activity} with an activity repeated daily, such as `Game`, `Knit`, or `Workout`. |
| Life Is Better With {Thing} | Replace {Thing} with something that improves life, such as `Dogs`, `Coffee`, `Books`, or `Sunshine`. |
| Home Is Where My {BelovedThing} Is | Replace {BelovedThing} with a cherished possession or companion, such as `Dog`, `Plants`, `Books`, or `Heels`. |
| I Like Big {PluralObject} and I Cannot Lie | Replace {PluralObject} with a beloved plural object, such as `Books`, `Burritos`, `Paychecks`, or `Dogs`. |
| Warning: {HumorousWarning} | Replace {HumorousWarning} with a playful caution, such as `I Run on Coffee`, `May Talk About My Dog`, or `Hugger on Duty`. |
| I'm The Reason {HumorousConsequence} | Replace {HumorousConsequence} with an exaggerated outcome, such as `My Gym Is Still Open` or `The Snack Aisle Is Empty`. |
| {Thing} Because {Reason} | Replace {Thing} with a passion and {Reason} with a humorous justification, such as `Coffee Because I Have to Adult`. |
| Be Nice To Me, I Might Be Your {Profession} | Replace {Profession} with a current or future role, such as `Boss`, `Nurse`, or `Future Mother-in-Law`. |
| Everything I Need Is {LocationOrPossession} | Replace {LocationOrPossession} with a place or possession, such as `In My Bag`, `In My Garage`, or `In This Coffee`. |
| Everything I Need I Learned From {Source} | Replace {Source} with an unexpected teacher, such as `My Cat`, `The Office`, or `Bob Ross`. |
| Real {Role} {Action} | Replace {Role} with a role and {Action} with a verb phrase, such as `Real Men Cook` or `Real Athletes Stretch`. |
| Mess With {PersonOrThing}, {Consequence} | Replace {PersonOrThing} with a subject and {Consequence} with a playful consequence, such as `Mess With the Cat, Get the Claws`. |
| {Trait} Is Better Than {OppositeTrait} | Replace {Trait} and {OppositeTrait} with opposing traits, such as `Sleep Is Better Than Stress`. |
| {Thing} First. {FollowUpActivity} | Replace {Thing} with a priority and {FollowUpActivity} with what comes next, such as `Coffee First. Questions Later.` |

*Alternative:* add only a small handful (3–5). Rejected because the issue supplies a curated list of 31 and the mechanism makes the marginal cost of each record near-zero; a richer starter set gives generation meaningful variety on first run.

**D2 — Reuse the existing one-time initialization unchanged.**
`InitializeAsync` reads the whole bundled document, validates it, and atomically imports it once, setting the marker. Growing the file needs no code change.

**D3 — Update the single pinning resource test.**
`SnowcloneCsvCodecTests.EmbeddedStarterResource_UsesTheNormalCsvContract` currently asserts `Assert.Single` on the resource. It must assert the full set: count 31, presence of each curated phrase, representative guidance, and `SnowcloneTemplatePolicy.Validate(...).IsValid` for every decoded row.

**D4 — Rename the prior record to match the curated list.**
The previous single row `Easily distracted by {X}` becomes `Easily Distracted By {Interest}` in the curated set (same intent; case/pairing normalized to the list). Because all stores initialize fresh, there is no in-place rename concern for existing data.

## Risks / Trade-offs

- **[Any default phrase fails validation]** → Pinned resource test must validate every row via the normal CSV contract so an invalid default fails the baseline before shipping. All 31 phrases were checked against the placeholder policy.
- **[Test drift / stale `Assert.Single`]** → The resource test is rewritten to assert the exact curated set; the count assertion catches accidental truncation or addition.
- **[Duplicate default phrases]** → All 31 normalize to distinct duplicate keys; the initialization import skips duplicates atomically and the resource test asserts the exact set, guarding against accidental repeats.
- **[Content curations may feel subjective]** → The set is issue-authoritative (GitHub issue 117); changes to wording are a product decision captured here, not left to implementation.

## UX Note

Not applicable at the interaction level: the Snowclone Library dialog, its states, focus, and destructive-confirmation behavior are already specified and implemented by `snowclone-library`. This change only alters the data that surfaces through the existing list, so no UI/UX guideline work or headless view-test change is required.

## Migration Plan

No migration. No rollback beyond reverting the resource file; because the resource is read at first-open initialization, reverting changes only affects stores not yet initialized. No compatibility concern for the shared SQLite v6/v7 schema.

## Implementation Plan

Layers affected: **Integration resource** and **Integration tests** only. No Domain, Application, or App code changes.

1. **Edit the embedded resource** `src/FusionCanvas.Integration/Snowclones/Resources/starter-snowclones.csv`:
   - Keep the header row `Phrase,Guidance`.
   - Replace the single existing data row with the 31 data rows from D1, in the order shown.
   - Keep the file UTF-8; no change to `EmbeddedBundledSnowcloneSource.ResourceName` or the `.csproj` embedded-resource configuration (already configured).
   - Follow RFC 4180 quoting: any field containing a comma, quote, CR, or LF must be quoted (matching the existing single row's style). Several phrases and nearly all guidance strings contain commas/apostrophes, so the author must not emit unquoted commas or they will silently mis-split rows. The pinning test decodes through the codec, so quoting errors fail the baseline.
2. **Update the pinning test** `tests/FusionCanvas.Integration.Tests/Snowclones/SnowcloneCsvCodecTests.cs` `EmbeddedStarterResource_UsesTheNormalCsvContract`:
   - Remove `Assert.Single`; assert `result.Rows.Count == 31`.
   - Assert the set of phrases equals the curated 31 (e.g., a case-insensitive comparison against the expected phrase list), and assert representative guidance (e.g., contains `Replace {X}`-style guidance) for at least one default.
   - For every decoded row, assert `SnowcloneTemplatePolicy.Validate(row.Phrase, row.Guidance).IsValid`, making the policy gate over the shipped starter set automated rather than relying on runtime rejection at initialization.
3. **Verify unchanged behavior** — no edits to `SnowcloneLibraryService` or view models; existing service/view-model tests use stub bundled sources and continue to pass unchanged.
4. **Regression run** — `dotnet test .\FusionCanvas.sln`.
5. **OpenSpec validation** — `openspec validate default-snowclones --strict` plus repository-required validation scope.

### Acceptance-to-verification mapping

| Scenario | Planned verification method |
|---|---|
| Snowclone library initializes for the first time (full curated set, as-if-imported, marker persisted) | `SnowcloneCsvCodecTests.EmbeddedStarterResource_UsesTheNormalCsvContract` (resource = full set, every row policy-valid); existing `InitializeAsync_ImportsOnceAndPersistsMarker` (service behavior unchanged) |
| Creator deletes an initialized default record (no resurrection) | Existing `InitializeAsync_AfterStarterDeletionDoesNotResurrectIt` (service logic unchanged) |
| Creator imports the bundled library explicitly | Existing `ImportBundledAsync_AddsUniqueAndPreservesExistingGuidance` (service logic unchanged) |
| Bundled starter data is invalid (no partial import, no marker) | Existing `InitializeAsync_InvalidBundleDoesNotSaveOrSetMarker` (service logic unchanged) |

### Decisions not to reopen

- The exact 31 phrases and guidance (issue-authoritative).
- That a fresh store initializes all 31 defaults via the existing one-time init (no per-store upgrade overlay).
- That no service, view-model, schema, or migration change is made for these defaults.
