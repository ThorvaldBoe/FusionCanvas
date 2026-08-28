## 1. Provider catalog presentation state

- [x] 1.1 Add the App-only provider catalog load-state enum and stable instruction/state/recovery properties.
- [x] 1.2 Classify loading, available, empty, unavailable, and error outcomes without changing the provider candidate contract.
- [x] 1.3 Preserve existing candidate selection and compatibility messages while preventing fabricated candidates.

## 2. Explicit selection guidance

- [x] 2.1 Add a persistent **Provider mockup image** label and matching accessible selector name.
- [x] 2.2 Keep provider-catalog provenance and unsupported local upload/drag-drop guidance visible in every state.
- [x] 2.3 Show state-specific text and provider setup/sync recovery for empty, unavailable, and error outcomes.

## 3. Tests and completion

- [x] 3.1 Add ViewModel tests for controlled loading, available, empty, unavailable, and throwing sources.
- [x] 3.2 Add Avalonia headless tests for visible instructions, selector accessibility, state guidance, and absence of upload/drop affordances.
- [x] 3.3 Run focused tests, the full solution baseline, strict OpenSpec validation, and `git diff --check`; create criterion-level `verification.md`.
- [x] 3.4 Complete the learning review and retrospective for archive confirmation.
