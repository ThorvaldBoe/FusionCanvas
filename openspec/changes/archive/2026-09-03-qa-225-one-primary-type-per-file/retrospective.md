# One Primary Type per File Retrospective

## Outcome

The production source tree now has one top-level handwritten type per file, with Avalonia code-behind files retaining their framework-owned partial class and companion view models moved to type-named files. Runtime behavior and public signatures were preserved.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| A source split could treat `.axaml.cs` as an ordinary C# file name | Framework partial files must retain the `.axaml.cs` pairing | Preserve the code-behind path and split only the companion view model | Architecture / implementation defect | Reusable for Avalonia source splits | Captured in design and architecture delta |

## Learning Review

- Result: reusable lesson identified.
- Evidence reviewed: source-layout scan, focused test, solution build, existing coding standard, and architecture guideline delta.
- Promotions completed: the App-layer scope and framework exceptions were captured in the active architecture delta.
