# Phrase Intelligence v2 Bootstrap Data

## Created files

This bootstrap pass creates the first small v2 Phrase Intelligence dataset from existing repository-owned seed data only. It does not collect new external phrase sources, scrape the web, or replace the legacy v1/bootstrap references.

Created v2 JSONL files:

```text
data/phrase-intelligence/sources/manual-seed.sources.v2.jsonl
data/phrase-intelligence/phrases/manual-seed.phrases.v2.jsonl
data/phrase-intelligence/patterns/manual-seed.patterns.v2.jsonl
data/phrase-intelligence/template-families/internal-template-families.v2.jsonl
```

The source file contains source-record entries for:

- the manual seed phrase list; and
- the internal structural template family list.

Both sources are marked as `tier-1` because they come from internal/manual bootstrap material rather than newly imported external sources.

## How v1 seed data maps to v2 records

The v1 file `data/phrase-patterns/manual-seed-phrases.jsonl` remains a legacy/bootstrap reference. Its records are mapped into v2 records as follows:

| v1 field | v2 mapping |
| --- | --- |
| `id` | carried forward into a v2-prefixed `id` such as `phrase-manual-seed-0030` or `pattern-manual-seed-0001` |
| `phrase` | `phraseText` for exact phrase records, or `patternText` for reusable slot-based pattern records |
| `normalizedPhrase` | `normalizedText` |
| `primaryCategory` | mapped to the closest v2 `sourceCategory` enum value |
| `secondaryCategories`, `tone`, `themes`, `audienceFit`, `designFit` | preserved as v2 `categories` and/or `creativeIntents` where useful |
| `source` | represented by shared v2 source records and per-record provenance metadata |
| `slots` | preserved as v2 `slots` |
| score fields | mapped into v2 potential and risk fields such as `transformationPotential`, `patternExtractionPotential`, `structuralExtractionPotential`, and `commercialUseRisk` |
| `exampleAdaptations` | stored as `exampleTransformations` with `requiresReviewBeforeUse: true` |
| `status` | mapped to `reviewStatus` |
| `notes` | preserved in v2 `notes` |

The v1 file `data/phrase-patterns/internal-structural-template-families.jsonl` remains the legacy/bootstrap reference for higher-level structures. Its records are mapped to v2 `template-family-record` entries by preserving family names, template forms, slot types, suitable niches, design fits, generated examples, and legal-risk notes.

## Phrase-record vs. pattern-record

Use a `phrase-record` when the record stores exact or near-exact reusable language with no unresolved placeholders. Examples from the bootstrap set include:

- `It Is What It Is`
- `Adventure Can Wait`
- `This Is Fine`

Use a `pattern-record` when the record is primarily a reusable structure with one or more placeholders. Examples include:

- `Easily Distracted By [X]`
- `Born To [X], Forced To [Y]`
- `Warning: May Contain [X]`

Pattern records are not final product phrases. They intentionally use `directUseAllowed: false` so generation can treat them as structural inputs rather than customer-facing copy.

## Why generated examples are not product-ready

Generated examples in this bootstrap dataset are included only to illustrate how a phrase pattern or template family can transform across niches. They are not approved product copy because they have not yet been checked for:

- originality against existing marketplace phrases;
- brand, franchise, team, celebrity, or trademark conflicts;
- accidental closeness to slogans, lyrics, quotes, titles, or meme captions;
- niche-specific cultural or safety concerns; and
- final design, typography, and product-context suitability.

For that reason every generated `exampleTransformations` item is marked with `requiresReviewBeforeUse: true`.

## What to validate before external data import

Before adding any external source data, validate that the v2 bootstrap records can support the full collection workflow:

1. Schema validation for every JSONL row against the matching v2 schema.
2. Stable source IDs and provenance links for imported, derived, and internally authored records.
3. Category mappings from collector-specific labels into the v2 controlled enums.
4. Risk routing for `collectionRisk` versus `commercialUseRisk`.
5. Review behavior for exact phrases, extracted patterns, generated examples, and template families.
6. Deduplication behavior across exact phrases, normalized text, pattern text, and template forms.
7. Import rules that prevent external source text from being treated as approved product copy by default.
8. Attribution and license handling for any future source that is not internal/manual `tier-1` material.
