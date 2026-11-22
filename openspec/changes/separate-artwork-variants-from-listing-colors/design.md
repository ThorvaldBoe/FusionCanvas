## Context

The current model stores a Design working set as selected Color labels, color-partitioned `DesignVariantRow` records, and row-by-Design-Area asset assignments. Listing projection and mockup generation both derive Color from those Design tables. This couples final artwork to product selection: adding an uncommon Color requires a Design-stage change even though the artwork itself has not changed.

The domain already has stable Offering Color Option Value and sellable Variant identities, Printify availability fields, and Store-owned Mockup Templates. Printify catalog setup currently filters some out-of-stock Variants, and the import path can mark a Variant archived because it is unavailable. The new workflow makes Design variants color-independent, makes Listing the authority for Item Colors, and treats provider availability as status rather than local retirement.

The user-facing primary workflow is the frequent Listing path: keep favorite Colors immediately visible, use **More Options** for occasional Colors, preview the assigned artwork against a flat approximate swatch, and select multiple Colors without leaving Listing. Favorite management and missing-template setup are less frequent Offering-scoped setup tasks. The workspace remains compact; More Options and catalog/setup editing are focused surfaces. Keyboard selection, preview-versus-add distinction, dialog cancel behavior, focus return, partial mockup success, and recoverable provider failures are part of the interaction contract.

This package conflicts with the current `printify-listing-lifecycle` assumption that Design owns Colors. Its implementation and deterministic verification are complete; its only unchecked item is a supplemental supervised showcase-store smoke check. Reconcile the local projection contract in this package and do not perform an external Printify mutation as a prerequisite. The new Listing selection remains the sole local authority.

## Goals / Non-Goals

**Goals:**

- Make Design artwork variants independent of specific product Colors while preserving per-variant Design Area assignments and optional light/dark contrast intent.
- Keep Blueprint Offering and fixed Provider in Design for Design Area guidance; do not let Listing silently change the offering.
- Move per-Item Color selection, favorite-first browsing, all-size inclusion, stock status, and per-Color artwork assignment to Listing.
- Preserve approximate Printify Color values for flat swatches and retrieve provider Variants even when currently out of stock.
- Keep mockup generation useful when one selected Color lacks a template by allowing independently ready Colors to proceed.
- Remind and require a manual acknowledgement before each Printify Publish or republish without claiming API verification.
- Migrate existing workspaces without losing artwork assets, Color-to-artwork mappings, or remote product mappings.

**Non-Goals:**

- Creating or editing artwork pixels in FusionCanvas for manual alternatives; creators may download, edit externally, and import a new artwork variant.
- Simulating heather/fabric texture or rendering realistic apparel photos in the Color picker.
- Switching providers inside Listing or automatically adapting artwork to a new Provider's Design Areas.
- Supporting arbitrary per-Size selection in this module; selecting a Color includes all active configured Size Variants.
- Automatic Printify Order Routing, setting Printify's product availability policy by API, or changing existing remote products during local migration.
- Changing provider-specific pricing, publishing lifecycle conflict handling, or unrelated catalog/template management.

## Decisions

### Design artwork representation

Reuse the existing row-and-slot structure as an artwork-variant concept rather than introducing a parallel image system. Each variant remains a stable row-like aggregate with ordering and Design Area-to-Asset assignments; remove Color membership from that aggregate. Keep the first variant as the default used for newly selected Listing Colors. Store contrast intent as optional variant metadata or a normalized enum field, not as a Color relation. Generation offers only the two agreed intents: light artwork for dark products and dark artwork for light products. Manual imports may leave intent unset.

The Offering and Provider remain Design context because they define available print areas and artwork guidance. A Provider change requires an explicit context change and review of Design Areas; the app preserves source artwork for manual adaptation and never labels it as compatible without validation.

### Listing selection and favorites

Persist favorite state on the stable Color Option Value or an Offering-scoped favorite relation. The implementation should choose the least disruptive existing persistence shape; favorite identity is Offering + Option Value, not display label. In Listing, show the favorite Color group inline and put the full configured collection in a focused **More Options** surface. Inside More Options, row activation changes only the preview; a separate checkbox changes the draft selection. Confirm applies all checked additions/removals atomically. A selected Color includes all active, provider-enabled sellable Variants containing it. Size remains implicit and all configured Sizes are included. Current availability is displayed as available/out-of-stock/last-known or unverified, not used as an eligibility filter.

Represent Color-to-artwork assignment at Item + Color Option Value identity. New selections use the first artwork variant; changing one assignment affects only that Color. Color names and swatch values remain separate: preserve the provider's approximate hexadecimal color value in existing Option Value metadata where available. Preview uses a flat swatch and an artwork thumbnail composited or positioned for inspection; it does not approximate fabric texture.

### Catalog and Printify behavior

For Printify provider catalog variant retrieval, request out-of-stock variants using the documented `show-out-of-stock=1` query parameter. Map `is_available` to refreshable provider-status metadata and do not use it as `OfferingVariant.IsArchived`; only provider-disabled or explicit local lifecycle rules can make a Variant ineligible. Preserve the last known stock status when refresh fails or a Variant is omitted. Existing shop-product import remains distinct from global provider-catalog discovery.

The Listing projection reads selected Colors and Design Area assignments from their new authoritative locations. A remote update/create includes the selected Colors' active provider-enabled Variants, including out-of-stock ones, only after the user initiates the existing lifecycle operation. Local migration never makes a remote request. Before every Publish or republish, require a clear manual acknowledgement that the user checked the Printify product setting; explicitly state FusionCanvas cannot inspect or set it. Do not conflate that reminder with automatic Order Routing.

### Missing mockup templates and partial work

Selecting an uncommon Color never requires a pre-existing template. Listing presents the missing-template prompt and setup action for that Color. Mockup generation processes each selected Color independently: missing or invalid template inputs create a per-Color diagnostic, while ready Colors still produce and persist their outputs. Existing generated outputs remain intact on partial failure. If setup is opened from Listing, preserve pending Color choices and return to the same Item and focus context where practical.

### Workspace migration and compatibility

Add a transactional, idempotent SQLite schema migration. Convert each legacy color-partitioned Design row into one color-independent artwork variant, preserving its row ID when possible, order, assigned Design Area assets, and metadata. Convert each legacy selected Color and row membership into an Item Listing Color assignment. Resolve legacy color labels against exactly one active Color Option Value in the selected Offering using case-insensitive exact value comparison. Preserve unmatched or ambiguous labels as explicit unresolved legacy selections tied to the old row's artwork assignment; require user resolution without blocking valid Colors. Never remove original artwork Assets or mutate Printify products. If a workspace has a legacy Color selection but no row mapping, retain the Color with the first/default artwork variant and surface any absent artwork as Listing readiness.

Persist favorite state and Item Color-to-artwork mapping through the repository snapshot and database migration. Existing Printify sync snapshots remain unchanged until a user explicitly performs an update; drift review must treat Listing selections as the new desired local projection after migration.

### UX and verification decisions

- Keep the regular favorite selection and current selected Colors visible in the Listing workspace; More Options uses a focused modal/dialog with search if needed to reach a long catalog, a preview area, and independent include checkboxes.
- Preserve selection draft on recoverable preview failures; cancel discards dialog-only checks; confirmation applies once; return focus to the invoking More Options control.
- Keep provider stock state textual and accessible in the dialog; swatch color is supplemental and never the only Color identity.
- Add deterministic Domain/Application/Integration/Persistence tests and Avalonia headless tests for binding, focus, check/preview separation, cancel/confirm, and missing-template state.
- Real-desktop Appium coverage is warranted for one journey in `tests/FusionCanvas.UITests`, scenario pack `listing-color-flexibility`: use a disposable workspace and mocked catalog/import adapters; load linked product setup, preview an uncommon Color without selecting it, check multiple Colors, confirm, and verify the stable Listing assignments after reading the workspace again. The deterministic Application and headless suites cover per-Color mockup partial success and the Printify publish acknowledgement, keeping the desktop journey free of remote lifecycle operations. No live API or normal workspace is used. Run this supplemental scenario pack on the Windows UI lane when available; it does not replace the deterministic `dotnet test .\FusionCanvas.sln` baseline.

## Risks / Trade-offs

- **Legacy Color labels do not map uniquely to stable Option Value IDs** → preserve the text and old artwork relation as unresolved data, report it in Listing, and let the creator resolve it without deleting the underlying asset.
- **Provider stock and local archival become conflated** → persist `is_available` separately from `IsArchived`, retain provider-enabled status, and test available → out-of-stock → available refresh cycles.
- **New Size Variants appear after a Color was selected** → derive Size inclusion from the current active Offering Variants; updates remain explicit remote operations, so newly configured Sizes do not silently mutate Printify.
- **One missing template could make all mockups appear blocked** → evaluate and persist outputs per selected Color and retain partial success with precise diagnostics.
- **Users may mistake the manual Publish acknowledgement for API verification** → phrase it as a reminder, require an explicit checkbox/confirmation, and retain an unverified status after acknowledgement.
- **Provider changes can invalidate Design Area assumptions** → preserve Design Area guidance tied to the selected Offering and require explicit context review before treating artwork as compatible with another Provider.
- **Scope touches multiple existing stage tools and persistence records** → sequence migration and application model changes before UI projection; keep lifecycle remote-mutation policy unchanged except for input ownership and the reminder.

## Migration Plan

1. Bump the workspace schema and apply favorite/swatch and Listing Color assignment schema changes transactionally.
2. Backfill legacy Design rows and Color memberships into artwork variants and Listing assignments; validate row counts, referential integrity, and repeat-run idempotency before committing the version.
3. Load/save both the migrated shape and unresolved legacy Color labels through `WorkspaceSnapshot` and `SqliteWorkspaceRepository`.
4. Keep Printify mappings/snapshots and remote products untouched. Require explicit user lifecycle update after the local migrated desired state is shown.
5. Rollback is application-version rollback, not reverse migration: keep old columns/tables readable or retained during the transition, and ensure older supported app versions fail with the existing newer-schema guard rather than corrupting the new state.

## Open Questions

None remain for product behavior in this module. During implementation, confirm Printify's exact catalog query and color metadata shape against the current adapter contract/documentation; this is a provider contract verification, not a product decision.

## Implementation Plan

### Affected layers and responsibilities

- **Domain (`FusionCanvas.Domain`):** Refactor `Products/DesignVariantRow.cs` into color-independent artwork-variant semantics (rename only if it improves clarity across all usages); remove `DesignVariantRowColor` as an active relationship; add a Listing Color selection/assignment entity keyed by Item and stable Offering Color Option Value ID, plus an unresolved legacy label state; add optional contrast intent. Keep `Catalog/OfferingOptionValue.cs` as the stable Color value and preserve approximate hex metadata there. Keep `Catalog/OfferingVariant.cs` active independently of availability.
- **Application (`FusionCanvas.Application`):** Update `DesignFiles/DesignStageService.cs`, `DesignStagePolicy`, and artwork generation/assignment services so Design can manage variants and Design Area assignments without requiring or mutating Colors. Add a Listing Color selection use case that previews without mutation, confirms multiple selections atomically, defaults to first artwork variant, and derives all compatible active Size Variants. Update `Listings/ListingProjectionRequest.cs` and `ListingProjectionBuilder.cs` to consume Listing assignments and per-Color artwork mapping. Update `Mockups/MockupGenerationService.cs` to resolve the selected Color's artwork variant and produce independent partial results. Add Publish reminder acknowledgement to `Listings/ListingLifecycleService.cs` / request boundary without claiming remote verification.
- **Integration (`FusionCanvas.Integration`):** Update `Stores/Printify/PrintifyCatalogClient.cs` to request `show-out-of-stock=1` on provider variants, parse the Color swatch when supplied, and retain availability separately. Update `Stores/Printify/PrintifyCatalogImportService.cs` so `is_available=false` no longer archives a provider-enabled Variant. Keep shop-product import distinct, but update its local setup conversion to create Listing Color assignments and color-independent artwork variants. Extend repository persistence in `Persistence/SqliteWorkspaceRepository.cs`, `WorkspaceSnapshot` serialization, and schema migration to persist favorite state, color-independent artwork variants, Listing Color assignments, swatch data, and unresolved legacy labels atomically.
- **App (`FusionCanvas.App`):** Remove product Color selection from the Design stage view and replace color-partition wording with artwork variants and the two contrast-intent choices for generated artwork. Keep Offering/provider selection and Design Area guidance. Add favorites to the Store Editor Offering Color management surface. Add compact favorite Color choices and a focused More Options picker in Listing, separate row preview from selection checkbox, show flat swatches and availability, support per-Color artwork assignment, and show missing-template prompt/partial-ready state. Add manual Printify availability acknowledgement before each Publish/republish.

### Likely code and test locations

- Design domain/application: `src/FusionCanvas.Domain/Products/DesignVariantRow.cs`, `DesignVariantRowColor.cs`, `DesignSelectedColor.cs`, `DesignSlotAssignment.cs`, `src/FusionCanvas.Application/DesignFiles/DesignStageService.cs`, `DesignStagePolicy.cs`, `ArtworkGenerationService.cs`.
- Listing: `src/FusionCanvas.Application/Listings/ListingProjectionRequest.cs`, `ListingProjectionBuilder.cs`, `ListingLifecycleService.cs`, listing selection request/results/services, `src/FusionCanvas.App/StageTools/PrintifyListingStageToolViewModel.cs`, Listing tool XAML and `MainWindow.axaml` integration.
- Mockups: `src/FusionCanvas.Application/Mockups/MockupGenerationService.cs`, `MockupGenerationState.cs`, result/diagnostic types, Listing mockup view model and its headless tests.
- Catalog/Printify: `src/FusionCanvas.Integration/Stores/Printify/PrintifyCatalogClient.cs`, `PrintifyCatalogImportService.cs`, `src/FusionCanvas.Application/Stores/Printify/PrintifyListingImportService.cs`, relevant `PrintifyCatalogClientTests.cs`, import service tests, catalog DTOs in `src/FusionCanvas.Application/Stores/Printify/`.
- Persistence: `src/FusionCanvas.Domain/Workspace/WorkspaceSnapshot.cs`, `src/FusionCanvas.Integration/Persistence/SqliteWorkspaceRepository.cs`, schema version/migration tests under `tests/FusionCanvas.Integration.Tests/Persistence/`.
- Store UI: `src/FusionCanvas.App/Stores/VariantChoiceViewModel.cs`, Offering option-value UI/view models, `StoreEditorWindow.axaml` and code-behind. Keep favorite editing scoped to the selected Offering and stable Color Option Value.
- Tests: mirror affected layers under `tests/FusionCanvas.Domain.Tests`, `tests/FusionCanvas.Application.Tests`, `tests/FusionCanvas.Integration.Tests`, `tests/FusionCanvas.App.Tests`, plus the planned `tests/FusionCanvas.UITests` scenario pack.

### Sequence and algorithms

1. Add domain invariants for color-independent artwork variants, optional contrast intent, Item Color assignment, favorite scope, and unresolved legacy mapping. Add a stable Color Value identifier to selection; avoid using display names as new relationship keys.
2. Implement and test a pure migration planner: for each legacy Design row, preserve its ID/order/slot assignments; for each selected Color, exact-match active Color Option Values in the Item's selected Offering; convert the row membership to the new Item Color assignment; preserve unresolved labels when the match is absent or ambiguous. Apply all data changes and schema version update in one SQLite transaction.
3. Update snapshot/repository read/write before changing UI so reload and round-trip tests prove data durability. Preserve legacy tables/columns until migration and compatibility tests pass.
4. Change Design-stage application logic and UI to manage only artwork variants/areas. Ensure adding/removing an artwork variant does not infer Color selection and blocks deletion of variants referenced from Listing.
5. Implement Listing favorite selection and More Options projection. Separate preview highlight from draft include state; on confirm atomically persist selected Color IDs and default/retained artwork assignments. Derive selected sellable Variant IDs from active provider-enabled Variants whose Color Option Value is selected; include all Sizes and keep out-of-stock metadata visible. Update downloaded Printify variant setup to populate the same Listing/artwork model without overwriting existing local setup.
6. Refactor Listing projection and Printify product create/update to consume the new authority. Test that migrated/selected Colors are included and unselected Colors are excluded; explicit lifecycle calls remain the only remote mutations.
7. Update mockup generation to use per-Color artwork assignments. Resolve template readiness independently by Color, preserve successful outputs, and return missing-template diagnostics without rolling back unrelated outputs.
8. Add pre-publish acknowledgement before the existing confirmation/API call for Publish and republish. Cancel sends no request; acknowledgement never writes a verified Printify setting.
9. Add Avalonia headless coverage for Listing picker interaction and focus. Add the one isolated Appium scenario pack for the complete user journey with a mock provider client.

### Decisions not to reopen

- Colors belong to Listing; artwork variants belong to Design.
- Blueprint Offering and fixed Provider remain selected in Design for Design Area identity. Listing cannot switch provider.
- Favorite scope is one Blueprint Offering; favorite is a shortcut, not an Item selection.
- **More Options** selection is per Item. Row preview and inclusion are separate interactions.
- Selecting a Color includes all active provider-enabled configured Sizes, including out-of-stock Variants.
- First artwork variant is assigned to newly selected Colors; other assignments are explicit per Color.
- Flat approximate swatches are sufficient; no fabric/heather simulation is included.
- Missing templates do not prevent selecting a Color or generating ready Colors.
- Every Printify Publish/republish gets a manual check reminder/acknowledgement. FusionCanvas does not set or verify the remote policy and does not use automatic Order Routing.
- Local migration preserves old mappings and does not mutate remote products.

### Acceptance-to-verification mapping

See `verification.md` for a scenario-by-scenario test plan. Each new and modified requirement is mapped to deterministic Domain, Application, Integration, persistence, or headless UI verification. The one Appium journey covers the high-information full Listing interaction; lower-level tests cover provider errors, migration edge cases, and partial mockup failures.
