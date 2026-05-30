# Phrase Pattern Database Specification

## Background

FusionCanvas is intended to support a structured creative workflow for turning rough ideas into stronger concepts, designs, and listings. One important part of that workflow is phrase ideation: finding familiar linguistic patterns that can be adapted into niche-specific phrases for Print-on-Demand, product concepts, marketing copy, and design exploration.

Many successful POD phrases are not created from scratch. They often borrow recognizable structures from everyday language, pop culture, idioms, slogans, memes, song titles, quote patterns, signage, and other familiar phrase forms. The creative value comes from combining recognition with a niche-specific twist.

Example:

```text
Original pattern:
Keep Calm and Carry On

Niche adaptation:
Keep Calm and Roll Initiative
```

The purpose of this document is to define the phrase types that should be collected into a reusable Phrase Pattern Database.

---

## Purpose

The Phrase Pattern Database should become a structured source of reusable phrase patterns for ideation, concept generation, and design refinement.

The database should help answer questions such as:

- What recognizable phrase patterns can be adapted to a specific niche?
- Which phrases have one or more replaceable slots?
- Which patterns are most suitable for humor, identity, sarcasm, nostalgia, or contrast?
- Which phrase types carry higher legal or originality risk?
- Which patterns are most useful for POD design concepts?

The database should not simply collect famous phrases. It should collect phrase structures that can generate new, niche-relevant ideas.

---

## Core Creative Principle

A strong adapted phrase often combines four elements:

1. Familiarity — the audience recognizes the original pattern.
2. Relevance — the adaptation clearly fits the niche.
3. Surprise — the inserted or replaced word creates a twist.
4. Designability — the phrase can be turned into a clear visual composition.

The most valuable phrase patterns are those that are both recognizable and flexible.

---

## Phrase Type Categories

The system should support the following 14 phrase types.

---

## 1. Snowclones / Plug-In Phrases

### Definition

Semi-fixed phrase templates containing one or more replaceable slots.

These are among the most valuable patterns because they are naturally designed for substitution.

### Examples

```text
Easily Distracted By [X]
Trust Me, I'm A [X]
[X] Is My Cardio
World's Okayest [X]
Professional [X]
Born To [X], Forced To [Y]
Powered By [X]
```

### Notes

Snowclones should explicitly store their slot structure.

Example metadata:

```json
{
  "phrase": "Born To [X], Forced To [Y]",
  "category": "snowclone",
  "slots": ["X", "Y"],
  "adaptabilityScore": 10
}
```

---

## 2. Idioms

### Definition

Common expressions whose meaning is not purely literal.

### Examples

```text
Bite The Bullet
Back To Square One
Hit The Nail On The Head
Walking On Thin Ice
Barking Up The Wrong Tree
```

### Notes

Idioms are useful because they can be literalized, twisted, or combined with niche imagery.

Example:

```text
Barking Up The Wrong Dungeon
```

---

## 3. Famous Quotes

### Definition

Widely recognized quotations attributed to historical figures, authors, public figures, fictional characters, or notable works.

### Examples

```text
To Be Or Not To Be
I Have A Dream
That's One Small Step For Man
I'll Be Back
```

### Notes

Famous quotes may have high recognition value but may also require substantial transformation before commercial use.

---

## 4. Song Titles

### Definition

Recognizable titles of songs.

### Examples

```text
Born To Be Wild
Highway To Hell
Livin' On A Prayer
Sweet Child O' Mine
Another One Bites The Dust
```

### Notes

Song titles are often short, memorable, rhythmic, and highly adaptable.

Example adaptation:

```text
Livin' On A Death Save
```

---

## 5. Song Lyrics

### Definition

Recognizable lyrical fragments or lyric-like phrases.

### Examples

```text
Make Love Not War
Hit Me Baby One More Time
We Will Rock You
```

### Notes

Lyrics often have strong rhythm and emotional resonance, but they usually carry higher legal risk than general phrase templates.

For commercial use, the system should treat lyrics primarily as inspiration and encourage substantial transformation.

---

## 6. Movie and TV Catchphrases

### Definition

Recognizable lines strongly associated with films, television shows, or fictional characters.

### Examples

```text
I'll Be Back
Winter Is Coming
Say Hello To My Little Friend
Yippee Ki Yay
```

### Notes

These can be powerful because they carry strong cultural associations. They should be flagged for legal and trademark risk when appropriate.

---

## 7. Advertising Slogans

### Definition

Phrases created as marketing slogans, brand taglines, or campaign lines.

### Examples

```text
Just Do It
Got Milk?
Think Different
Because You're Worth It
```

### Notes

Advertising slogans are short and memorable by design. They often have high commercial/legal sensitivity and should be used carefully.

Example adaptation:

```text
Got Mana?
```

---

## 8. Proverbs

### Definition

Traditional sayings expressing common wisdom or cultural lessons.

### Examples

```text
Better Late Than Never
Fortune Favors The Bold
A Bird In The Hand Is Worth Two In The Bush
Curiosity Killed The Cat
```

### Notes

Proverbs are useful because they are familiar, broadly understood, and often old enough to be culturally embedded rather than tied to a modern brand or work.

---

## 9. Internet Memes

### Definition

Phrase patterns originating from internet culture, image macros, social platforms, forums, or viral formats.

### Examples

```text
One Does Not Simply...
Change My Mind
This Is Fine
Hold My Beer
Tell Me Without Telling Me
```

### Notes

Memes are highly adaptable and often very useful for modern POD humor. They may have varying levels of ownership, association, or cultural sensitivity.

---

## 10. Workplace and Everyday Expressions

### Definition

Common phrases used in daily life, workplaces, family life, routines, and casual conversation.

### Examples

```text
Living The Dream
It Is What It Is
Not My Problem
That's Above My Pay Grade
Another Day At The Office
```

### Notes

These are often extremely useful because they are relatable and usually less tied to a specific copyrighted work.

---

## 11. Contrasting Pair Templates

### Definition

Phrase structures built around comparison, contrast, preference, or opposition.

### Examples

```text
Less [X], More [Y]
[X] > [Y]
Not [X], Just [Y]
I Came For [X], Stayed For [Y]
More [X], Fewer [Y]
```

### Notes

These templates are especially useful for POD because they create a clear visual hierarchy and can be adapted to almost any niche.

Example:

```text
Less Drama, More XP
```

---

## 12. Fake Authority Templates

### Definition

Phrase patterns that imply expertise, certification, status, membership, or official authority in a humorous or identity-based way.

### Examples

```text
Officially Certified [X]
Department Of [X]
Licensed [X]
University Of [X]
Certified Professional [X]
```

### Notes

These work well for badges, seals, stamps, faux institutions, and identity-led designs.

---

## 13. Warning and Signage Templates

### Definition

Phrase patterns inspired by warning signs, safety labels, official notices, instructions, and public signage.

### Examples

```text
Warning:
Caution:
Authorized Personnel Only
Do Not Disturb
Enter At Your Own Risk
```

### Notes

These provide strong visual direction and are highly compatible with badge, label, and sign-style designs.

---

## 14. Structural Linguistic Patterns

### Definition

Reusable sentence architectures that feel familiar even when they are not tied to one specific quote, idiom, slogan, or source phrase.

These are broader phrase structures that can generate original-sounding phrases while preserving a recognizable rhythm or rhetorical shape.

### Examples

```text
Keep [X], I'll Take [Y]
[X] Called, I Didn't Answer
I Came, I Saw, I [X]
No [X], No Problem
Too [X] To [Y]
Here For The [X], Not The [Y]
My [X], My Rules
```

### Notes

This may become one of the most valuable categories for FusionCanvas because it supports original phrase generation without relying too heavily on specific copyrighted or trademarked phrases.

These patterns should be treated as reusable phrase architectures.

Example adaptation:

```text
Reality Called, I Didn't Answer
Here For The XP, Not The Drama
My Dungeon, My Rules
```

---

## Recommended Metadata Model

Each phrase or phrase pattern should contain structured metadata.

```json
{
  "id": "",
  "phrase": "",
  "normalizedPhrase": "",
  "category": "",
  "source": "",
  "sourceType": "",
  "recognitionScore": 1,
  "adaptabilityScore": 1,
  "slotCount": 0,
  "slots": [],
  "tone": [],
  "legalRisk": "",
  "exampleAdaptations": [],
  "notes": ""
}
```

### Field Descriptions

- `id`: Stable unique identifier.
- `phrase`: Original phrase or pattern.
- `normalizedPhrase`: Consistent lowercase or canonical form for duplicate detection.
- `category`: One of the 14 phrase categories.
- `source`: Known source, if applicable.
- `sourceType`: Public domain, pop culture, brand slogan, traditional, unknown, etc.
- `recognitionScore`: Estimated familiarity from 1 to 10.
- `adaptabilityScore`: Estimated usefulness for niche adaptation from 1 to 10.
- `slotCount`: Number of replaceable positions.
- `slots`: Slot descriptions, such as role, object, activity, emotion, location, creature, class, profession, etc.
- `tone`: Humor, sarcasm, inspirational, nostalgic, dark, cozy, rebellious, absurd, etc.
- `legalRisk`: Low, medium, high, or unknown.
- `exampleAdaptations`: Sample niche-specific transformations.
- `notes`: Any additional remarks.

---

## Suggested Collection Strategy

Use separate collectors or sub-jobs for each major category rather than one generic collector.

Recommended collectors:

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

Each collector should:

1. Gather candidate phrases.
2. Normalize formatting.
3. Remove obvious duplicates.
4. Classify the phrase type.
5. Identify possible slots.
6. Estimate recognition and adaptability.
7. Flag legal risk.
8. Generate a small number of example adaptations.

---

## Recommended Priority for POD Use

Suggested priority order:

1. Snowclones / Plug-In Phrases
2. Structural Linguistic Patterns
3. Workplace and Everyday Expressions
4. Internet Memes
5. Warning and Signage Templates
6. Fake Authority Templates
7. Contrasting Pair Templates
8. Idioms
9. Proverbs
10. Movie and TV Catchphrases
11. Song Titles
12. Song Lyrics
13. Advertising Slogans
14. Famous Quotes

This prioritization favors categories that are adaptable and likely to produce original niche phrases with lower commercial risk.

---

## Long-Term Vision

The Phrase Pattern Database should become a reusable knowledge source for:

- FusionCanvas concept generation
- POD design ideation
- Phrase refinement
- Design scoring
- Listing title generation
- Marketing copy generation
- Niche adaptation
- AI-assisted brainstorming

The database should focus on phrase utility, adaptability, and creative potential rather than merely collecting famous phrases.

The ultimate goal is to help generate phrases that feel familiar enough to be instantly understood, but original enough to work as fresh niche-specific design concepts.
