# Phrase Pattern Database v2 Schema

## Purpose

The v2 Phrase Intelligence Database schema defines four practical JSONL-ready record types for broad cultural language collection without treating every collected phrase as product-ready copy.

The v2 model separates:

1. source metadata and acquisition posture;
2. exact or near-exact phrase records;
3. extracted pattern records; and
4. higher-level template family records.

This separation lets FusionCanvas learn from idioms, headlines, memes, slogans, quotes, titles, lyrics, workplace language, and internally authored templates while preserving provenance, review gates, and risk distinctions.

The schema files are:

```text
schemas/phrase-intelligence/phrase-record.schema.json
schemas/phrase-intelligence/source-record.schema.json
schemas/phrase-intelligence/pattern-record.schema.json
schemas/phrase-intelligence/template-family.schema.json
```

These schemas are intentionally small enough for newline-delimited JSON files and later database import. They do not define collectors and they do not require immediate migration of the existing bootstrap data.

---

## v2 Record Types

### `source-record`

A `source-record` describes a source before or alongside any collected phrase data. It stores source name, category, URL, license posture, acquisition method, recommended usage mode, source tier, and risk fields.

Use source records for questions such as:

- Is this source suitable for direct collection, review-only collection, pattern extraction, or metadata-only tracking?
- Does the source allow exact text storage?
- Does it require attribution?
- Is the source commercially safe, culturally valuable, or both?

### `phrase-record`

A `phrase-record` stores exact or near-exact language, such as:

- catchphrases;
- idioms;
- titles;
- quotes;
- slogans;
- headlines;
- song-title-like text;
- meme captions;
- public-domain or traditional sayings; and
- internally authored seed phrases.

Phrase records may be valuable for recognition, duplicate detection, cultural reference detection, or later pattern extraction. A phrase record is not automatically product-ready.

### `pattern-record`

A `pattern-record` stores an extracted reusable structure, such as:

```text
Make [X], Not [Y]
[X] Called, I Didn't Answer
Keep [X] And [Y]
When In Doubt, [ACTION]
```

Pattern records should abstract away source-specific expression where possible. They can reference a source phrase through `sourcePhraseId`, but their `patternText` should be a reusable structure rather than a copied quote or slogan.

### `template-family-record`

A `template-family-record` groups several related patterns into a higher-level creative mechanic, such as:

- Identity Declaration;
- Preference Statement;
- Warning Label;
- Boundary Statement;
- Priority Stack; or
- Faux Certification.

Template families support ideation and generation by describing recurring slot types, creative intents, and example original transformations.

---

## Why v2 Separates Phrase Records From Pattern Records

The v1 planning documents correctly identify that phrase ideation depends on both recognizable language and reusable structure. However, exact phrases and extracted patterns have different risk profiles and different product value.

A phrase record may contain culturally recognizable text. That is useful for analysis, normalization, duplicate checks, source mapping, and understanding why a pattern is familiar. But exact phrase text can also carry copyright, trademark, brand, personality, publicity, marketplace, or attribution concerns.

A pattern record captures the reusable mechanics: slot positions, contrast, cadence, negation, escalation, imperative framing, list structure, or call-and-response shape. Pattern records are usually safer and more useful for generation because they encourage original language rather than direct reuse.

For example:

```text
Phrase record: a specific slogan, lyric, quote, title, headline, or meme caption.
Pattern record: [IMPERATIVE_VERB] [ABSTRACT_GOAL]
Generated original phrase: Brew The Brave
```

The exact phrase can remain restricted, while the extracted structure can become a controlled creative tool.

---

## Why Collection Risk and Commercial-Use Risk Are Separate

v2 uses two independent risk fields:

### `collectionRisk`

`collectionRisk` measures the risk of acquiring, storing, indexing, displaying, or retaining a source or phrase inside FusionCanvas.

Allowed values:

```text
low
medium
high
restricted
```

Collection risk considers:

- source terms of service;
- license clarity;
- whether exact source text is stored;
- automated versus manual collection;
- personal data or platform restrictions;
- attribution requirements;
- takedown needs; and
- whether only metadata or abstract structure is retained.

### `commercialUseRisk`

`commercialUseRisk` measures the risk of using exact or near-exact language on a product, listing, ad, or customer-facing design.

Allowed values:

```text
low
medium
high
very-high
```

Commercial-use risk considers:

- copyright sensitivity;
- trademark or brand association;
- franchise association;
- distinctive slogan or lyric status;
- personality or publicity concerns;
- marketplace enforcement likelihood;
- consumer confusion risk; and
- whether generated text is sufficiently original.

These risks often diverge. A public-domain proverb may have low collection risk and low commercial-use risk. A modern slogan list may be possible to describe as metadata but very risky for direct product use. A licensed dataset may have low collection risk for internal storage but still high commercial-use risk for exact customer-facing phrases.

---

## Core Controlled Values

The v2 schemas use controlled enum values for the main routing and review fields.

### `recommendedUsageMode`

```text
direct-collection
collect-with-review
inspiration-only
pattern-extraction-only
metadata-only
```

### `collectionRisk`

```text
low
medium
high
restricted
```

### `commercialUseRisk`

```text
low
medium
high
very-high
```

### Potential fields

The following fields share the same potential scale:

- `transformationPotential`
- `patternExtractionPotential`
- `structuralExtractionPotential`

Allowed values:

```text
low
medium
high
very-high
```

### `reviewStatus`

```text
candidate
approved
needs-review
rejected
duplicate
archived
```

---

## Shared v2 Fields

The schemas share a common practical vocabulary so records can be stored as JSONL and imported later.

| Field | Purpose |
| --- | --- |
| `id` | Stable record identifier. |
| `recordType` | One of `phrase-record`, `pattern-record`, `template-family-record`, or `source-record`. |
| `phraseText` / `patternText` | Exact phrase text or extracted reusable pattern text. Template families may use `familyName` and optional `patternText`. |
| `normalizedText` | Lowercase/canonical text for search, duplicate detection, and joins. |
| `sourceCategory` | Controlled source or phrase category. |
| `sourceId` | Link to a `source-record` when known. |
| `sourceName` | Human-readable source name. |
| `provenance` | Origin, retrieval, review, evidence, and confidence details. |
| `sourceUrl` | Source URL when applicable. |
| `sourceLicense` | License, rights, or internal-source posture. |
| `collectionMethod` | How the record was created or obtained. |
| `storesExactSourceText` | Whether the record stores exact external source wording. |
| `derivedFromExactPhrase` | Whether this record was derived from exact phrase text. |
| `sourcePhraseId` | Link from a pattern or template back to a phrase record when retained. |
| `sourceTier` | Tier 1 through Tier 5 acquisition posture. |
| `recommendedUsageMode` | Routing guidance for downstream systems. |
| `collectionRisk` | Risk of collecting or storing the record. |
| `commercialUseRisk` | Risk of using the text directly or near-directly in commerce. |
| `transformationPotential` | Usefulness for making substantially new variants. |
| `patternExtractionPotential` | Usefulness for deriving reusable phrase patterns. |
| `structuralExtractionPotential` | Usefulness for deeper linguistic mechanics. |
| `directUseAllowed` | Whether exact text may be used directly after normal checks. |
| `requiresReviewBeforeUse` | Whether downstream use must be reviewed first. |
| `requiresAttribution` | Whether attribution is required by license or policy. |
| `reviewStatus` | Current curation state. |
| `categories` | Additional category tags. |
| `creativeIntents` | Creative intent IDs or labels. |
| `templateFamilies` | Related family IDs or names. |
| `slots` | Replaceable positions and slot metadata. |
| `exampleTransformations` | Original examples or review-gated transformations. |
| `notes` | Human-readable curation notes. |
| `createdAt` / `updatedAt` | ISO 8601 timestamps. |

---

## Source Tiers and Usage Modes

v2 maps the source-tier strategy into a compact enum:

| Tier | Typical posture | Usual `recommendedUsageMode` |
| --- | --- | --- |
| `tier-1` | Direct collection sources, such as internally authored templates and public-domain/traditional material. | `direct-collection` |
| `tier-2` | Valuable sources that require provenance, license, or specificity review. | `collect-with-review` |
| `tier-3` | Cultural sources useful for ideation but not generally product-ready as exact text. | `inspiration-only` |
| `tier-4` | Sources where exact language may be sensitive but abstract patterns are valuable. | `pattern-extraction-only` |
| `tier-5` | Sources where only source metadata, summaries, or research notes should be retained. | `metadata-only` |

The tier is a handling posture, not a moral judgment and not a permanent ban. A Tier 4 source can produce a low-risk pattern record if the extracted pattern is abstract enough and does not preserve protected expression.

---

## How High-Risk Cultural Phrases Can Produce Safe Pattern Records

High-risk cultural phrases are often valuable because they reveal durable language mechanics:

- imperative frames;
- contrast pairs;
- identity claims;
- refusal structures;
- title cadence;
- joke setup and reversal;
- repetition;
- parallelism;
- warning-label syntax; and
- compact emotional framing.

A phrase can have `commercialUseRisk: "very-high"` while the derived pattern has lower commercial-use risk because the pattern no longer stores or recommends the exact phrase.

Example flow:

```json
{
  "recordType": "phrase-record",
  "recommendedUsageMode": "pattern-extraction-only",
  "collectionRisk": "high",
  "commercialUseRisk": "very-high",
  "storesExactSourceText": true,
  "directUseAllowed": false,
  "requiresReviewBeforeUse": true
}
```

The derived pattern might be:

```json
{
  "recordType": "pattern-record",
  "patternText": "[SHORT_IMPERATIVE_VERB] [ABSTRACT_NOUN]",
  "derivedFromExactPhrase": true,
  "recommendedUsageMode": "direct-collection",
  "collectionRisk": "low",
  "commercialUseRisk": "medium",
  "storesExactSourceText": false,
  "directUseAllowed": false,
  "requiresReviewBeforeUse": true
}
```

The generated phrase should then be original, niche-specific, and reviewed before commercial use if it inherits high-risk lineage.

---

## Record Flow: Source Phrase to Pattern to Generated Original Phrase

Recommended v2 flow:

1. **Create or review a `source-record`.** Assign `sourceTier`, `recommendedUsageMode`, license posture, `collectionRisk`, and `commercialUseRisk`.
2. **Create a `phrase-record` only when exact phrase storage is allowed by the usage mode and review policy.** Set `storesExactSourceText` accurately.
3. **Extract one or more `pattern-record` entries.** Use slots such as `[X]`, `[Y]`, `[ACTION]`, `[ROLE]`, or typed slot metadata. Set `sourcePhraseId` when the pattern came from a retained phrase record.
4. **Group patterns into `template-family-record` entries.** Link families through `templateFamilies` or family IDs.
5. **Generate original candidate phrases outside these schemas.** Generated outputs should carry lineage to their source pattern and any source phrase lineage.
6. **Gate generated phrases before commercial use.** Preserve inherited risk, run duplicate checks, avoid protected names, and require review when `requiresReviewBeforeUse` is true.

In short:

```text
source-record
  -> phrase-record, when exact storage is allowed
    -> pattern-record, when reusable structure is extracted
      -> template-family-record, when related patterns share mechanics
        -> generated original phrase, reviewed before customer-facing use
```

Generated original phrases are deliberately not added as a fifth schema in this change. They can be represented later by an output-candidate schema that references `pattern-record` and `template-family-record` IDs.

---

## Migration Notes for Existing v1 Bootstrap Data

The existing v1 files should remain unchanged for now. They are legacy/bootstrap data that can be migrated later with a small mapping script and human review.

Existing v1 legacy/bootstrap files:

```text
data/phrase-patterns/source-tier-taxonomy.json
data/phrase-patterns/manual-seed-phrases.jsonl
data/phrase-patterns/source-candidates.jsonl
data/phrase-patterns/internal-structural-template-families.jsonl
data/phrase-patterns/creative-intent-taxonomy.json
```

Related v1 planning documents:

```text
docs/phrase-pattern-database-specification.md
docs/phrase-pattern-database-collection-strategy.md
docs/phrase-pattern-database-source-research.md
docs/phrase-pattern-database-expanded-acquisition-strategy.md
docs/phrase-pattern-database-creative-intents.md
docs/phrase-pattern-database-generic-design-principles.md
```

### Suggested later migration mapping

| v1 field or concept | v2 target |
| --- | --- |
| `phrase` | `phraseText` or `patternText`, depending on whether the text is exact phrase language or already a reusable slot pattern. |
| `normalizedPhrase` | `normalizedText`. |
| `primaryCategory` / `secondaryCategories` | `sourceCategory` plus `categories`. |
| `source.name` | `sourceName`. |
| `source.url` | `sourceUrl`. |
| `source.license` | `sourceLicense`. |
| `source.type` | `collectionMethod`, `sourceCategory`, or `provenance.origin`, depending on context. |
| `legalRisk` / `commercialRiskScore` | `commercialUseRisk`, with manual review for scores near category boundaries. |
| source-tier taxonomy `recommendedUsageMode` | `recommendedUsageMode`. |
| source-tier taxonomy category risk | Initial `commercialUseRisk`, `transformationPotential`, and `patternExtractionPotential`. |
| template family `familyName` | `familyName` on `template-family-record`. |
| template family `templateForms` | `templateForms` and related `pattern-record` entries. |
| template family `slotTypes` | `slots`. |
| creative intent IDs | `creativeIntents`. |
| `status` | `reviewStatus`. |
| `exampleAdaptations` | `exampleTransformations`. |

Migration should not blindly promote v1 seed phrases into product-ready language. Each migrated record should explicitly set `directUseAllowed`, `requiresReviewBeforeUse`, `storesExactSourceText`, and both risk fields.

---

## Practical JSONL Guidance

Each JSONL line should be one complete record. Keep references simple:

- `sourceId` links phrase, pattern, or template records to a source record.
- `sourcePhraseId` links extracted patterns back to retained phrase records.
- `templateFamilies` links phrase or pattern records to reusable families.
- `creativeIntents` may store IDs from the creative intent taxonomy.

Use stable IDs with readable prefixes, for example:

```text
src-...
phrase-...
pattern-...
tmplfam-...
```

Avoid storing unnecessary exact source text when `recommendedUsageMode` is `pattern-extraction-only` or `metadata-only`. Prefer `storesExactSourceText: false` for abstract records derived from high-risk sources.
