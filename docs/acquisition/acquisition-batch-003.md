# Phrase Intelligence Acquisition Batch 003

## Scope

Batch 003 continues the internally authored v2 acquisition workflow with 60 reusable phrase patterns across the next three categories recommended by the snowclone seed documentation:

1. collection and hobby-excess templates;
2. routine and fuel templates; and
3. place and escape templates.

The batch is stored in:

```text
data/phrase-intelligence/sources/acquisition-batch-003.sources.v2.jsonl
data/phrase-intelligence/patterns/acquisition-batch-003.patterns.v2.jsonl
```

The reproducible batch definition and writer are stored in:

```text
tools/New-AcquisitionBatch003.ps1
```

## Acquisition Posture

All records are internally authored generic structures. The batch does not scrape external websites, retain exact source phrases, or claim public-domain provenance.

Each source and pattern record therefore uses:

- `sourceTier: "tier-1"`;
- `recommendedUsageMode: "direct-collection"`;
- `collectionRisk: "low"`;
- `storesExactSourceText: false`;
- `directUseAllowed: false`; and
- `requiresReviewBeforeUse: true`.

`direct-collection` means the abstract record is suitable for collection. It does not make generated customer-facing text product-ready. Every example remains a review-gated illustration.

## Coverage

| Group | Count | Main mechanics |
| --- | ---: | --- |
| Collection and hobby excess | 20 | one-more framing, collection counts, supply stashes, project queues, selective excess, and hobby priorities |
| Routine and fuel | 20 | fuel-first sequences, input/output frames, status labels, reset loops, routine stacks, and energy contrasts |
| Place and escape | 20 | happy places, destination signs, mental escape, belonging, scenic routes, and place-based contrasts |

## Review Notes

Before any generated adaptation is used commercially, reviewers should still check:

- exact and normalized duplicates across existing FusionCanvas records;
- marketplace saturation and confusing similarity;
- brand, franchise, title, lyric, slogan, and character-name references introduced through slot values;
- grammatical agreement after slot substitution; and
- whether the result is sufficiently original for its intended product and audience.

The stored patterns are candidates for creative generation, not approved product copy.
