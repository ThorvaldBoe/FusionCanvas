## 1. Specification and service contract

- [x] 1.1 Add the Blueprint archive-with-dependents request/contract and implement ownership validation plus atomic cascade in `CatalogSetupService`.
- [x] 1.2 Preserve ordinary archive dependency behavior and add application tests for all dependent catalog record types and external relationship preservation.

## 2. Blueprint detail warning flow

- [x] 2.1 Add pending Blueprint archive state, warning copy, commands/events, and mutation handling to `StoreManagementViewModel`.
- [x] 2.2 Add the prominent confirmation UI to the Blueprint detail Basic section with accessible cancel/confirm actions and no mutation on dismissal.
- [x] 2.3 Add focused view-model/headless UI tests for confirmation visibility, cancellation, and successful command routing.

## 3. Verification

- [x] 3.1 Run focused catalog and app tests; correct failures and record criterion-level evidence in `verification.md`.
- [x] 3.2 Run `openspec validate` and `dotnet test .\FusionCanvas.sln`; record results and limitations in `verification.md`.
