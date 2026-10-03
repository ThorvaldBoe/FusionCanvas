## 1. Readiness projection

- [x] 1.1 Add typed Offering readiness state/issue records in `FusionCanvas.Application.Catalog`, preserving template-specific `MockupTemplateReadinessBlocker` evidence.
- [x] 1.2 Implement the read-only Offering readiness builder using active same-Offering catalog records and the existing Mockup Template readiness evaluator.
- [x] 1.3 Extend Offering summary loading to return readiness for both Offering overview cards and focused Offering detail without changing persistence or eligibility behavior.

## 2. Store presentation

- [x] 2.1 Extend Offering presentation models with precise catalog/mockup status, ready-template count, and compact issue guidance.
- [x] 2.2 Add the focused Store Offering readiness panel with empty, attention-needed, and ready states, accessible names, and progressive disclosure consistent with the existing catalog editor.
- [x] 2.3 Preserve existing draft/navigation safeguards and confirm no new readiness control mutates catalog records.

## 3. Verification

- [x] 3.1 Add application tests for complete readiness, missing Variants/Design Areas/Templates, multiple Draft templates, archived/read-only review, malformed/stale data handling, and no-mutation behavior.
- [x] 3.2 Add presentation-model tests and deterministic Avalonia headless coverage for readiness state, issue visibility, and accessible guidance.
- [x] 3.3 Run focused tests and review failures against each acceptance scenario; correct implementation or approved artifacts as needed.
- [x] 3.4 Run `openspec validate catalog-offering-readiness-guidance --strict`.
- [ ] 3.5 Run the baseline `dotnet test .\FusionCanvas.sln` after restoring dependencies if required, and record any environment limitation separately from product failures.
