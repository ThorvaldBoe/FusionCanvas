## 1. Mockup Domain Entity

- [ ] 1.1 Add a `Mockup` domain entity owned by exactly one listing, with stable identity, created/modified timestamps, Listing-stage context, optional showcased-design reference, source flag (manual, generated), lifecycle marker (active, superseded, rejected), and documented mockup metadata.
- [ ] 1.2 Add a regeneration-metadata block for generated mockups (source design, mockup product, template, color variant, placement parameters, generation timestamp).
- [ ] 1.3 Add domain tests for item-binding, multi-mockup preservation, showcased-design cross-listing rejection, source-flag values, lifecycle markers, and zero-mockup listings.

## 2. Mockup Application Layer

- [ ] 2.1 Define `IMockupService` requests and results for create, edit, asset link, supersede, reject, and remove, with recoverable error reporting.
- [ ] 2.2 Implement manual mockup creation that attaches an existing asset as a mockup image through asset-management and records supplied metadata.
- [ ] 2.3 Implement mockup edit that preserves identity, listing, showcased-design reference, source flag, lifecycle marker, asset references, and unknown metadata.
- [ ] 2.4 Implement supersede and reject transitions that preserve records without deletion.
- [ ] 2.5 Add application tests for manual create, edit, asset link, supersede, reject, metadata preservation, and repository-failure atomicity.

## 3. Persistence and Migration

- [ ] 3.1 Add a forward-only schema migration that creates the mockup table, regeneration-metadata block, and Mockup asset context kind, and increments the snapshot version.
- [ ] 3.2 Migrate existing snapshots by assigning zero mockups without data backfill.
- [ ] 3.3 Add SQLite integration tests for create, edit, asset link, supersede, reject, metadata, complete reload, and migration of pre-existing snapshots.
- [ ] 3.4 Verify failed or cancelled mockup operations persist no partial mockups, asset links, lifecycle changes, or metadata changes.

## 4. Asset and Cross-Capability Integration

- [ ] 4.1 Add Mockup as a valid asset context kind and preserve referenced assets when a mockup is removed.
- [ ] 4.2 Preserve mockup records when a referenced asset is removed through asset-management.
- [ ] 4.3 Treat mockups as dependent creative records in the listing and design permanent-deletion guards and block connected record deletion with actionable guidance.
- [ ] 4.4 Provide a regeneration hook that FC-0212's Basic Listing Tool can call to regenerate a generated mockup from its regeneration-metadata block.
- [ ] 4.5 Add integration tests for asset context links, asset removal preservation, deletion blocking, and the regeneration hook contract.

## 5. Verification

- [ ] 5.1 Run domain, application, integration, app, and UI automation test suites and resolve core-domain-model, asset-management, design-records, and listing-management regressions.
- [ ] 5.2 Manually verify manual mockup attach, edit, supersede, reject, asset link and removal, metadata, persistence, migration, and deletion-guard blocking.
- [ ] 5.3 Run strict OpenSpec validation and confirm every mockup-records and modified core-domain-model and asset-management scenario is covered by implementation or automated tests.
