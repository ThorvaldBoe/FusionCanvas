# Basic Product Creation Workflow — Implementation Notes

## 1.2 Repository-wide `Listing`/`listing` terminology inventory

Classification key:
- **U** = universal Item terminology (rename to Item)
- **F** = final Listing-stage terminology (keep `Listing`)
- **M** = marketplace-future terminology (keep)
- **H** = historical v4 migration SQL / archived-change / historical doc context (keep)

### Domain (`src/FusionCanvas.Domain`)

| Symbol / location | Classification | Notes |
| --- | --- | --- |
| `WorkspaceEntities.cs` `record Listing` (line 85) | U | Rename to `Item`; allow empty `Name` |
| `WorkspaceEntities.cs` `Prompt.ListingId` (line 138) | U | Rename to `ItemId` |
| `WorkspaceRelationships.cs` `enum ListingStatus` | U | Rename to `ItemStatus` |
| `WorkspaceRelationships.cs` `ListingStatuses` static | U | Rename to `ItemStatuses` |
| `WorkspaceRelationships.cs` `WorkspaceEntityKind.Listing = 3` | U | Rename symbol to `Item`; **keep numeric value 3** |
| `WorkspaceRelationships.cs` `record ListingTag(Guid ListingId, ...)` | U | Rename to `ItemTag(Guid ItemId, ...)` |
| `WorkspaceSnapshot.cs` `Listings` / `ListingTags` members | U | Rename to `Items` / `ItemTags` |
| `ListingHierarchy.cs` (class + helpers) | U | Rename file to `ItemHierarchy.cs`; rename methods/params |
| `NavigationTree.cs` `MoveListing`/`BuildListingNode`/`listingsBy*`/`Listing` params | U | Rename to Item terminology |
| `NavigationTree.cs` `WorkspaceEntityKind.Listing` references | U | Become `WorkspaceEntityKind.Item` |
| `WorkflowStage.cs` `enum WorkflowStage.Listing = 3` | **F** | Keep — final stage name |
| `WorkflowStage.cs` display "Listing" | **F** | Keep |

### Application (`src/FusionCanvas.Application`)

Universal contracts to rename to Item terminology (classification **U**):
`ListingManagementService`/requests/results, `ListingInspectorService`, `ListingMetadataCodec`, `ListingContext`, `ListingSummary`, `ListingId`, `ListingContextReference`/`AssetContextReference` for Listing links, Tag-link contracts, navigation/tree projections, Store/Niche/Group/Workspace universal `Listings` members, Asset link helpers, Prompt references, Tool Context (`SelectedListing`), Stage Tool host universal references.

Keep (**F**): the `WorkflowStage.Listing` value and final-stage display text in tool descriptors; the basic Listing tool's stage identity.

### Integration (`src/FusionCanvas.Integration`)

- `SqliteWorkspaceRepository` physical schema: `listings`, `listing_tags`, `prompts.listing_id` (**U** → rename to `items`, `item_tags`, `prompts.item_id` via v5 migration).
- `asset_links.entity_kind = 3` (**H**-compatible): keep numeric value 3; only the enum symbol changes.
- Migration SQL string literals referencing `listings`/`listing_tags` (**H** within the v4→v5 migration routine).

### App (`src/FusionCanvas.App`)

Universal Listing types/bindings/commands/automation labels/tests (**U**): `ListingInspectorViewModel`, `DocumentContext`/`DocumentWindowViewModel` universal references, `WorkspaceTreeViewModel`, `WorkflowStageNavigatorViewModel`, `MainWindowViewModel`, Tool Context labels, Asset view model `ContextKindLabel = "Listing"` for universal records. Keep `WorkflowStage.Listing` and final-stage tool naming (**F**).

### Tests (`tests/`)

Rename affected test files/types along with production contracts (**U**): `ListingHierarchyTests`, `ListingInspectorPersistenceTests`, `ListingManagementPersistenceTests`, `AssetManagementServiceTests`/`AssetsViewModelTests` Listing helpers, `GroupManagementServiceTests` Listing refs. Keep references that exercise the final `WorkflowStage.Listing` stage (**F**), the v4 migration fixtures (**H**), and archived-change/historical contexts (**H**).

### Docs

- `docs/ui-guidelines.md`: updated (task 1.3) with the Item explicit-Save exception.
- Canonical docs (`architecture.md`, `data-model.md`, `design-pipeline.md`, UI/UX, product, roadmap): update only where current Listing terminology/behavior is made stale by this module (task 8.1); do not import historical `docs/LifeOS/` scope (**H**).
