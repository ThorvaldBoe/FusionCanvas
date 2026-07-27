# Phrase Intelligence Acquisition Batch 004

## Scope

Batch 004 imports the 67 snowclone structures in the **Date unknown** section of
Wiktionary's `Appendix:English snowclones` page, supplied by a user on
2026-07-27.

The batch adds 66 new pattern records. It skips:

- `X, and all I got was this lousy Y`, because
  `I Survived [EVENT] And All I Got Was [OBJECT]` already represents the same
  reusable structure.

The batch is stored in:

```text
data/phrase-intelligence/sources/acquisition-batch-004.sources.v2.jsonl
data/phrase-intelligence/patterns/acquisition-batch-004.patterns.v2.jsonl
```

The reproducible batch definition and writer are stored in:

```text
tools/New-AcquisitionBatch004.ps1
```

## Acquisition Posture

The Wiktionary source is recorded as `CC-BY-SA-4.0` and attribution-required.
All patterns are candidates, are prohibited from direct use, and require review
before adaptation.

Most records use:

- `recommendedUsageMode: "collect-with-review"`;
- `collectionRisk: "low"`; and
- `commercialUseRisk: "medium"`.

The two conspicuously brand-derived structures use
`recommendedUsageMode: "pattern-extraction-only"` and high commercial-use risk:

- `Remember when [X]? Pepperidge Farm remembers.`
- `I can't believe it's not [X]`

## Normalization Notes

- Source variables `W`, `X`, `Y`, and `Z` are represented as bracketed slots.
- Source punctuation and grammatical suffixes are retained where structurally
  meaningful.
- The mojibake sequence in `X, Y, and Zâ€”pick any two` was repaired to an em
  dash.
- The explanatory `boys will be boys` link was not treated as a separate source
  entry.
