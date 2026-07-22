# Adopt Module Delivery Workflow Retrospective

## Outcome

FusionCanvas now uses a rolling, one-module-at-a-time delivery process with collaborative discovery, implementation-ready OpenSpec packages, criterion-level acceptance evidence, capability-based agent routing, and risk-based desktop UI verification. LifeOS planning material remains preserved but is no longer required or authoritative. The process delta has been synced into the accepted workflow, testing, and QA specifications.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| Behavior-focused specs plus ordinary tasks were sufficiently detailed for implementation agents | The user reported mediocre results from cheaper models when specifications left important implementation decisions implicit | Keep durable specs behavior-focused, but require a dedicated detailed `Implementation Plan`, bounded tasks, and ambiguity escalation in every delivery package | Missing process requirement | Reusable across feature modules | Promoted to the workflow spec, project config, `AGENTS.md`, and propose/apply skills |
| Long-range PRDs could remain the default input to OpenSpec proposals | The large up-front inventory became stale and encouraged overly broad implementation batches | Plan only the next cohesive module; preserve LifeOS files as optional historical ideas without current authority | Planning/process correction | Reusable across future planning | Promoted to workflow spec, roadmap, project context, agent guide, and explore/propose skills |
| Proportional UI testing still meant covering every changed feature path on the desktop | Interactive UI verification is valuable but quota- and time-intensive | Target the module's critical workflow and distinct high-risk paths; cover equivalent low-risk variants deterministically; reserve full regression for milestone/release/full-QA gates | Testing strategy | Reusable across user-facing modules | Promoted to testing/QA specs, architecture, QA playbook, active UI-baseline change, and agent skills |
| The existing active UI-baseline change could be left untouched | Its unconditional wording could later be archived and reintroduce policy contradictions | Reconcile its proposal, design, delta specs, and tasks with the risk-based policy now | Specification drift correction | Change-specific reconciliation with reusable effect | Updated `add-ui-testing-baseline` active artifacts |
| The default parallel solution baseline would provide final verification | Parallel Avalonia compilation left generated App output locked by orphaned test/compiler processes | Stop only the processes created by the failed run and rerun the full baseline serialized with `-m:1` | Implementation/verification defect | Environment-specific | Recorded in `verification.md`; no durable policy promotion |

## Learning Review

- Result: reusable lessons identified and promoted.
- Evidence reviewed: user feedback, current accepted process/testing/QA specs, canonical contributor docs, shared OpenSpec skills, the active UI-testing change, strict OpenSpec validation, canonical wording scans, diff checks, and the full deterministic baseline.
- Promotions completed: rolling module scope, implementation-readiness rules, acceptance evidence, agent handoffs, risk-based desktop allocation, module-scoped QA, and historical-reference status were promoted to their narrowest durable sources.
- Deferred promotions: none.
