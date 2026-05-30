# Phrase Pattern Database Source Research

## Purpose

This document defines how to research, evaluate, and prioritize sources for filling the FusionCanvas Phrase Pattern Database.

It does not yet prescribe final sources. The purpose is to create a structured research framework so that future work can identify the best sources for each phrase type without mixing source discovery, legal evaluation, data collection, and implementation decisions too early.

Related documents:

```text
docs/phrase-pattern-database-specification.md
docs/phrase-pattern-database-collection-strategy.md
```

---

## Background

The Phrase Pattern Database is intended to collect recognizable and adaptable phrase structures that can support Print-on-Demand ideation, FusionCanvas concept generation, phrase refinement, and niche-specific creative exploration.

The database should not be built by blindly scraping random phrase lists. It should be filled from sources that are useful, reliable, legally understandable, and suitable for transformation into original ideas.

Different phrase categories require different source strategies. A source that is excellent for idioms may be unsuitable for song lyrics. A meme source may be culturally valuable but legally or technically difficult. A public-domain proverb collection may be legally safer but less modern.

This document provides a research and evaluation framework for choosing the right source mix.

---

## Research Goals

The source research process should answer the following questions:

1. Which sources contain high-quality phrase patterns?
2. Which sources are legally safer to use?
3. Which sources are technically easy to collect from?
4. Which sources have enough structure to support automation?
5. Which sources provide strong recognition value?
6. Which sources provide strong adaptability value?
7. Which sources should only be used for inspiration, not direct storage?
8. Which sources should be excluded?

---

## Source Evaluation Criteria

Each potential source should be evaluated using the criteria below.

---

## 1. Phrase Quality

How useful are the phrases creatively?

High-quality sources contain phrases that are:

- Short enough for designs
- Recognizable
- Adaptable
- Memorable
- Rhythmically strong
- Suitable for transformation
- Suitable for visual interpretation

Score:

```text
1 = mostly low-quality or unusable phrases
10 = consistently high-value phrase patterns
```

---

## 2. Recognition Value

How likely is the target audience to recognize the phrase or phrase structure?

A source may be valuable even if not everyone recognizes it, as long as the relevant niche audience does.

Score:

```text
1 = obscure
10 = broadly recognizable
```

---

## 3. Adaptability

How easily can phrases from the source be adapted into niche-specific variants?

High-adaptability sources often contain:

- Replaceable nouns
- Replaceable roles
- Replaceable activities
- Contrasting pairs
- Reusable sentence structures
- Repeated templates

Score:

```text
1 = hard to adapt
10 = highly adaptable
```

---

## 4. Legal / Commercial Risk

How risky is it to use material from the source for commercial ideation?

The database should distinguish between:

- Storing a phrase for analysis
- Using a phrase directly on a product
- Using a phrase as inspiration
- Using a heavily transformed variant

Risk indicators:

- Modern song lyrics
- Distinctive movie or TV quotes
- Advertising slogans
- Trademarked phrases
- Brand-associated lines
- Franchise-associated terms
- Recently created memes with identifiable creators

Safer indicators:

- Public-domain text
- Traditional proverbs
- Common idioms
- Generic everyday expressions
- Structural phrase patterns
- Heavily transformed variants

Score:

```text
1 = low commercial concern
10 = high commercial concern
```

Important: This score is a screening aid, not legal advice.

---

## 5. License Clarity

How clear are the usage rights, license terms, or public-domain status?

Score:

```text
1 = unclear or restrictive
10 = clear and permissive
```

Sources with unclear license terms should either be excluded or used only for manual inspiration and research notes.

---

## 6. Technical Accessibility

How easy is it to collect data from the source?

Consider:

- API availability
- Downloadable datasets
- Structured HTML
- Rate limits
- Terms of service
- Authentication requirements
- Anti-scraping restrictions
- Data cleanliness

Score:

```text
1 = difficult or impractical
10 = easy and clean
```

---

## 7. Data Structure

How structured is the source?

Highly structured sources are easier to process automatically.

Examples of useful structure:

- Phrase field
- Category field
- Source/origin field
- Tags
- Popularity metrics
- Public-domain status
- Author/date metadata
- Explanations separate from phrases

Score:

```text
1 = unstructured text only
10 = clean structured data
```

---

## 8. Automation Potential

Can this source be used repeatedly by an automated collector?

High automation potential means:

- Stable format
- Clear access pattern
- Acceptable terms
- Low maintenance burden
- Programmatic access
- Predictable output

Score:

```text
1 = manual-only
10 = highly automatable
```

---

## 9. Freshness / Cultural Relevance

How current is the source?

This matters especially for memes, workplace phrases, internet phrases, and pop culture references.

Score:

```text
1 = outdated or historically interesting only
10 = highly current and culturally relevant
```

Freshness should not always be prioritized. Older sources may be more legally stable and culturally durable.

---

## 10. Source Reliability

How trustworthy is the source metadata?

For example, a quote collection may be full of misattributions, while a structured public-domain corpus may be more reliable.

Score:

```text
1 = unreliable or noisy
10 = highly reliable
```

---

## Source Decision Categories

After evaluation, each source should be assigned one of the following decisions.

```text
approved-for-collection
approved-for-manual-review
inspiration-only
research-later
exclude
```

### approved-for-collection

Safe and useful enough to automate collection.

### approved-for-manual-review

Useful, but requires human review before storage or use.

### inspiration-only

Useful for understanding patterns, but not suitable for storing phrases directly.

### research-later

Potentially useful, but not enough information yet.

### exclude

Not suitable due to poor quality, unclear rights, technical issues, or excessive risk.

---

## Recommended Source Research Table

Each researched source should be recorded using this structure.

```markdown
| Source | Phrase Categories | Quality | Recognition | Adaptability | Legal Risk | License Clarity | Technical Access | Structure | Automation | Freshness | Reliability | Decision | Notes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| Example Source | Idioms, Proverbs | 8 | 8 | 7 | 2 | 7 | 6 | 5 | 5 | 4 | 7 | research-later | Placeholder only |
```

---

## Category-Specific Research Questions

Each phrase category needs its own research angle.

---

## 1. Snowclones / Plug-In Phrases

Research questions:

- Are there existing snowclone dictionaries or datasets?
- Which phrase patterns are common in POD, memes, and social media?
- Can patterns be inferred from repeated phrase variants?
- Which patterns have clear replaceable slots?

Preferred source qualities:

- Template-like structure
- Multiple examples per pattern
- Clear slot behavior
- High adaptability

Likely collection approach:

```text
Manual curation + LLM-assisted expansion + pattern mining from phrase lists
```

---

## 2. Idioms

Research questions:

- Which idiom lists have clear licensing?
- Are there public-domain idiom collections?
- Which idioms are short enough for designs?
- Which idioms can be literalized visually?

Preferred source qualities:

- Stable expressions
- Meaning explanations separated from phrase text
- Cultural familiarity
- Low commercial risk

Likely collection approach:

```text
Structured lists + manual filtering + scoring
```

---

## 3. Famous Quotes

Research questions:

- Which quote sources clearly distinguish public-domain from modern quotes?
- How reliable are attributions?
- Which quotes are actually reusable patterns rather than fixed statements?
- Which should be inspiration-only?

Preferred source qualities:

- Public-domain status
- Reliable attribution
- Date/death-year metadata
- Short quote length

Likely collection approach:

```text
Public-domain-first + high manual review
```

---

## 4. Song Titles

Research questions:

- Which metadata APIs or datasets can list song titles legally?
- Are titles alone useful as phrase patterns?
- Which titles are generic enough to inspire transformed patterns?
- Which titles are too closely tied to an artist or brand?

Preferred source qualities:

- Title-only metadata
- Popularity indicators
- Genre/year metadata
- No lyric scraping

Likely collection approach:

```text
Metadata-only research + high transformation requirement
```

---

## 5. Song Lyrics

Research questions:

- Should lyrics be stored at all, or only used as inspiration?
- Which lyric-like public-domain texts are available?
- How can the system avoid copying protected lyrics?
- Can the system extract rhythm or structure without storing lyric text?

Preferred source qualities:

- Public-domain lyrics only, or no direct storage
- Strong transformation rules
- Inspiration-only handling for modern lyrics

Likely collection approach:

```text
Avoid direct modern lyric storage; use inspiration-only or public-domain sources
```

---

## 6. Movie and TV Catchphrases

Research questions:

- Which catchphrase sources are reliable?
- Which phrases are trademarked or franchise-associated?
- Which phrases should be inspiration-only?
- Can the system store structural variants instead of exact quotes?

Preferred source qualities:

- Source attribution
- Popularity/recognition indicators
- Clear separation of quote and source

Likely collection approach:

```text
Manual review + high legal risk scoring + transformation-first use
```

---

## 7. Advertising Slogans

Research questions:

- Which slogans are active trademarks?
- Which are historical but still protected?
- Should slogans be stored as examples of structure only?
- Can slogan mechanics be extracted without storing direct commercial slogans?

Preferred source qualities:

- Brand attribution
- Date/campaign metadata
- Trademark awareness

Likely collection approach:

```text
Inspiration-only by default; store derived structural patterns where possible
```

---

## 8. Proverbs

Research questions:

- Which proverb collections are public domain?
- Which proverbs are culturally familiar to the target audience?
- Which are short enough for POD?
- Which can be twisted into fantasy, gaming, or workplace humor?

Preferred source qualities:

- Traditional origin
- Public-domain or permissive use
- Cultural notes
- Short form variants

Likely collection approach:

```text
Public-domain collections + normalization + variant merging
```

---

## 9. Internet Memes

Research questions:

- Which meme phrase formats are adaptable and durable?
- Which sources have stable meme metadata?
- Which memes are too visual-context-dependent?
- Which meme phrases have identifiable creators or legal restrictions?

Preferred source qualities:

- Template format
- Origin information
- Popularity indicators
- Recency indicators

Likely collection approach:

```text
Manual curation + trend-aware research + risk flagging
```

---

## 10. Workplace and Everyday Expressions

Research questions:

- Which everyday expressions are broadly recognized?
- Which are useful for identity humor?
- Can they be collected from corpora, phrase lists, or manual curation?
- Which expressions are too generic to be worth storing?

Preferred source qualities:

- Common usage
- Low legal risk
- High relatability
- Strong niche adaptation potential

Likely collection approach:

```text
Manual curation + corpus-informed expansion + LLM-assisted enrichment
```

---

## 11. Contrasting Pair Templates

Research questions:

- Which comparison structures are common in POD?
- Which templates support strong visual hierarchy?
- Which patterns work across niches?
- Can these be generated from structural rules rather than harvested?

Preferred source qualities:

- Template clarity
- High adaptability
- Low legal risk
- Strong designability

Likely collection approach:

```text
Generate as structural templates + validate with examples
```

---

## 12. Fake Authority Templates

Research questions:

- Which faux-certification patterns are common?
- Which patterns are generic enough to use safely?
- Which official-looking terms should be avoided?
- How can these map to badge/seal design layouts?

Preferred source qualities:

- Generic authority structure
- Low direct-source dependency
- Strong identity humor

Likely collection approach:

```text
Template generation + manual curation
```

---

## 13. Warning and Signage Templates

Research questions:

- Which sign and label structures are generic?
- Which warning phrases are public/common language?
- Which safety sign standards should not be copied too closely?
- Which templates work best visually?

Preferred source qualities:

- Generic sign language
- Strong visual format
- Low source dependency
- Clear template structure

Likely collection approach:

```text
Template generation + common signage language + manual review
```

---

## 14. Structural Linguistic Patterns

Research questions:

- Which sentence architectures are common and flexible?
- Which structures feel familiar without being tied to a single protected source?
- Which patterns can generate original phrases?
- How should structural patterns be represented in the database?

Preferred source qualities:

- Reusable grammar pattern
- Multiple slot positions
- Low legal risk
- Strong originality potential

Likely collection approach:

```text
Manual curation + LLM-assisted generation + examples from approved phrase records
```

---

## Source Record Schema

Every researched source should be stored using a structured source record.

```json
{
  "id": "",
  "name": "",
  "url": "",
  "sourceType": "",
  "phraseCategories": [],
  "license": "",
  "termsNotes": "",
  "accessMethod": "",
  "technicalNotes": "",
  "qualityScore": 1,
  "recognitionScore": 1,
  "adaptabilityScore": 1,
  "legalRiskScore": 1,
  "licenseClarityScore": 1,
  "technicalAccessScore": 1,
  "structureScore": 1,
  "automationScore": 1,
  "freshnessScore": 1,
  "reliabilityScore": 1,
  "decision": "research-later",
  "reviewedAt": "",
  "reviewedBy": "",
  "notes": ""
}
```

---

## Early Source Strategy

The safest early strategy is to prioritize categories that do not depend heavily on modern copyrighted works or brand-specific slogans.

Recommended early focus:

1. Structural Linguistic Patterns
2. Snowclones / Plug-In Phrases
3. Workplace and Everyday Expressions
4. Contrasting Pair Templates
5. Fake Authority Templates
6. Warning and Signage Templates
7. Idioms
8. Proverbs

These categories are more likely to generate original phrases with lower commercial risk.

Recommended later focus:

1. Internet Memes
2. Song Titles
3. Famous Quotes
4. Movie and TV Catchphrases
5. Song Lyrics
6. Advertising Slogans

These can be valuable, but they need more careful source research and stronger legal-risk handling.

---

## Source Usage Modes

Not all sources should be used the same way.

### Direct Collection

The source can be imported into the database with metadata.

Best suited for:

- Public-domain proverbs
- Common idioms
- Generic structural patterns
- Manually curated internal templates

### Manual Review Collection

The source can provide candidates, but every phrase requires human review.

Best suited for:

- Quotes
- Catchphrases
- Memes
- Ambiguous phrase lists

### Inspiration Only

The source should not be stored directly, but can inform pattern design.

Best suited for:

- Advertising slogans
- Modern song lyrics
- Highly distinctive franchise quotes
- Trademark-heavy sources

### Excluded

The source should not be used.

Reasons:

- Unclear license
- Poor quality
- Excessive legal risk
- Technical restrictions
- Terms of service conflict
- Too much noise

---

## Research Workflow

Recommended workflow:

```text
Identify candidate source
  ↓
Record source metadata
  ↓
Evaluate using scoring criteria
  ↓
Assign decision category
  ↓
Map source to phrase categories
  ↓
Define usage mode
  ↓
Add source to source research table
  ↓
Prioritize for collector implementation
```

---

## Minimum Requirements Before Building a Collector

Before implementing a collector for any source, confirm:

1. The source has enough useful phrase content.
2. The source license or terms are understood well enough.
3. The access method is technically feasible.
4. The source maps clearly to one or more phrase categories.
5. The expected output can fit the phrase record schema.
6. The collection process does not require excessive manual cleanup.
7. The source has a clear decision value other than `research-later`.

---

## Manual Seed Source

Before external collection begins, the project should support a manually curated seed source.

This source can include phrases added directly by the user during brainstorming.

Suggested source record:

```json
{
  "id": "manual-seed",
  "name": "Manual Seed List",
  "sourceType": "internal-manual",
  "phraseCategories": ["all"],
  "license": "internal",
  "accessMethod": "manual-entry",
  "decision": "approved-for-collection",
  "notes": "User-curated phrases and patterns collected during FusionCanvas ideation."
}
```

This allows the system to start delivering value before external source research is complete.

---

## First Research Milestone

The first research milestone should produce:

1. At least 3 candidate sources for idioms.
2. At least 3 candidate sources for proverbs.
3. At least 3 candidate approaches for snowclones.
4. At least 3 candidate approaches for structural linguistic patterns.
5. A decision on whether modern song lyrics should be excluded from direct storage.
6. A decision on whether advertising slogans should be inspiration-only by default.
7. A prioritized list of the first 3 collectors to implement.

---

## Suggested First Collectors

Based on risk, usefulness, and implementation simplicity, the first collectors should probably be:

1. Manual Seed Collector
2. Structural Linguistic Pattern Collector
3. Idiom Collector
4. Proverb Collector
5. Snowclone Collector

The first collector should be manual because it validates the data model before introducing source-specific complexity.

---

## Open Questions

The following questions should be answered during source research:

- Should source text be stored verbatim, or only normalized phrase records?
- Should high-risk phrases be stored but blocked from direct use?
- Should the database separate inspiration records from usable records?
- Should there be a `commercialUseAllowed` field?
- Should every generated adaptation keep a link back to the source pattern?
- Should popularity or search-volume data be integrated later?
- Should Etsy/Amazon/Redbubble phrase examples be used only for competitive research rather than collection?
- Should the database include non-English phrase sources later?
- Should the system track whether a phrase is overused in POD?

---

## Recommended Next Step

Before writing production collectors, create a small manual seed dataset with approximately 100 phrase patterns across the lowest-risk categories.

Suggested distribution:

```text
25 Structural Linguistic Patterns
20 Snowclones / Plug-In Phrases
15 Workplace and Everyday Expressions
15 Contrasting Pair Templates
10 Fake Authority Templates
10 Warning and Signage Templates
5 Idioms or Proverbs
```

This will test whether the schema, scoring system, slot model, and review workflow are practical.

---

## Guiding Principle

The source strategy should favor phrase mechanics over phrase copying.

The best sources are not necessarily the ones with the most famous phrases. The best sources are the ones that reveal reusable structures that can be transformed into original, niche-relevant, commercially safer designs.
