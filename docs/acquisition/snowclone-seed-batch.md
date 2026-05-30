# Snowclone Seed Batch

## Purpose

This batch creates the first dedicated snowclone acquisition set for the Phrase Intelligence v2 database. It adds one curated source record and 100 pattern records that store reusable phrase structures with explicit placeholder slots.

The records are intended to help FusionCanvas generate, compare, and review flexible language mechanics such as identity statements, preference contrasts, warning-label humor, ritual/fuel jokes, collection jokes, and niche-specific activity claims. The batch intentionally stores patterns rather than final product phrases.

## Why Snowclones Are The First Acquisition Target

Snowclones are the highest-value first target because they already behave like templates. A useful snowclone has a recognizable shape and one or more replaceable parts, which maps directly to the v2 pattern-record model.

They are especially useful early in acquisition because they:

- expose slots clearly enough for structured metadata;
- adapt across many commercial niches without being fantasy-specific;
- support rapid generation of original candidate phrases;
- make deduplication and risk review easier than large exact-phrase collections;
- bridge existing manual seed patterns with future source-driven acquisition; and
- provide strong examples for template-family discovery.

## How These Differ From Final Product Phrases

The entries in `snowclone-seed.patterns.v2.jsonl` are not final niche phrases. They are reusable structures such as:

```text
[TOPIC] Is My Happy Place
Never Underestimate A [ROLE] With [OBJECT]
Will [ACTIVITY] For [PREFERRED_THING]
```

A final product phrase fills those slots with niche content, for example a pet, coffee, gardening, programming, teaching, nursing, fishing, car, fitness, parenting, book, cooking, gaming, hiking, or crafting variant. Those transformations still require review before use.

Each record therefore sets:

- `directUseAllowed: false`;
- `requiresReviewBeforeUse: true`;
- `recommendedUsageMode: collect-with-review`; and
- medium commercial-use risk by default.

This keeps the database focused on reusable mechanics while avoiding accidental approval of niche adaptations that may be overused, marketplace-saturated, confusingly similar to known slogans, or otherwise unsuitable.

## Review Criteria

Reviewers should approve, revise, or reject candidate patterns using these criteria:

1. **Slot clarity**: The pattern must contain at least one meaningful placeholder slot with a generic slot type.
2. **Niche agnosticism**: The stored pattern should work across several niches and should not be a single niche adaptation.
3. **Transformability**: The structure should produce multiple original candidate phrases without requiring exact source wording.
4. **Commercial caution**: Generated phrases should avoid exact brand slogans, franchise references, lyrics, titles, character names, and source-specific wording.
5. **Recognizability without copying**: The cadence may be familiar, but the record should remain a generalized structure rather than a copied protected expression.
6. **Deduplication**: The pattern should not duplicate existing manual v2 patterns or legacy manual seed phrases unless it is an intentional cleaner replacement.
7. **Example diversity**: Example transformations should demonstrate more than one niche and should remain illustrative, not product-approved copy.

## Risks And Limitations

Snowclones are useful because they are recognizable, but that recognizability also creates risk. Some phrase structures may have originated in advertisements, films, television, memes, songs, books, or other commercially sensitive sources. This batch avoids exact brand slogans and trademarked/franchise-specific wording, but review is still required because a generated transformation can become risky even when the abstract pattern is acceptable.

Known limitations of this first batch:

- provenance is internal manual curation rather than external source research;
- records are candidates, not approved production copy;
- example transformations are illustrative and may be common in marketplaces;
- several patterns have broad cultural or meme-like cadence and should not be pushed toward source-specific adaptations;
- the batch emphasizes English-language POD-style phrasing; and
- template-family assignments are intentionally broad seed metadata, not final taxonomy decisions.

## Suggested Next Acquisition Batches

Recommended follow-up batches:

1. **Warning-label and caution-sign templates**: Expand safety-sign, faux label, side-effect, and disclaimer structures.
2. **Workplace expression templates**: Add meeting, deadline, shift-work, desk-sign, and professional-role patterns.
3. **Authority and faux-certification templates**: Add badge, permit, department, inspector, expert, and official-member structures.
4. **Collection and hobby-excess templates**: Add collector, stash, "one more," and supplies-related structures.
5. **Routine and fuel templates**: Add coffee/tea/snack, morning ritual, workout fuel, and shift-survival structures.
6. **Place and escape templates**: Add trail, garden, kitchen, library, garage, beach, campsite, and happy-place structures.
7. **Public-domain proverb pattern extraction**: Convert traditional sayings into safer abstract structures with provenance.
8. **Meme-structure review batch**: Capture only abstract mechanics from meme-like structures with higher review flags where needed.
