# User-Submitted Snowclone Acquisition — 2026-07-27

## Scope

This acquisition imports the snowclone catalog supplied directly by the user on
2026-07-27. The submitted text contained 380 non-empty candidate lines after
the catalog header was removed.

The import produced:

- 366 unique slot-bearing pattern records;
- 13 skipped structural duplicates, including duplicates within the submission
  and patterns already present in the v2 snowclone seed data; and
- 1 skipped incomplete fragment (`one giant leap?`) with no recognizable
  replaceable slot.

The records are stored in:

```text
data/phrase-intelligence/sources/submitted-snowclones-2026-07-27.sources.v2.jsonl
data/phrase-intelligence/patterns/submitted-snowclones-2026-07-27.patterns.v2.jsonl
```

The repeatable importer is stored in:

```text
tools/Import-SubmittedSnowclones.ps1
```

## Normalization

The importer:

- converts legacy metasyntactic variables such as `X`, `Y`, and `N` to explicit
  v2 slots such as `[X]`, `[Y]`, and `[N]`;
- handles compact morphological forms such as `Xgate`, `LolX`, and `SchadenX`;
- removes URLs and editorial annotations from the pattern text;
- preserves meaningful alternatives and optional structural wording;
- compares canonicalized slot structures against all existing v2 pattern files;
  and
- emits stable sequential IDs and a shared source reference.

## Review Posture

The submission includes structures associated with films, television,
advertising, memes, songs, books, titles, and proverbs. Their presence in this
database is not approval for commercial use.

All imported source and pattern records therefore use:

- `sourceTier: "tier-4"`;
- `recommendedUsageMode: "pattern-extraction-only"`;
- `collectionRisk: "high"`;
- `commercialUseRisk: "high"`;
- `directUseAllowed: false`;
- `requiresReviewBeforeUse: true`; and
- `reviewStatus: "needs-review"`.

Before using an adaptation, review its provenance, legal and marketplace risk,
originality, source-specific wording, and any attribution requirement.

## Reproducing The Import

Run the importer with the original submitted text:

```powershell
.\tools\Import-SubmittedSnowclones.ps1 -InputPath <path-to-pasted-text.txt>
```
