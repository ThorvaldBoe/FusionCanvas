## Context

FusionCanvas uses a roadmap and LifeOS PRD files as planning source material, while accepted behavior belongs under `openspec/specs`. Active changes currently live under `openspec/changes`, but the workflow that connects PRD items, OpenSpec proposals, implementation, validation, and archive is not itself captured as accepted project behavior.

This change defines a lightweight contributor workflow. It is intentionally project-process documentation, not application runtime behavior.

## Goals / Non-Goals

**Goals:**

- Make the OpenSpec workflow clear enough that future roadmap items can be converted into reviewable changes.
- Preserve the distinction between planning documents, active changes, accepted specs, and archived history.
- Define the minimum expected contents of a feature specification.
- Keep the workflow usable for Phase 1 and later features without requiring heavyweight approvals or external tooling.

**Non-Goals:**

- Implement issue tracker integration, release management, or contributor governance.
- Replace the existing OpenSpec CLI workflow.
- Move PRD documents into accepted specs without review.
- Add application UI, domain model, persistence, or plugin behavior.

## Decisions

### Document Workflow as an OpenSpec Capability

The project workflow will be captured as an OpenSpec capability named `openspec-project-workflow`.

Rationale: The workflow affects how every future feature becomes accepted behavior, so it should live in the same source-of-truth system it governs. An alternative would be a standalone contributor guide, but that would leave the workflow outside the accepted spec archive.

### Keep PRDs as Planning Input

The specification will state that roadmap and PRD documents are planning references, not accepted OpenSpec specifications.

Rationale: This keeps historical product thinking available while preventing broad PRD content from becoming binding behavior accidentally. An alternative would be to import PRDs directly into `openspec/specs`, but that would skip proposal review and mix aspirational roadmap content with accepted requirements.

### Require Lightweight, Testable Specification Content

Feature specs will require enough detail to guide implementation: user-facing behavior, requirements, scope, acceptance criteria, open questions, and related notes where relevant.

Rationale: The workflow should prevent vague implementation while avoiding process drag. An alternative would be a strict, exhaustive template for every feature, but that would be too heavy for small changes.

### Preserve Completed and Superseded Context

Completed or superseded changes will be archived or otherwise preserved rather than deleted.

Rationale: FusionCanvas depends on product reasoning over time, especially as PRD items are converted into accepted behavior. An alternative would be to keep only the current spec set, but that would make it harder to understand why behavior changed.

## Risks / Trade-offs

- Workflow feels heavier than needed for small changes -> Treat the workflow as required for significant feature behavior and keep small maintenance changes lightweight.
- Contributors confuse PRDs with accepted specs -> State the distinction explicitly in the spec and proposal process.
- Archive structure evolves later -> Require preservation now while leaving exact future archive refinements open to the existing OpenSpec tooling.
- Process docs drift from CLI behavior -> Prefer OpenSpec CLI commands and resolved paths in contributor tasks instead of hard-coding assumptions beyond the repository layout.
