## 1. Provider normalization

- [ ] 1.1 Add a focused application-layer provider-normalization component with Store-scoped trimmed, case-insensitive name matching.
- [ ] 1.2 Implement deterministic survivor selection, preserving active/oldest/local identity rules.
- [ ] 1.3 Implement Printify external-ID alias extraction and merge while preserving unknown provider metadata.
- [ ] 1.4 Reassign all Store-owned `BlueprintOffering.PrintProviderId` references and archive superseded provider records atomically in the working snapshot.

## 2. Catalog and import integration

- [ ] 2.1 Apply provider normalization during `CatalogSetupService.LoadForStoreAsync` before catalog state and picker data are built.
- [ ] 2.2 Update manual Print Provider creation to reject an active same-name provider without changing the catalog.
- [ ] 2.3 Update `PrintifyCatalogImportService` to normalize before matching, reuse the canonical provider, preserve aliases, and keep imported offering identities idempotent.
- [ ] 2.4 Confirm compatibility synchronization and Store scoping do not recreate or cross-link superseded providers.

## 3. Automated coverage

- [ ] 3.1 Add application tests for stale duplicate repair, deterministic survivor selection, offering-reference reassignment, archival, alias preservation, and Store isolation.
- [ ] 3.2 Add Printify import tests for same-name providers with different external IDs and repeated imports using each ID.
- [ ] 3.3 Add manual-provider creation tests proving active same-name duplicates are rejected and failed mutations preserve the snapshot.
- [ ] 3.4 Add App ViewModel or Avalonia headless coverage proving the fixed-provider picker exposes one entry and preserves the selected offering/provider after normalization.
- [ ] 3.5 Add integration persistence coverage proving canonical providers, archived duplicates, aliases, and reassigned offerings survive save/reload.

## 4. Acceptance and delivery verification

- [ ] 4.1 Review the implementation against every scenario in the `product-supplier-setup` and `printify-catalog-import` delta specs; correct artifacts if implementation constraints reveal a genuine requirement mismatch.
- [ ] 4.2 Run focused catalog, import, picker, and persistence tests and record criterion-level evidence in the change verification record.
- [ ] 4.3 Run `openspec validate --changes` and resolve all validation failures.
- [ ] 4.4 Run `dotnet test .\\FusionCanvas.sln` and record the complete deterministic baseline result.
