# Phrase Pattern Database Generic Design Principles

## Purpose

This document clarifies the product direction for the FusionCanvas Phrase Pattern Database: it is a generic phrase-pattern system, not a fantasy, tabletop role-playing game, or any other niche-specific phrase library.

The database should help FusionCanvas identify reusable language mechanics that can be adapted across many audiences, products, and creative briefs. Fantasy and TTRPG examples may remain useful as sample adaptations, regression tests, and demonstration content, but they must not define the core schema, collection priorities, or template taxonomy.

Related documents:

```text
docs/phrase-pattern-database-specification.md
docs/phrase-pattern-database-collection-strategy.md
docs/phrase-pattern-database-source-research.md
data/phrase-patterns/source-candidates.jsonl
data/phrase-patterns/manual-seed-phrases.jsonl
```

---

## Core Position

The Phrase Pattern Database is niche-agnostic.

A core record should describe a reusable phrase structure such as:

```text
Born To [ACTION], Forced To [OBLIGATION]
```

It should not be stored as though one niche-specific adaptation is the canonical pattern:

```text
Born To Roll Dice, Forced To Work
```

The first record is a generic structure that can support professions, hobbies, sports, pets, parenting, gaming, gardening, coffee, fitness, cars, and many other niches. The second phrase is only one possible adaptation of that structure.

---

## Design Principles

## 1. Store Generic Patterns As The Core Asset

The highest-value database asset is the reusable mechanic behind a phrase, not a single niche joke.

Core phrase records should prioritize:

- Replaceable slot structure
- Reusable grammar
- Tone and design mechanics
- Common layout behavior
- Adaptability across unrelated niches
- Legal and originality risk at the structural level

Core phrase records should avoid baking in niche nouns unless the niche noun is necessary to explain the pattern.

### Preferred core pattern

```text
My [THING] Is My [BENEFIT]
```

### Niche adaptations

```text
My Garden Is My Therapy
My Dog Is My Cardio
My Coffee Is My Personality
My Paintbrush Is My Escape
My Long Run Is My Reset
```

The reusable record is the pattern. The adaptations are examples or separate adaptation records.

---

## 2. Treat Fantasy And TTRPG As One Test Niche Among Many

Fantasy and TTRPG phrases are useful because they contain clear roles, actions, artifacts, and humorous contrasts. However, they should be treated as one niche among many, not as the system's default domain.

Acceptable uses of fantasy/TTRPG examples:

- Demonstrating that a template can adapt to gaming language
- Testing slot replacement behavior
- Comparing tone across niches
- Verifying that niche-specific adaptations can be stored separately
- Providing sample outputs in a mixed set of examples

Unacceptable uses:

- Naming generic slot types after fantasy concepts only
- Making template families depend on TTRPG mechanics
- Using fantasy examples as the majority of every template record
- Treating fantasy phrases as canonical core records when a generic structure exists

A template family can include a fantasy adaptation such as:

```text
Fueled By Dice And Snacks
```

But it should sit beside adaptations such as:

```text
Fueled By Coffee And Deadlines
Fueled By Miles And Music
Fueled By Soil And Sunshine
Fueled By Treats And Tail Wags
```

---

## 3. Separate Generic Patterns From Niche-Specific Adaptations

Where possible, the database should separate structural records from niche-specific outputs.

Recommended separation:

1. **Template family record** — describes the generic language mechanic.
2. **Core phrase pattern record** — stores a reusable template form and slot schema.
3. **Niche adaptation record** — stores a generated or manually curated adaptation for a target niche.
4. **Design concept record** — stores visual layout, icon, and product-specific treatment.

This separation keeps the database flexible. A generic template can serve dozens of niches without duplicating the same structural metadata.

### Example

Generic template family:

```text
I Would Rather Be [ACTIVITY]
```

Adaptations:

```text
I Would Rather Be Fishing
I Would Rather Be Baking
I Would Rather Be Lifting
I Would Rather Be Gardening
I Would Rather Be Gaming
I Would Rather Be Walking The Dog
```

The family should not be renamed around any one of those activities.

---

## 4. Use Generic Slot Types

Slot types should be broad enough to work across many niches while still being specific enough for quality generation.

Prefer slot types such as:

- `activity`
- `role`
- `object`
- `tool`
- `place`
- `event`
- `trait`
- `emotion`
- `constraint`
- `reward`
- `problem`
- `fallback_action`
- `preferred_state`
- `identity_label`
- `time_period`
- `companion`
- `consumable`
- `achievement`
- `warning_condition`
- `process_step`

Avoid overly niche slot types in core records, such as:

- `spell_name`
- `dice_type`
- `class_name`
- `quest_item`
- `boss_monster`

Niche-specific slot vocabularies can still exist, but they should map back to generic parent slot types.

Example mapping:

```json
{
  "genericSlotType": "tool",
  "niche": "tabletop gaming",
  "nicheSlotType": "dice"
}
```

This makes the system usable for professions, hobbies, pets, sports, family, gaming, fitness, cars, gardening, coffee, parenting, and future niches that have not been defined yet.

---

## 5. Keep Template Families Generic

Template families should describe language mechanics rather than topical content.

Good template-family names:

- Identity Declaration
- Preference Statement
- Contrast Pair
- Faux Certification
- Warning Label
- Survival Badge
- Fuel Source
- Priority Stack
- Cause And Effect
- Better With Companion
- Countdown Or Checklist
- Permission Slip
- Mood Forecast
- Boundary Statement
- Tiny Achievement

Poor template-family names for core records:

- Dice Goblin Phrases
- Wizard Work Jokes
- Dungeon Warning Labels
- Dragon Coffee Sayings
- Paladin Certification Templates

The second group may be useful for niche collections, but not for the core generic taxonomy.

---

## 6. Prioritize Phrase Mechanics During Source Research

Source research should prioritize phrase mechanics, not niche content.

A useful source teaches the system about structures such as:

- Replaceable identity labels
- Action-versus-obligation contrasts
- Repeated phrase frames
- Familiar sign or label formats
- Comparison structures
- Advice structures
- Mock credentials
- Achievement or survival framing
- Preference and priority statements
- Cause-and-effect jokes

A less useful source is merely a list of niche jokes with no reusable structure.

Research notes should ask:

- What pattern mechanics does this source expose?
- Are the structures reusable outside the source niche?
- Can the pattern be represented with generic slots?
- Is the legal risk tied to wording, source identity, or general structure?
- Should this be stored as a core pattern, a source note, or only inspiration?

The safest and most reusable records will usually come from generic structural analysis, public-domain or traditional expressions, internally authored templates, and heavily transformed mechanics rather than direct copying of modern protected text.

---

## 7. Keep Legal Risk Low By Avoiding Protected Expressions

Core records should not depend on exact song lyrics, movie quotes, TV quotes, advertising slogans, brand slogans, trademarked franchise language, or distinctive modern meme wording.

The system may record risk metadata and research notes about high-risk categories, but the generic template-family seed data should prefer low-risk mechanics.

Low-risk direction:

```text
[ROLE] In Progress
```

Higher-risk direction:

```text
A phrase that closely copies a specific lyric, character catchphrase, ad campaign, or franchise slogan
```

Legal risk metadata should distinguish between:

- Generic structure risk
- Exact wording risk
- Source licensing risk
- Trademark or brand association risk
- Adaptation originality risk

This is a product-screening aid, not legal advice.

---

## 8. Design For Many Niches From The Start

The database should support any niche where people express identity, humor, pride, frustration, belonging, achievement, taste, or routine.

Examples of supported niches include:

- Professions and workplace roles
- Hobbies and crafts
- Pets and animal communities
- Sports and teams at a generic level
- Family roles
- Gaming, including tabletop and video games
- Fitness and wellness
- Cars, motorcycles, and mechanical hobbies
- Gardening and plants
- Coffee, tea, cooking, and food interests
- Parenting and caregiving
- Outdoor recreation
- Music practice and performance
- School, study, and teaching
- Travel and local pride

The system should be able to take the same family and produce different niche adaptations.

Generic family:

```text
Warning: May Start Talking About [TOPIC]
```

Adaptations:

```text
Warning: May Start Talking About Dogs
Warning: May Start Talking About Marathon Training
Warning: May Start Talking About Compost
Warning: May Start Talking About Espresso
Warning: May Start Talking About Classic Cars
Warning: May Start Talking About Character Builds
```

---

## 9. Use Example Adaptations As Validation, Not Canon

Example adaptations are helpful, but they should not be mistaken for the canonical data.

Every generic template-family record should ideally include examples from multiple niches. A balanced example set might include:

- One profession or workplace example
- One hobby or craft example
- One pet, family, or lifestyle example
- One fitness, sport, or outdoor example
- One gaming or fantasy/TTRPG example when useful

This proves that the template is actually generic.

If a template only works in one niche, it may belong in a niche adaptation file rather than the generic template-family file.

---

## 10. Suggested Data Boundary

The initial generic template-family dataset should live in:

```text
data/phrase-patterns/internal-structural-template-families.jsonl
```

Those records should contain internally authored, low-risk, generic template families. They should not copy distinctive protected phrases.

Recommended fields:

- `id`
- `familyName`
- `primaryCategory`
- `templateForms`
- `slotTypes`
- `suitableNiches`
- `designFits`
- `legalRisk`
- `exampleAdaptations`

Future datasets may add separate adaptation records keyed back to `id`, for example:

```text
data/phrase-patterns/niche-adaptations.jsonl
```

That separation will let FusionCanvas expand into new niches without rewriting the generic database.

---

## Implementation Checklist

When adding or reviewing a phrase-pattern record, ask:

- Is this record describing a reusable pattern rather than a single niche phrase?
- Can the template work for at least three unrelated niches?
- Are the slot types generic and reusable?
- Are niche examples stored as examples or adaptation records, not as the core identity of the pattern?
- Does the record avoid exact protected lyrics, quotes, slogans, brand language, and franchise-specific wording?
- Does the record include legal-risk metadata?
- Does the source note emphasize phrase mechanics rather than topical content?
- Would this still make sense for professions, hobbies, pets, sports, family, gaming, fitness, cars, gardening, coffee, and parenting?

If the answer is no, the record should be revised, moved to a niche-specific adaptation layer, or excluded from the generic seed dataset.
