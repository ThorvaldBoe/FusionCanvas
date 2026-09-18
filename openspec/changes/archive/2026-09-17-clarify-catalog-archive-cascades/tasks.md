## 1. Application contracts and dependency inspection

- [x] 1.1 Add catalog archive preview/result contracts that represent grouped catalog-owned descendants and named external blockers.
- [x] 1.2 Add preview and confirmed Offering archive-cascade operations to the application catalog interface and service.
- [x] 1.3 Replace generic single-record dependency text with concrete type/name resolution for Variant, Option Value, Placeholder, and Offering blockers.

## 2. Atomic persistence behavior

- [x] 2.1 Implement one snapshot mutation that archives the Offering and all current catalog-owned descendant collections while preserving stable identities and relationships.
- [x] 2.2 Revalidate Store, Offering, descendant identities, and external blockers during confirmation; save only a fully valid snapshot.
- [x] 2.3 Add application/integration tests for successful cascade, cancellation/no-op preview, external blockers, unrelated-record preservation, and identity preservation.

## 3. Store Editor workflow

- [x] 3.1 Add Offering archive preview/confirmation state and commands to `CatalogSetupViewModel`, including busy, cancel, blocked, and post-success selection behavior.
- [x] 3.2 Add the clearly labeled Archive Blueprint Offering action to the Offering Basics surface and bind it to the focused confirmation state.
- [x] 3.3 Render grouped dependent counts/names, reversible wording, concrete blocked guidance, and a keyboard-accessible cancel/confirm flow.
- [x] 3.4 Refresh Store Management and Catalog Setup state after cascade success so archived Offering records and counts remain coherent.

## 4. Verification and completion

- [x] 4.1 Add Avalonia headless coverage for Offering archive action ownership, preview contents, cancellation, external blocker guidance, and successful UI refresh.
- [x] 4.2 Add regression coverage proving Variant archive buttons still invoke the existing command and show named Placeholder blockers when order is wrong.
- [x] 4.3 Run focused Application/App tests and update change verification evidence for every acceptance scenario.
- [ ] 4.4 Run `openspec validate --strict` and `dotnet test .\\FusionCanvas.sln`; resolve failures without expanding scope.
