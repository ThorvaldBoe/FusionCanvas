# Verification

## Acceptance evidence

| Criterion | Evidence |
|---|---|
| Offering archive action is visible and opens a reversible dependency review | `StoreEditorHeadlessTests.OfferingManagement_DisclosesArchiveCascadeAndCanCancel` passed. |
| Offering cascade archives catalog-owned descendants atomically and preserves identity | `CatalogSetupServiceTests.PreviewsAndArchivesOfferingWithCatalogOwnedDependentsAtomically` passed. |
| External Item/listing blockers are named and prevent mutation | `CatalogSetupServiceTests.OfferingArchivePreviewNamesExternalItemBlockersWithoutMutating` passed. |
| Variant archive guidance names the blocking Placeholder | `StoreEditorHeadlessTests.VariantManagement_ArchiveButtonInvokesVariantArchiveCommand` passed. |
| Existing archive wording remains compatible for other dependent records | `CatalogSetupViewModelTests.ConfirmDesignAreaArchive_BlockedReferencedAreaDisplaysRecoverableGuidance` passed. |
| OpenSpec structure and requirements are valid | `openspec validate clarify-catalog-archive-cascades --type change --strict` passed. |

## Regression baseline

`dotnet test .\\FusionCanvas.sln -m:1 -v minimal` completed with unrelated existing failures in Printify import/client tests and one legacy dependency-message assertion, the latter fixed by preserving the word `referenced` in concrete guidance. Focused reruns for the changed Application and App tests passed: 14 application tests and 3 App tests.
