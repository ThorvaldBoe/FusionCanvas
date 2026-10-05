# Build Windows Package Pipeline Retrospective

## Outcome

The module was merged through PR #814 as commit `053af3dc6e26c3ef5529b4cfc78eafd56cdd3b40`. The merged `main` workflow passed its deterministic gate, published a self-contained Windows candidate package, generated its checksum, created the provenance attestation, and uploaded `FusionCanvas-0.2.0-win-x64`.

## Feedback-Driven Adjustments

| Initial assumption | Evidence | Correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| The informational assembly version could be used directly as a three-part artifact version. | Local publish produced `0.2.0.556+eabbd3c3f9`, which is an assembly/build representation rather than the canonical package SemVer. | Read `NuGetPackageVersion` from Nerdbank.GitVersioning's `GetBuildVersion` target and validate SemVer. | Ordinary implementation defect | All future package/release workflows | Preserved in the active design and verification evidence. |
| A solution restore was sufficient before a `win-x64` `--no-restore` publish. | Local RID publish failed with `NETSDK1047` until the app was restored with `--runtime win-x64`. | Keep RID-aware restore directly before the self-contained publish. | Reusable engineering lesson | Windows RID packaging workflows | Preserved in the active design and workflow. |
| The standard merge wrapper could complete delivery from this worktree. | `gh pr merge` attempted to use `main`, which is checked out by another local worktree. | Used GitHub's merge API after confirming the PR was clean and all required checks passed. | One-off delivery tooling issue | This local worktree layout only | Deferred; no repository rule needed. |

## Learning Review

- Result: reusable lessons identified.
- Evidence reviewed: proposal, design, delta specification, tasks, local build/test/publish output, PR #814 checks, merged `main` workflow 37196470918, and the final uploaded artifact.
- Promotions completed: package version source and RID-aware restore remain documented in the active design/specification and workflow.
- Deferred promotions: none; the merge-wrapper workaround is local delivery context rather than a repository rule.
