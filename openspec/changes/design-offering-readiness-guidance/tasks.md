## 1. Design projection

- [x] 1.1 Extend `DesignStageState` with optional normalized Offering readiness.
- [x] 1.2 Calculate readiness from the current snapshot through `OfferingReadinessBuilder`, preserving legacy and stale fallbacks.

## 2. Design presentation

- [x] 2.1 Expose compact readiness status, ready-template count, and translated issue guidance on `DesignStageToolViewModel`.
- [x] 2.2 Add the accessible read-only readiness panel before the Design color/slot workflow.
- [x] 2.3 Preserve artwork-generation readiness, stale recovery, and mutation safeguards.

## 3. Verification

- [x] 3.1 Add application tests for incomplete, draft-template, ready, legacy, and protected configurations.
- [x] 3.2 Add presentation and deterministic Avalonia headless coverage for status, guidance, accessibility, and unchanged controls.
- [x] 3.3 Run focused tests and reconcile failures against every acceptance scenario.
- [x] 3.4 Run `openspec validate design-offering-readiness-guidance --strict`.
- [ ] 3.5 Run `dotnet test .\FusionCanvas.sln` after restoring dependencies if required and record limitations separately.
