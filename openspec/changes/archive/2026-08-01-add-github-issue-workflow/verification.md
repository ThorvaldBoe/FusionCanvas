# Verification — add-github-issue-workflow

## Acceptance Evidence

| Acceptance scenario | Result | Evidence / limitation |
| --- | --- | --- |
| External contributor reports a bug | Pass (local) | `.github/ISSUE_TEMPLATE/bug_report.yml` has the required version, operating-system, expected/actual behavior, reproduction, and frequency fields; it assigns `type: bug` and `status: needs-triage`. |
| External contributor requests a feature | Pass (local) | `.github/ISSUE_TEMPLATE/feature_request.yml` has the required problem, workaround, outcome, affected-area, and alternatives fields; it assigns `type: feature` and `status: needs-triage`. |
| Sensitive-content guidance and blank-issue restriction | Pass (local) | Both forms contain the required public-data warning; `config.yml` sets `blank_issues_enabled: false`. |
| Small taxonomy and live repository setup | Pass | `gh repo view` confirmed Issues are enabled. `gh label list` confirmed the eleven documented labels exist; there were no pre-existing issues before unused default labels were removed. |
| Issue and OpenSpec authority split | Pass | The `github-issue-workflow` delta, modified `openspec-project-workflow` delta, `CONTRIBUTING.md`, and `AGENTS.md` consistently distinguish intake/delivery tracking from behavior/specification authority. |
| Direct bug, promoted feature, and split-module routes | Pass | `CONTRIBUTING.md` includes worked examples for all three routes and documents `## Origin`, child issues, branch names, and PR closing keywords. |
| Rendered Issue Form preview and disposable submissions | Deferred | GitHub only loads the new forms after these uncommitted files are pushed. After merge, submit one disposable issue through each form and confirm required validation, warning rendering, default labels, and the absence of a blank-issue option; then delete the disposable issues. |

## Validation Commands

| Command | Result |
| --- | --- |
| PowerShell Issue Form structural assertions | Pass |
| `openspec validate add-github-issue-workflow --strict` | Pass |
| `dotnet test .\\FusionCanvas.sln` | Pass — 255 tests, 0 failures; pre-existing xUnit analyzer warnings remain. |

## Scope Notes

- No application code, persistence, UI, or external API behavior changed; Avalonia headless view testing is not applicable.
- GitHub labels are a repository-side configuration, while Issue Forms and policy are version-controlled in this change.
