## Context

The workspace database persists an aggregate `WorkspaceSnapshot`, with Store-scoped records held in SQLite and reconstructed together. Store Management already owns a dedicated draft-safe editor with tabs for basic info, niches, and tags. The Design tool currently owns design-file operations only. This module adds catalog administration and design-target selection without bringing Printify transport or DTOs into the inner layers.

## Goals / Non-Goals

**Goals:**

- Maintain a Store-scoped, local catalog of product blueprints, fixed-provider and Choice-network offerings, variants, and printable areas.
- Let editable Design-stage Items persist zero or more selected, Store-compatible printable areas.
- Preserve Printify-relevant facts necessary for future import: external IDs, position, decoration method, pixel dimensions, and variant applicability.
- Fit catalog administration into the existing Store Editor's focused, draft-safe interaction model.

**Non-Goals:**

- Printify API authentication, retrieval, synchronization, or publishing.
- Pricing, shipping, mockups, artwork positioning/resizing, DPI validation, and generic multi-platform provider plugins.
- Moving Store Management into Settings.

## Decisions

### Domain model is provider-neutral with explicit fulfillment kind

Add domain records under `Domain/Products`: `StoreProduct`, `FulfillmentOffering`, `ProductVariant`, `DesignArea`, and `ItemDesignAreaTarget`. All records carry local stable IDs; Product/Offering retain optional external IDs. `FulfillmentKind.FixedProvider` requires a provider name; `FulfillmentKind.PrintifyChoiceNetwork` forbids one. This represents Choice honestly while supporting a future adapter boundary. Printify DTOs remain Integration-only.

Alternatives: modeling Choice as a provider would falsely imply a fixed provider; embedding catalog fields in Item metadata would prevent relational validation and reliable future sync.

### Variants and areas belong to offerings

Variants model exact option combinations using ordered name/value pairs serialized at the persistence boundary. A DesignArea belongs to one offering and relates to zero or more offered variants. It stores a display name, Printify position, decoration method, positive pixel width and height, and selected variant IDs. Areas with no variant restriction apply to all offering variants. Color and size are not global records.

### Item targets are references, never copied dimensions

`ItemDesignAreaTarget` joins an Item to a DesignArea. Selection validates that Item and area share the same Store and that the Item is editable at Design. An empty selection is valid. Target references keep later catalog corrections visible and make removal safety enforceable.

### Persistence extends the snapshot and uses additive schema migration

Add catalog lists and Item targets to `WorkspaceSnapshot`, SQLite tables and FK constraints, full-snapshot save/load ordering, and a new schema version migration. Catalog rows are written before Item targets and removed in dependency order. The repository validates Store membership, parent relationships, duplicate IDs, positive dimensions, applicable variant ownership, and target ownership.

### Store Editor gets a Products & fulfillment tab

Extend `StoreManagementEditorTab` and its ViewModel with a fourth tab that uses three progressively disclosed levels: products list, offerings list for the selected product, and an offering editor containing variants and design areas. Records use in-memory drafts with explicit Save/Cancel and participate in existing discard prompting. Deletion is confirmed and blocked if any dependent catalog record or Item target exists. Archived Stores show the tab read-only.

### Design tool presents compact target guidance

Extend `DesignStageToolViewModel` or a focused collaborator with a target summary and selector. It loads only active areas from the Item's Store, exposes Choice-network warning text, allows multi-select only when Design is editable, and persists selection atomically. It never modifies design files.

## Risks / Trade-offs

- [Large Store Editor surface] → Reuse its existing tab/draft conventions and add headless coverage for selected tab, empty state, and focus.
- [Stale selected targets after catalog edits] → Block destructive removal while referenced; catalog updates preserve IDs.
- [Future API shape changes] → Preserve stable external IDs plus a small provider metadata JSON field at the Integration boundary; do not make the Domain a Printify DTO.
- [Snapshot changes affect many tests] → Keep compatibility constructors/default empty lists and update fixture builders centrally.

## Migration Plan

1. Increment the SQLite schema version and add catalog/target tables with Store and parent foreign keys.
2. Existing databases migrate with empty catalog and target tables; existing Items remain valid with zero targets.
3. New save/load paths round-trip old and new snapshots. A failed migration leaves the transaction rolled back; the existing newer-schema guard remains the rollback protection.

## Implementation Plan

1. Add Domain records/enums and snapshot members, including compatibility defaults. Add pure invariant tests in `tests/FusionCanvas.Domain.Tests/Products`.
2. Add Application ports, requests, summaries, and `ProductSupplierSetupService` for Store-filtered loading, draft commits, removal checks, and Item target selection. Test deterministic collaborators in `tests/FusionCanvas.Application.Tests/Products`.
3. Update `SqliteWorkspaceRepository` and schema version/migrations; ensure insert/load/delete order and validation; add isolated persistence/migration tests.
4. Wire the new service into `AppWorkspaceFactory` and `MainWindowViewModel`; extend Store Management tab, ViewModel draft state, commands, confirmation/discard routing, and `StoreEditorWindow.axaml`.
5. Extend the Design-stage presentation and ViewModel with target selection, Choice warning, read-only behavior, error reporting, and reload synchronization.
6. Add focused App ViewModel and Avalonia headless view tests for Store Editor tab selection/empty/read-only states and Design target binding/selection. Map every spec scenario to one of the preceding focused tests or persistence tests.
7. Run strict OpenSpec validation and `dotnet test .\\FusionCanvas.sln`; record criterion-level results in `verification.md` during apply.

## Open Questions

None that block this manual module. The exact future Printify import payload, provider metadata retention policy, and Choice resizing warning wording are deliberately deferred to the API integration module.
