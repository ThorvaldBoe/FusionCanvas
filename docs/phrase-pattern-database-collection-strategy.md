# Phrase Pattern Database Collection Strategy

## Purpose

This document defines the initial strategy for collecting, normalizing, enriching, and storing phrase patterns for the FusionCanvas Phrase Pattern Database.

It is intentionally source-agnostic for now. Specific websites, datasets, APIs, books, public-domain collections, and scraping approaches should be researched and added later.

The goal is to define the collection workflow before deciding exactly where each phrase type should be harvested from.

Related document:

```text
docs/phrase-pattern-database-specification.md
```

---

## Background

FusionCanvas should support a structured creative process for turning rough ideas into stronger concepts, phrases, and designs.

A major part of POD ideation is recognizing reusable phrase structures. These structures can come from snowclones, idioms, memes, proverbs, song titles, movie catchphrases, slogans, signage, workplace expressions, and other familiar linguistic forms.

The Phrase Pattern Database should collect these patterns in a way that supports later transformation into niche-specific concepts.

The system should not simply store famous phrases. It should store reusable creative patterns with metadata that helps determine whether a phrase is recognizable, adaptable, legally risky, and design-friendly.

---

## Collection Goals

The collection process should produce phrase records that are:

1. Recognizable
2. Adaptable
3. Categorized
4. Searchable
5. De-duplicated
6. Scored
7. Legally flagged
8. Suitable for automated ideation

The system should prioritize phrase patterns that can generate many useful niche variations over phrases that are merely famous.

---

## Phrase Categories Covered

Collection should eventually support all 14 phrase categories:

1. Snowclones / Plug-In Phrases
2. Idioms
3. Famous Quotes
4. Song Titles
5. Song Lyrics
6. Movie and TV Catchphrases
7. Advertising Slogans
8. Proverbs
9. Internet Memes
10. Workplace and Everyday Expressions
11. Contrasting Pair Templates
12. Fake Authority Templates
13. Warning and Signage Templates
14. Structural Linguistic Patterns

---

## Recommended Collector Architecture

Each category should have its own collector or sub-job.

Recommended structure:

```text
Collectors/
├── Snowclones
├── Idioms
├── FamousQuotes
├── SongTitles
├── SongLyrics
├── MovieAndTVCatchphrases
├── AdvertisingSlogans
├── Proverbs
├── InternetMemes
├── WorkplaceEverydayExpressions
├── ContrastingPairTemplates
├── FakeAuthorityTemplates
├── WarningSignageTemplates
└── StructuralLinguisticPatterns
```

Each collector should produce the same normalized output model, even if the collection method differs.

---

## Collection Pipeline

Each collector should follow the same high-level pipeline.

```text
Source Input
  ↓
Raw Phrase Extraction
  ↓
Cleaning and Normalization
  ↓
Duplicate Detection
  ↓
Category Classification
  ↓
Slot Detection
  ↓
Metadata Enrichment
  ↓
Legal Risk Flagging
  ↓
Quality Scoring
  ↓
Storage
  ↓
Review / Curation
```

---

## Stage 1: Source Input

A source may be any structured or semi-structured input that contains useful phrase candidates.

Potential source types, to be researched later:

- Public-domain text collections
- Open datasets
- Wikis
- Quote collections
- Idiom lists
- Proverb lists
- Meme databases
- Song metadata APIs
- Movie quote datasets
- Internal manually curated lists
- User-entered phrase examples
- LLM-generated seed lists

For now, source records should store enough information to evaluate reliability and provenance later.

---

## Stage 2: Raw Phrase Extraction

The extraction process should convert raw source content into candidate phrases.

Typical extraction tasks:

- Extract text lines
- Remove numbering and bullets
- Strip source-specific formatting
- Remove explanations unless needed
- Split multi-phrase entries
- Preserve original casing where useful
- Detect embedded placeholders such as `[X]`, `___`, `{word}`, or similar

Raw extraction should not be overly aggressive. It is better to keep a questionable candidate with a low confidence score than to discard potentially useful material too early.

---

## Stage 3: Cleaning and Normalization

Each candidate phrase should be normalized for duplicate detection and search.

Recommended normalization steps:

- Trim whitespace
- Collapse repeated spaces
- Normalize curly quotes to straight quotes
- Normalize dashes where useful
- Remove trailing punctuation for comparison purposes
- Lowercase normalized form
- Remove obvious numbering or list markers
- Preserve original phrase separately

Example:

```json
{
  "phrase": "Trust Me, I'm a Wizard!",
  "normalizedPhrase": "trust me im a wizard"
}
```

The original phrase should remain unchanged for display and reference.

---

## Stage 4: Duplicate Detection

Duplicate detection should operate on multiple levels.

### Exact normalized duplicates

Same normalized phrase.

Example:

```text
Trust me, I'm a wizard
Trust Me I'm A Wizard!
```

### Near duplicates

Small spelling, punctuation, or wording variations.

Example:

```text
Easily Distracted By Dragons
Easily Distracted By The Dragons
```

### Pattern duplicates

Different filled-in versions of the same template.

Example:

```text
Easily Distracted By Dogs
Easily Distracted By Cats
Easily Distracted By Dragons
```

These may resolve to a shared pattern:

```text
Easily Distracted By [X]
```

---

## Stage 5: Category Classification

Each phrase should be assigned one primary category from the 14 phrase types.

A phrase may also have secondary categories.

Example:

```json
{
  "phrase": "Trust Me, I'm A Wizard",
  "primaryCategory": "snowclone",
  "secondaryCategories": ["fakeAuthorityTemplate"]
}
```

Classification should allow uncertainty.

Recommended field:

```json
{
  "categoryConfidence": 0.85
}
```

Phrases with low classification confidence should be routed to manual review.

---

## Stage 6: Slot Detection

Slot detection identifies parts of a phrase that can be replaced.

Examples:

```text
Trust Me, I'm A [ROLE]
Easily Distracted By [OBJECT_OR_TOPIC]
Born To [ACTIVITY], Forced To [OBLIGATION]
Here For The [REWARD], Not The [CONFLICT]
```

Recommended slot metadata:

```json
{
  "slots": [
    {
      "name": "ROLE",
      "type": "role",
      "required": true,
      "exampleValues": ["wizard", "dungeon master", "bard"]
    }
  ]
}
```

Slot detection may be manual, rule-based, LLM-assisted, or a combination.

---

## Stage 7: Metadata Enrichment

Each phrase should be enriched with useful creative metadata.

Recommended fields:

```json
{
  "tone": ["humor", "identity"],
  "themes": ["gaming", "fantasy", "workplace"],
  "audienceFit": ["ttrpg", "gamers", "fantasy fans"],
  "designFit": ["badge", "typographic", "signage"],
  "exampleAdaptations": [
    "Trust Me, I'm A Wizard",
    "Trust Me, I'm The Dungeon Master"
  ]
}
```

This metadata should help FusionCanvas recommend patterns for specific design ideas.

---

## Stage 8: Legal Risk Flagging

The database should not attempt to provide legal conclusions, but it should flag potential risk.

Recommended values:

```text
low
medium
high
unknown
```

### High-risk indicators

- Strong association with a modern brand
- Advertising slogan
- Distinctive movie or TV catchphrase
- Recent song lyric
- Trademarked phrase
- Character-specific quote
- Phrase strongly tied to a protected franchise

### Lower-risk indicators

- Generic everyday expression
- Traditional proverb
- Common idiom
- Broad structural pattern
- Heavily transformed phrase
- Public-domain source

### Important note

Legal risk scoring should be treated as a screening aid, not legal advice.

Final commercial use should still require human judgment.

---

## Stage 9: Quality Scoring

Each phrase should receive several practical scores.

### Recognition Score

How likely the target audience is to recognize the phrase or structure.

Scale:

```text
1 = obscure
10 = instantly recognizable
```

### Adaptability Score

How easily the phrase can be adapted to a niche.

Scale:

```text
1 = hard to adapt
10 = highly adaptable
```

### Designability Score

How easily the phrase can become a strong visual design.

Scale:

```text
1 = hard to visualize
10 = strong visual potential
```

### Originality Potential Score

How likely the phrase pattern is to generate original commercial-safe adaptations.

Scale:

```text
1 = mostly derivative
10 = excellent for original variations
```

### Commercial Risk Score

How much caution is needed before commercial use.

Scale:

```text
1 = low concern
10 = high concern
```

---

## Stage 10: Storage

The initial storage format may be JSON, JSONL, SQLite, or another lightweight format.

The storage layer should support:

- Importing batches
- Updating metadata
- Merging duplicates
- Searching by phrase
- Searching by category
- Searching by slot type
- Filtering by scores
- Filtering by legal risk
- Tracking source provenance

A simple starting point could be:

```text
data/
├── phrase-patterns.jsonl
├── phrase-sources.jsonl
└── phrase-review-queue.jsonl
```

Later this can evolve into SQLite, Postgres, or an application-managed database.

---

## Recommended Phrase Record Schema

```json
{
  "id": "",
  "phrase": "",
  "normalizedPhrase": "",
  "primaryCategory": "",
  "secondaryCategories": [],
  "categoryConfidence": 0.0,
  "source": {
    "name": "",
    "type": "",
    "url": "",
    "license": "",
    "retrievedAt": ""
  },
  "slots": [],
  "tone": [],
  "themes": [],
  "audienceFit": [],
  "designFit": [],
  "recognitionScore": 1,
  "adaptabilityScore": 1,
  "designabilityScore": 1,
  "originalityPotentialScore": 1,
  "commercialRiskScore": 1,
  "legalRisk": "unknown",
  "exampleAdaptations": [],
  "status": "candidate",
  "notes": ""
}
```

---

## Recommended Source Record Schema

```json
{
  "id": "",
  "name": "",
  "type": "",
  "url": "",
  "license": "",
  "accessMethod": "",
  "categories": [],
  "reliabilityScore": 1,
  "usageNotes": "",
  "lastCheckedAt": ""
}
```

---

## Review Statuses

Each phrase should have a review status.

Recommended statuses:

```text
candidate
approved
needs-review
rejected
duplicate
archived
```

### candidate

Imported or generated but not reviewed.

### approved

Good enough to use in ideation.

### needs-review

Requires manual classification, legal caution, or quality assessment.

### rejected

Not useful or unsuitable.

### duplicate

Duplicate of another phrase or pattern.

### archived

Not currently useful, but kept for reference.

---

## LLM-Assisted Enrichment Workflow

An LLM can assist with:

- Category classification
- Slot detection
- Tone tagging
- Adaptability scoring
- Example adaptation generation
- Legal-risk flagging
- Duplicate detection suggestions
- Design-fit suggestions

However, LLM output should be treated as suggested metadata, not ground truth.

Recommended flow:

```text
Raw Candidate
  ↓
Rule-based cleaning
  ↓
LLM enrichment
  ↓
Validation rules
  ↓
Review queue if confidence is low
  ↓
Approved phrase database
```

---

## Validation Rules

Automated validation should check that:

- Required fields are present
- Scores are within valid ranges
- Category is one of the allowed 14 categories
- Legal risk is one of the allowed values
- Normalized phrase is generated
- Duplicate candidates are detected
- Slot count matches slot metadata
- Source metadata is present when available

Records that fail validation should be rejected or routed to review.

---

## Human Review Workflow

Human review should focus on:

- Removing low-quality phrases
- Merging duplicates
- Correcting category errors
- Adjusting risk scores
- Improving slot definitions
- Adding better example adaptations
- Approving high-value patterns

Manual review is especially important for:

- Movie and TV catchphrases
- Song lyrics
- Advertising slogans
- Franchise-associated phrases
- Modern memes
- Any phrase intended for direct commercial use

---

## Initial MVP Scope

The first implementation should avoid over-engineering.

Recommended MVP:

1. Create JSONL storage.
2. Define phrase record schema.
3. Define source record schema.
4. Implement one collector for manually curated seed phrases.
5. Implement normalization.
6. Implement duplicate detection.
7. Implement basic category assignment.
8. Implement basic scoring fields.
9. Export approved phrases for use by FusionCanvas.

The MVP does not need scraping, APIs, or advanced automation at first.

---

## Suggested MVP File Structure

```text
phrase-pattern-database/
├── data/
│   ├── phrase-patterns.jsonl
│   ├── phrase-sources.jsonl
│   └── phrase-review-queue.jsonl
├── schemas/
│   ├── phrase-pattern.schema.json
│   └── phrase-source.schema.json
├── collectors/
│   ├── manual-seed-collector/
│   └── README.md
├── enrichment/
│   └── README.md
├── validation/
│   └── README.md
└── README.md
```

---

## Future Enhancements

Potential later additions:

- SQLite or Postgres storage
- Vector search for similar phrase patterns
- Phrase clustering
- Slot-type taxonomy
- Niche adaptation generator
- Legal-risk rule engine
- Phrase scoring UI
- Manual review interface
- Import history
- Source reliability tracking
- Deduplication reports
- Export to FusionCanvas concept pipeline

---

## Important Design Principle

The Phrase Pattern Database should be built as a creative intelligence system, not just a list of phrases.

A weak system stores this:

```text
I'll be back
```

A stronger system stores this:

```json
{
  "phrase": "I'll Be Back",
  "primaryCategory": "movieAndTVCatchphrase",
  "recognitionScore": 10,
  "adaptabilityScore": 7,
  "commercialRiskScore": 9,
  "possiblePatterns": [
    "I'll Be [X]",
    "I'll Be Back With [X]"
  ],
  "notes": "Highly recognizable but strongly associated with a specific film franchise. Use only as inspiration or transform substantially."
}
```

The goal is not to copy culture.

The goal is to understand phrase mechanics well enough to create original, niche-relevant, commercially safer ideas.
