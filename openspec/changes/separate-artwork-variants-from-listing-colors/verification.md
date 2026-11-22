# Verification Plan

This file is the scenario-level verification map for `separate-artwork-variants-from-listing-colors`. The methods below are planned acceptance evidence; results, command output, test identifiers, and artifacts must be filled in during implementation and completion review. No acceptance criterion is considered passed from an aggregate test result alone.

## Acceptance Scenario Map

| Capability / requirement | Acceptance scenario | Planned verification |
| --- | --- | --- |
| artwork-variant-management / Design artwork variants are independent of product Colors | Design has one artwork variant | Domain + DesignStageService tests: creating Design context yields a default variant and does not require Listing Colors. |
| artwork-variant-management / same | Creator adds a manually prepared alternative | Application test: importing a second Asset creates an independent variant; existing row/area assignments remain unchanged. |
| artwork-variant-management / same | Creator sets contrast intent | Domain/Application round-trip test for both intents and unset; confirm no Color selection is created. |
| artwork-variant-management / same | Creator assigns different artwork to multiple Design Areas | Application test: assignments remain scoped by variant and area across save/reload. |
| artwork-variant-management / same | Creator changes the Design Offering | View-model/Application test: explicit change guidance appears and prior artwork stays; no unverified compatibility is shown. |
| artwork-variant-management / same | Creator removes an artwork variant used by a Listing Color | Domain/Application test: removal is blocked or requires atomic reassignment; cancel preserves assignment. |
| artwork-variant-management / Existing color-partitioned Design artwork migrates without losing assignments | Existing color mapping resolves uniquely | SQLite migration integration test: stable Option Value and old row map to Listing; IDs and Design Area assignments survive. |
| artwork-variant-management / same | Existing color mapping is unmatched or ambiguous | SQLite migration + Listing Application test: unresolved label and mapping survive; another valid Color remains usable. |
| artwork-variant-management / same | Workspace migration is repeated | Migration integration test applies twice and compares identities, counts, mappings, and Printify link/snapshot data. |
| listing-variant-selection / Favorite Colors are scoped to a Blueprint Offering | Creator marks a commonly used Color as a favorite | Domain/Application + Store Editor headless test: favorite persists by Option Value ID and appears only on same Offering. |
| listing-variant-selection / same | Creator removes a Color from favorites | Application test: favorite disappears while existing Item selection remains. |
| listing-variant-selection / Listing progressively discloses Offering Colors | Creator uses a favorite Color | Listing service/view-model test: selection persists and derives compatible Sizes; reload retains selection. |
| listing-variant-selection / same | Creator inspects an uncommon Color | Application + headless view test: More Options lists all configured Colors; preview activation changes preview only. |
| listing-variant-selection / same | Creator searches the full Color collection | Headless view/view-model test: case-insensitive filtering preserves draft check state and clearing the query does not mutate Listing selection. |
| listing-variant-selection / same | Creator selects several uncommon Colors | Application test: confirm atomically adds all checked Colors and defaults each to first artwork variant. |
| listing-variant-selection / same | Color has no approximate value | Headless view test: neutral swatch and name appear without fabricated hex. |
| listing-variant-selection / same | Color has no active provider-enabled Variants | Listing view-model/headless test confirms the Color is unavailable with a reason and cannot be added. |
| listing-variant-selection / same | Creator cancels More Options | Headless view test: Escape/cancel discards dialog draft and returns focus to the invoking action. |
| listing-variant-selection / Listing Color selection includes all configured Sizes and preserves stock state | Color has several configured Sizes | Domain/Application projection test: all active provider-enabled variants with that Color, across Sizes, are derived. |
| listing-variant-selection / same | Some configured Sizes are out of stock | Application/headless view test: available and OOS variants both remain selected and have distinct text states. |
| listing-variant-selection / same | Provider catalog is unavailable | Application test: stale local variants remain selected with unverified status after refresh failure. |
| listing-variant-selection / Each selected Listing Color has an explicit artwork variant | First artwork variant is assigned by default | Application persistence test: newly selected Color maps to first variant and mapping reloads. |
| listing-variant-selection / same | Creator assigns an alternate artwork variant | Application test: changing one Color mapping leaves other mappings and Design data unchanged. |
| listing-variant-selection / same | Assigned artwork variant has no usable artwork | Listing readiness test: affected Color is identified and is not marked ready. |
| listing-variant-selection / same | Creator removes a selected Color | Application test: only Item selection/assignment is removed; global favorite and Design variant remain. |
| product-supplier-setup / Imported catalog records retain provider identity and local terminology | Import maps one provider offering | Existing catalog import contract test confirms Store ownership and stable provider identities. |
| product-supplier-setup / same | Imported variant compatibility is preserved | Catalog import test confirms placeholder compatibility after repeated import. |
| product-supplier-setup / same | Out-of-stock catalog variants are returned | Printify client contract test asserts `show-out-of-stock=1`; import test asserts OOS does not set `IsArchived`. |
| product-supplier-setup / same | Provider Color has an approximate swatch value | Printify parser/import test confirms name and approximate hex metadata round-trip with stable Option Value ID. |
| product-supplier-setup / same | Provider does not return an out-of-stock Variant | Import test confirms omitted provider data does not delete/archive local Variant or erase last-known availability. |
| printify-listing-lifecycle / The Printify tool consumes authoritative local product inputs | Listing Colors are projected into Printify variants | ListingProjectionBuilder + request serializer tests: selected Colors/sizes including OOS are present; unselected Colors are absent. |
| printify-listing-lifecycle / same | A selected Color is not supported by the Provider | Application readiness test confirms actionable error and no remote call. |
| printify-listing-lifecycle / same | Provider-Network Offering is selected | Application readiness test confirms create/update is unavailable without concrete Provider. |
| printify-listing-lifecycle / Printify publication reminds the creator to verify out-of-stock availability behavior | Creator acknowledges the availability reminder | View-model/lifecycle test verifies acknowledgement proceeds but persists no verified remote setting. |
| printify-listing-lifecycle / same | Creator cancels at the availability reminder | Lifecycle test verifies publish client receives no call and mapping/publication state is unchanged. |
| printify-listing-lifecycle / same | Creator publishes when the API cannot verify the setting | Lifecycle test verifies reminder says manual/unverified and does not expose or invoke automatic Order Routing. |
| printify-listing-import / Downloaded Printify variant setup populates Listing and artwork variants | Creator downloads setup for a linked product | Import service test confirms selected Color IDs and all enabled compatible Sizes are placed in Listing and image signatures become color-independent variants with per-Color assignments. |
| printify-listing-import / same | Linked product has an out-of-stock enabled Variant | Import contract test confirms enabled OOS Variant remains active and selected when present on the linked product. |
| printify-listing-import / same | Item already has local variant or artwork setup | Import service test confirms the existing local Design/Listing state and remote mapping remain unchanged with actionable guidance. |
| printify-listing-import / same | Printify artwork import is partial or ambiguous | Import service test confirms successful assignments survive, unsupported slots remain empty, and Listing Colors receive readiness guidance. |
| listing-mockup-generation / Listing-stage mockup tool presents applicable local templates | Listing has applicable templates and assigned artwork | Application projection test confirms the tool resolves each Listing Color to its assigned variant. |
| listing-mockup-generation / same | Listing cannot yet generate a mockup | Application/headless view test confirms the affected Color's blocked reason and unrelated ready Colors remain enabled. |
| listing-mockup-generation / same | Listing is protected | Existing protected-state tests plus headless view check confirm read-only behavior. |
| listing-mockup-generation / Applying a template composes one output per applicable Listing Color and artwork variant | Creator applies a complete template | MockupGenerationService test confirms one output per selected Color uses its assigned artwork, template revision, and metadata. |
| listing-mockup-generation / same | A Color template is missing | Mockup service test confirms precise diagnostic, no fabricated output, ready Color success retained, and setup path remains available. |
| listing-mockup-generation / same | Mapping or source input is invalid | Mockup service test confirms per-output failure, existing outputs unchanged, and source/template assets retained. |

## UI Coverage Decision

This user-facing module warrants one Windows Appium scenario pack for the rendered favorite/More Options path and durable Listing selection. Pack: `listing-color-flexibility` in `tests/FusionCanvas.UITests`, one ordered journey over one fresh disposable workspace/database. Shared start state: imported linked product with two favorite Colors, uncommon Colors, and provider-configured variants. User actions: download linked product setup; open Listing; preview an uncommon Color without checking it; check two Colors and confirm; read the workspace again and verify stable selected Color identities. Visible outcomes include the favorite-first collection, separate preview/check controls, swatches, and confirmation. Durable outcome is the set of Listing Color assignments while the imported artwork and remote mapping remain intact. Cadence/filter: supplemental Windows UI lane selected by `ScenarioPack=listing-color-flexibility`; run for this feature's completion and later UI-affecting regressions, never as the deterministic default baseline. The deterministic Application and headless suites cover partial mockup readiness and the publish reminder, avoiding remote lifecycle operations in the desktop journey. Lower-layer tests cover migration ambiguity, Color selection invariants, provider errors, and mockup partial failures.

## Completion Evidence

Fill this table during implementation. Use exact test names or command output and link artifacts where applicable.

| Gate | Result | Evidence |
| --- | --- | --- |
| Artwork variants and contrast intent | Pass | `ArtworkVariantListingColorTests`; `DesignStageServiceTests`; `DesignStageToolHeadlessTests`. |
| Listing favorite, Color selection, artwork mapping, all Sizes, and availability | Pass | `ListingColorSelectionServiceTests`; `ListingLifecycleTests`; `ListingProjectionBuilder` and Printify serializer tests. |
| More Options preview/select/cancel and search focus | Pass | `FusionCanvas.App.Tests.StageToolViewModelsTests.ListingColorOptionsWindow_PreviewDoesNotSelectAndEscapeDiscardsDraft` (Avalonia headless). |
| More Options lists all active configured Colors, including favorites, and applies mixed favorite/nonfavorite checks | Pass | `StageToolViewModelsTests.ListingColorOptionsWindow_PreviewDoesNotSelectAndEscapeDiscardsDraft` asserts the rendered dialog's color rows contain favorite and nonfavorite Colors, cancel discards both drafts, and confirmation persists both selections; included in the full App suite below. |
| Migration, idempotence, unresolved resolution, and rollback | Pass | `ArtworkVariantMigrationPersistenceTests` migration, round-trip, and `Migration_RollsBackListingSelectionsWhenBackfillFails`; remote mapping equality asserted after migration. |
| Printify catalog query, swatch, and availability behavior | Pass | `PrintifyCatalogClientTests`; Printify catalog import Application tests; Integration suite. |
| Mockup assignment and partial success | Pass | `MockupGenerationApplicabilityRegressionTests.Apply_generates_ready_color_with_its_assigned_art_and_keeps_unready_color_selected`. |
| Publish reminder, cancellation, republish, and non-persistence | Pass | `StageToolViewModelsTests.PrintifyTool_HeadlessShopifyJourneyPublishesUnpublishesAndReopensDeletionGate`. |
| Avalonia headless view coverage | Pass | `FusionCanvas.App.Tests`: 1,053 passed in final solution run; focused dialog case included. |
| Appium `listing-color-flexibility` scenario pack | Implemented; execution unavailable | Test source compiled. `127.0.0.1:4723` was not accepting connections, so the Windows desktop journey was not run. |
| Workspace migration and Printify contract review | Pass | Schema order is Artwork Styles v24, Listing Color assignments v25, provider availability v26. `ArtworkVariantMigrationPersistenceTests` covers failed v25 backfill rollback and retry; query contract requires `show-out-of-stock=1`; provider availability remains separate from local archival. |
| Changed-scope OpenSpec drift review | Pass | Reviewed Domain/Application/Integration/App changes against all six delta specs; no remote mutation is performed by local migration. |
| Strict OpenSpec validation | Pass | `openspec validate separate-artwork-variants-from-listing-colors --strict`. |
| `dotnet test .\FusionCanvas.sln -m:1 --no-restore` after issue #985 fix | Pass | Domain 301, Application 740, Integration 473, App 1,055, UI Description 29; 2,598 passed, 0 failed. NU1900 was emitted because `api.nuget.org` was unreachable. |
