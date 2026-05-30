# Phrase Pattern Database Expanded Acquisition Strategy

## Purpose

This document reframes FusionCanvas acquisition strategy around a **Phrase Intelligence Database** (also called a **Cultural Language Database**) rather than a narrow database of commercially safe shirt phrases.

The database exists to collect, classify, analyze, and transform recognizable language structures from culture. Commercial product use is only one downstream use case. A phrase can be unsafe for direct product use and still be highly valuable as source material for:

- pattern extraction
- snowclone extraction
- structure analysis
- phrase-family generation
- transformation analysis
- cultural reference detection
- originality review
- concept generation

Related documents:

```text
docs/phrase-pattern-database-specification.md
docs/phrase-pattern-database-collection-strategy.md
docs/phrase-pattern-database-source-research.md
docs/phrase-pattern-database-generic-design-principles.md
docs/phrase-pattern-database-creative-intents.md
data/phrase-patterns/source-candidates.jsonl
data/phrase-patterns/internal-structural-template-families.jsonl
```

---

## Strategic Reframe

Earlier collection planning emphasized low-risk phrase sources because FusionCanvas is intended to support Print-on-Demand ideation. That remains important, but it is incomplete.

FusionCanvas should not confuse **collection eligibility** with **commercial readiness**.

A famous movie quote, song lyric, advertising slogan, or meme caption may be unsuitable for direct placement on a product. However, it may still contain a reusable cultural mechanism:

- a repeatable sentence frame
- a recognizable slot pattern
- a contrast structure
- a rhythm or cadence
- a call-and-response form
- a joke setup
- a cultural reference marker
- a title pattern
- a compressed identity claim
- a transformation pathway from source phrase to safe original phrase

The expanded acquisition strategy should therefore ask two separate questions:

1. **Is this language valuable to collect or represent for phrase intelligence?**
2. **What restrictions apply to direct commercial use, transformation, display, storage, and automation?**

The answer to the first question can be yes even when the answer to direct commercial use is no.

---

## Core Principle: Collect Culture, Use With Controls

The Phrase Intelligence Database should eventually contain tens of thousands of records across phrase records, source records, structural template records, phrase-family records, and extracted pattern records.

The system should collect cultural language inputs broadly, but store and expose them with controls:

- source category
- provenance
- collection method
- review status
- collection risk
- commercial use risk
- transformation potential
- pattern extraction potential
- structural extraction potential
- recommended usage mode
- direct-use prohibition flags where needed
- notes on whether the record should be used only for metadata, analysis, or pattern extraction

This approach preserves the creative intelligence value of high-recognition language while preventing the application from treating every collected phrase as product-ready text.

---

## 1. Collection Sources

Collection sources are the places, corpora, references, or internal workflows from which FusionCanvas may discover cultural language.

Recommended source families include:

- public-domain proverb and literature collections
- idiom dictionaries and lexical resources
- snowclone references and phrase-template research
- internal structural template generation
- user-curated phrase mechanics
- famous quote references
- movie and TV quote references
- song title and lyric references
- advertising slogan references
- internet meme references
- headline and title corpora
- social media, forum, Reddit, and YouTube title language
- podcast and blog title language
- workplace and everyday language review panels
- commercial language samples such as signage, packaging, product listings, and marketing copy

A source should not be rejected simply because it contains protected expressions. Instead, it should be assigned an acquisition posture that reflects what can safely be done with it.

For example:

- A public-domain proverb collection may support direct collection and controlled direct display.
- A song lyric reference may support metadata-only collection or pattern-extraction-only analysis.
- An advertising slogan list may support cultural-reference detection and transformation analysis, but not direct phrase generation.
- A meme database may support trend awareness, structural extraction, and reference detection with careful provenance controls.

---

## 2. Source Tiers

Source tiers describe how a category should be handled as an input to the Phrase Intelligence Database. They are not moral judgments and are not exclusion rules.

### Tier 1: Direct Collection Sources

Sources whose phrase text can generally be collected as phrase records after normal quality, provenance, and duplicate review.

Typical examples:

- internally generated structural templates
- user-created phrase mechanics
- public-domain proverbs
- public-domain literature excerpts selected for phrase structure
- generic everyday expressions created by reviewers

Recommended usage mode: `direct-collection`.

### Tier 2: Collect With Review Sources

Sources that are valuable and may contain collectable phrases, but require manual review for provenance, over-specificity, attribution, licensing, modernity, or confusion risk.

Typical examples:

- idiom resources
- famous quote references
- news headlines
- blog titles
- forum language
- workplace language
- social media catchphrases

Recommended usage mode: `collect-with-review`.

### Tier 3: Inspiration-Only Sources

Sources whose language is culturally valuable but should not normally be stored as product-ready phrase text. These sources can guide ideation, transformation pathways, and internal pattern design.

Typical examples:

- commercial language
- slogan references
- modern meme references
- distinctive catchphrase collections

Recommended usage mode: `inspiration-only`.

### Tier 4: Pattern-Extraction-Only Sources

Sources whose exact phrase text may be sensitive, but whose abstract structure is highly valuable.

Typical examples:

- movie quotes
- TV quotes
- song lyrics
- advertising slogans
- highly distinctive internet memes

Recommended usage mode: `pattern-extraction-only`.

### Tier 5: Metadata-Only Sources

Sources where FusionCanvas should record only source metadata, cultural reference identifiers, categories, summaries, or research notes, not phrase text.

Typical examples:

- proprietary corpora
- rights-unclear datasets
- sources with restrictive terms
- sources used for cultural-reference detection benchmarks

Recommended usage mode: `metadata-only`.

---

## 3. Collection Risk

`collectionRisk` measures the risk of acquiring, storing, indexing, or displaying a phrase or source record inside FusionCanvas.

It is separate from commercial use risk.

Collection risk considers:

- source terms of service
- license clarity
- whether phrase text is copied verbatim
- whether only metadata or abstract structure is stored
- whether the source includes copyrighted or trademark-associated language
- whether the source contains personal data, usernames, private communities, or platform-specific restrictions
- whether automated collection is allowed or should be manual-only
- whether attribution, share-alike, or takedown workflows are required

Suggested scale:

```text
low = normal provenance and quality review is enough
medium = collect only with review, attribution, and usage-mode limits
high = avoid verbatim phrase storage; prefer pattern, metadata, or manually authored abstractions
restricted = do not ingest phrase text unless permission, license, or legal review is obtained
```

Example: a movie quote may have high collection risk if copied verbatim from a proprietary script corpus, but a manually authored record saying "three-word imperative catchphrase associated with a sci-fi franchise" may have lower collection risk because it stores abstract metadata rather than quote text.

---

## 4. Commercial Use Risk

`commercialUseRisk` measures the risk of using the phrase directly or near-directly on a product, listing, ad, or customer-facing design.

It considers:

- copyright sensitivity
- trademark and brand association
- franchise association
- personality/publicity concerns
- recognizability as a protected work
- likelihood of consumer confusion
- whether a phrase is a distinctive slogan or lyric
- whether the output is transformative enough to stand as original language

Suggested scale:

```text
low = generally suitable for direct use after normal duplicate and marketplace checks
medium = use only after review and likely transformation
high = do not use directly; require substantial transformation
very-high = never use directly; use only for metadata, detection, or abstract pattern extraction
```

Commercial use risk should travel with generated variants. A generated phrase derived from high-risk source language should retain derivation metadata and require stronger originality review before product use.

---

## 5. Transformation Potential

`transformationPotential` measures how useful a source phrase is for creating substantially new, niche-specific, original variants.

High transformation potential often appears when a phrase has:

- replaceable nouns, roles, actions, places, or objects
- a memorable grammatical skeleton
- a clear contrast or reversal mechanism
- a reusable joke structure
- a strong cadence independent of exact words
- a title-like frame
- a broad emotional or identity pattern
- a known snowclone family

Suggested scale:

```text
low = mostly fixed language with little reusable structure
medium = adaptable with human effort
high = naturally supports many niche variants
very-high = useful as a reusable phrase-family generator
```

Example: a protected slogan may be very high risk for direct commercial use, but it may still have high transformation potential if it demonstrates a compact imperative structure, question-answer structure, or two-word contrast pattern.

---

## 6. Pattern Extraction Potential

`patternExtractionPotential` measures whether a source can produce reusable phrase patterns such as snowclones, title formulas, joke setups, identity claims, or contrast templates.

This differs from transformation potential:

- transformation potential asks whether one phrase can become new variants;
- pattern extraction potential asks whether a source category can yield generalized templates across many phrase examples.

High pattern extraction potential sources include:

- snowclone databases
- meme references
- headlines
- YouTube titles
- podcast titles
- advertising slogans
- movie and TV catchphrases
- song titles
- workplace language
- forum and Reddit language

Pattern records should abstract away protected wording whenever possible.

Example pattern record:

```json
{
  "pattern": "[IMPERATIVE_VERB] The [ABSTRACT_NOUN]",
  "sourceCategory": "Advertising Slogans",
  "derivedFromExactPhrase": false,
  "commercialUseRisk": "medium",
  "recommendedUsageMode": "pattern-extraction-only"
}
```

---

## 7. Structural Extraction Potential

`structuralExtractionPotential` measures whether a source can teach FusionCanvas reusable linguistic mechanics even when no specific phrase should be retained.

Structural extraction captures deeper features such as:

- slot type sequences
- syntactic frames
- rhetorical devices
- meter and cadence
- parallelism
- negation structures
- escalation structures
- contrast pairs
- call-and-response mechanics
- title casing conventions
- list formats
- warning-label syntax
- faux authority formats
- identity declaration syntax

Structural extraction is the safest and most scalable way to learn from sensitive cultural language. It allows the database to become smarter without turning protected phrases into direct product suggestions.

Example structural record:

```json
{
  "structure": "role_claim_plus_activity",
  "templateForms": ["[ROLE] By Day, [ACTIVITY] By Night"],
  "slotTypes": ["role", "activity"],
  "sourceCategoriesObserved": ["Workplace Language", "TV Quotes", "Internet Memes"],
  "storesExactSourcePhrase": false,
  "recommendedUsageMode": "direct-collection"
}
```

---

## Required Record Concepts

### `collectionRisk`

Risk associated with acquiring and storing the source material.

Recommended values:

```text
low
medium
high
restricted
```

### `commercialUseRisk`

Risk associated with direct or near-direct customer-facing product use.

Recommended values:

```text
low
medium
high
very-high
```

### `transformationPotential`

How strongly the phrase or source supports creation of original variants.

Recommended values:

```text
low
medium
high
very-high
```

### `patternExtractionPotential`

How useful the phrase or source is for extracting generalized phrase templates.

Recommended values:

```text
low
medium
high
very-high
```

### `structuralExtractionPotential`

How useful the phrase or source is for extracting abstract linguistic mechanics.

Recommended values:

```text
low
medium
high
very-high
```

### `recommendedUsageMode`

Allowed values:

```text
direct-collection
collect-with-review
inspiration-only
pattern-extraction-only
metadata-only
```

---

## Why High-Risk Phrases May Still Be Valuable

A phrase may be valuable to collect or represent even if it should never be used directly on a product.

Examples:

- A movie quote can reveal a durable catchphrase shape, such as a short threat, promise, greeting, or identity declaration.
- A slogan can reveal a compact persuasion structure, such as imperative verb plus abstract goal.
- A song lyric can reveal cadence, repetition, and emotional compression.
- A meme can reveal a reusable joke setup, escalation pattern, caption rhythm, or image-text relationship.
- A headline can reveal curiosity gaps, list framing, contrast, and topical phrasing.
- A Reddit comment pattern can reveal authentic informal phrasing and community-specific rhetorical moves.

The database should therefore distinguish among:

1. **verbatim phrase text**
2. **source metadata**
3. **cultural reference markers**
4. **abstract pattern records**
5. **structural template families**
6. **generated original variants**
7. **commercially approved output phrases**

Only the last category should be treated as ready for product use.

---

## Recommended Acquisition Workflow

1. **Discover source**: identify category, provenance, license posture, technical access, and cultural value.
2. **Assign source tier**: choose direct collection, collect with review, inspiration-only, pattern-extraction-only, or metadata-only.
3. **Estimate collection risk**: decide whether exact phrase text may be stored.
4. **Estimate commercial use risk**: decide whether direct product use is blocked, review-only, or potentially allowed.
5. **Extract records**: create phrase, pattern, source, metadata, or structural records according to usage mode.
6. **Normalize and deduplicate**: detect exact duplicates, near duplicates, pattern duplicates, and family relationships.
7. **Enrich**: score recognition, transformation, pattern extraction, structural extraction, designability, and creative intent.
8. **Gate output**: prevent high-risk direct phrases from appearing as product-ready suggestions.
9. **Generate transformed variants**: use source language as inspiration for new original concepts.
10. **Review commercial candidates**: apply marketplace, trademark, originality, and human review checks before export.

---

## Data Model Implications

Phrase and pattern records should support acquisition metadata such as:

```json
{
  "sourceCategory": "Movie Quotes",
  "sourceTier": "pattern-extraction-only",
  "storesExactPhrase": false,
  "collectionRisk": "high",
  "commercialUseRisk": "very-high",
  "transformationPotential": "high",
  "patternExtractionPotential": "very-high",
  "structuralExtractionPotential": "very-high",
  "recommendedUsageMode": "pattern-extraction-only",
  "directCommercialUseAllowed": false,
  "requiresTransformation": true,
  "requiresHumanReview": true
}
```

The database should permit multiple record types from the same cultural source:

- `sourceRecord`: where the material came from
- `phraseRecord`: exact phrase where allowed
- `metadataRecord`: cultural reference information without phrase text
- `patternRecord`: generalized reusable template
- `structureRecord`: abstract linguistic mechanics
- `transformationRecord`: explanation of how a source phrase was transformed
- `commercialCandidateRecord`: generated phrase proposed for product use

---

## Guidance By Usage Mode

### `direct-collection`

Use when phrase text can be stored as a normal phrase candidate.

Controls:

- provenance required
- duplicate detection required
- commercial use still requires marketplace checks

### `collect-with-review`

Use when phrase text may be stored, but only after human or policy review.

Controls:

- review status required
- source notes required
- direct product use disabled until approved

### `inspiration-only`

Use when a source can inform ideation, but exact source phrases should not be treated as reusable assets.

Controls:

- store notes, categories, and observations
- avoid product-facing exact text
- prefer original internally written templates

### `pattern-extraction-only`

Use when exact phrases are sensitive but generalized mechanics are valuable.

Controls:

- extract abstract templates
- do not expose source phrases as candidate product copy
- keep derivation notes and risk flags

### `metadata-only`

Use when the database should track the source or cultural reference without storing phrase text.

Controls:

- store source identity, category, tags, and research notes
- no verbatim phrase field
- no direct generation from exact language

---

## Large-Scale Collection Goal

The long-term goal is a large-scale phrase intelligence corpus containing tens of thousands of records, not a small handpicked list of safe slogans.

A mature corpus should include:

- broadly recognized traditional language
- modern internet language
- popular media reference structures
- commercial persuasion structures
- community-specific speech patterns
- title and headline mechanics
- generic internal template families
- transformed phrase families
- cultural reference detection signatures

The system should scale by separating **what is culturally useful to understand** from **what is commercially safe to print**.

---

## Final Guiding Principle

Do not exclude cultural language merely because it is protected, risky, modern, or unsuitable for direct product use.

Instead:

1. collect or represent it at the appropriate abstraction level;
2. label collection and commercial risks separately;
3. extract reusable patterns and structures where possible;
4. generate original variants rather than copying source phrases;
5. reserve direct product use for reviewed, transformed, and commercially appropriate output.
