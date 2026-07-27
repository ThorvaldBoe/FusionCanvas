# Phrase Intelligence Acquisition Batch 005

## Scope

Batch 005 screens the 21 pop-culture seed/pattern rows in the public
`chrome_extension/patterns` file from the CATCHPHRASE research repository,
released with the ACL-IJCNLP 2021 paper *Catchphrase: Automatic Detection of
Cultural References*.

The batch adds 13 previously absent slot-bearing structures. It excludes:

- four structures already represented in the repository:
  `I love the smell of X in the morning`,
  `the first rule of X is you do not talk about X`,
  `Dude, where's my X?`, and `Honey, I shrunk the X`;
- three fixed phrases whose upstream patterns contain no wildcard:
  `live long and prosper`, `what is dead may never die`, and
  `a girl has no name`; and
- `you're a X`, because the residual frame is too broad to carry a useful,
  distinctive snowclone structure.

The batch is stored in:

```text
data/phrase-intelligence/sources/acquisition-batch-005.sources.v2.jsonl
data/phrase-intelligence/patterns/acquisition-batch-005.patterns.v2.jsonl
```

The reproducible batch definition and writer are stored in:

```text
tools/New-AcquisitionBatch005.ps1
```

## Sources

- Dataset repository:
  <https://github.com/NSweed/CATCHPHRASE>
- Peer-reviewed paper:
  <https://aclanthology.org/2021.acl-short.1/>

The paper is published under CC BY 4.0 through the ACL Anthology, but the
dataset repository does not declare a license. The records therefore use
`sourceLicense: "unknown"` and do not infer dataset rights from the paper's
publication license.

## Acquisition Posture

Every retained structure is derived from recognizable pop-culture wording.
All source and pattern records therefore use:

- `recommendedUsageMode: "pattern-extraction-only"`;
- `collectionRisk: "high"`;
- `commercialUseRisk: "high"`;
- `directUseAllowed: false`;
- `requiresReviewBeforeUse: true`;
- `requiresAttribution: true`; and
- `reviewStatus: "needs-review"`.

Before adapting a structure, review its source quotation, franchise or
trademark associations, dataset rights, transformation distance, originality,
audience suitability, and marketplace rules.

## Normalization Notes

- Upstream wildcard positions are represented as bracketed `[X]` and `[Y]`
  slots.
- Source quotations and example realizations are not copied into the records.
- Fixed wording is retained where it defines the recognizable cadence.
- No direct-use approval or example product copy is included.
