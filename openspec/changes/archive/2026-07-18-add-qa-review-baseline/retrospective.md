# add-qa-review-baseline Retrospective

## Outcome

The FusionCanvas QA review process is defined and accepted. The change produced three durable artifacts:

1. `docs/qa-review.md` — the operational playbook with QA-1 through QA-5 tasks, the review protocol, severity scale, standard report format, and fix-routing rules.
2. `AGENTS.md` — the shared agent guide that routes QA requests to the playbook and documents the OpenSpec workflow, architecture rules, testing baseline, and security expectations.
3. `openspec/specs/qa-review-baseline/spec.md` — the accepted capability spec capturing the durable QA requirements (process is documented and repeatable; design/architecture conformance; testing baseline; security hygiene; drift detection; finding routing).

The first full QA review was executed on 2026-07-18 and produced a consolidated report with findings across all five QA areas. The findings were routed per the playbook's fix table: direct-maintenance fixes (dead code, doc drift, `.gitignore` gap, xUnit1051 warnings, patch-version package updates) were applied as separate commits, and this archival completes the OpenSpec ceremony for the QA process itself.

## Feedback-Driven Adjustments

| Initial assumption | Evidence | Correction | Classification | Applicability | Promotion |
|---|---|---|---|---|---|
| The QA playbook alone is sufficient | AGENTS.md needed to route agents to the playbook and bind the OpenSpec workflow | Added AGENTS.md as the shared entry point for all agents | Ordinary implementation defect | All agents | Promoted to AGENTS.md and the spec's "process is documented" requirement |
| OpenCode and Codex need separate skill setups | Both agents use the same `.codex/skills/` directory | Registered `.codex/skills` in `opencode.json` so OpenCode loads the same skills | One-off preference | Tooling-specific | None — `opencode.json` is configuration |

## Learning Review

- **Result:** no reusable lessons identified beyond the artifacts themselves.
- **Evidence reviewed:** the change proposal, design, delta spec, and tasks; the first full QA review report (`TestResults/qa/2026-07-18-qa-review.md`); the fix commits applied from the report's verdict.
- **Promotions completed:** the durable QA requirements are promoted to `openspec/specs/qa-review-baseline/spec.md` via this change's delta. The playbook (`docs/qa-review.md`) remains living documentation that can evolve without OpenSpec ceremony, as the spec explicitly states.
- **Deferred promotions:** none. UI/UX guidelines and architecture guidance are not affected by a process change.
