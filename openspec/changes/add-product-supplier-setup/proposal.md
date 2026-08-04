## Why

Creators currently have to research a product's provider-specific printable areas outside FusionCanvas before beginning a design. That repeats manual work and makes it easy to prepare artwork for the wrong size or position.

Issue [#94](https://github.com/ThorvaldBoe/FusionCanvas/issues/94) needs a durable, store-scoped product catalog so a creator can maintain the products and fulfillment choices they actually use, then deliberately select the relevant design targets while working on an Item.

The catalog is deliberately local-first and manual in this module. The same structure must retain Printify's meaningful catalog relationships so future API import can map data into the application without forcing a redesign or leaking provider DTOs into the Domain.

## Origin

Primary issue: [#94](https://github.com/ThorvaldBoe/FusionCanvas/issues/94)

## What Changes

- Add a store-management Product and fulfillment catalog where a creator manually maintains Printify product blueprints, fixed-provider offerings, and Printify Choice network offerings.
- Capture provider-offering variants and their option values, plus design areas containing Printify-compatible position, decoration method, pixel width, pixel height, and applicable variants.
- Let an Item's editable Design stage use no configured target or select one or more configured design areas from its Store; retain the selected targets with the Item.
- Present Printify Choice as a variable fulfillment network rather than a fixed provider, including a clear consistency warning.
- Keep catalog setup in the focused store-management surface; do not add it to the application Settings window.

### Included user workflows

1. A creator opens **Manage stores**, selects an active Store, and opens a **Products & fulfillment** tab. This occasional administration stays out of the main creative workspace.
2. The creator creates a product blueprint (for example, a Gildan 64000), then adds a fulfillment offering for either a named provider or the Printify Choice network.
3. Within an offering, the creator enters the available variant option values and combinations, then adds its printable areas. A printable area is defined by its position, decoration method, pixel dimensions, and the variants to which it applies.
4. The creator edits or removes catalog records through explicit actions. A record that is in use by an Item cannot be removed in a way that silently invalidates that Item's selected targets.
5. At the editable Design stage, the creator can continue without a configured target, or select one or more compatible printable areas from the Item's Store. The selections persist with the Item and are shown as compact target guidance alongside the existing design-file workflow.

### Interaction and state decisions

- Opening Store Management selects the active Store when possible; the catalog tab loads that Store's data only. It shows a useful empty state and an obvious first action when no product has been configured.
- Catalog record creation and structural edits use drafts with explicit Save and Cancel. Selection changes and closing Store Management prompt before discarding meaningful unsaved catalog drafts.
- Catalog mutation, target selection, and Design-stage loading expose a recoverable inline error on validation or persistence failure; no partial change is treated as confirmed.
- Archived Stores expose their catalog as read-only and cannot be used to add or edit Item targets. A Design-stage selection is read-only when the Item is not editable under the existing workflow rules.
- Destructive removal is explicit and confirmed. If a product, offering, or area has selected Item targets, removal is blocked with guidance to first clear or replace those targets; no historical target is silently erased.
- Keyboard focus lands in the primary name field for a new catalog draft and returns to the invoking management control when Store Management closes.

### Data decisions already made

- Catalog data is scoped to one Store, never application-wide or merely workspace-wide.
- A **product blueprint** represents the underlying blank product and stores a provider-independent local identity plus optional external platform identity.
- A **fulfillment offering** joins a blueprint to either a named fixed provider or the Printify Choice network. It owns the provider-specific catalog facts.
- A **variant** is a concrete available option combination. Color and size are option values on a variant/offering, not global entities; this avoids assuming colors or sizes have cross-provider identity.
- A **design area** belongs to one offering and includes Printify-compatible `position`, `decoration method`, `width`, `height`, and an explicit set of applicable variants.
- An Item stores references to zero or more selected design areas from its own Store. Zero targets is intentional and valid.
- Printify Choice is a fulfillment mode, not a normal provider. Its underlying fulfillment provider is variable and opaque, so the UI must disclose that dimensions or placement may vary and keep no false provider identity.

### Boundaries and non-goals

This module delivers the single outcome of selecting trustworthy, Store-configured design targets during design work. It does not attempt to make FusionCanvas a complete Printify product editor.

- No Printify credentials, API calls, import, synchronization, refresh, merge, or conflict resolution.
- No marketplace publishing, listing export, pricing, shipping, mockup generation, or provider-order routing.
- No automatic artwork resizing, placement editor, image validation, DPI calculation, or conversion from a design area into final artwork dimensions.
- No global product library shared between Stores and no Settings-window consolidation of Store Management.
- No generic multi-platform catalog abstraction beyond the provider-neutral domain shape required to avoid a Printify DTO dependency. Supporting Printful or another platform remains future work.

## Capabilities

### New Capabilities

- `product-supplier-setup`: Store-scoped manual management of product blueprints, fulfillment offerings, variants, and design areas.
- `design-area-target-selection`: Optional multi-selection of configured, store-compatible design-area targets for an Item at the Design stage.

### Modified Capabilities

- `basic-product-workflow`: The Design Stage Tool gains optional configured design-area targets while retaining its existing design-file behavior.
- `store-management`: The focused Store Editor gains the store-scoped product and fulfillment catalog tab.

## Impact

- Domain and application layers gain store catalog and Item design-target concepts, validation, and use cases.
- SQLite workspace persistence gains catalog and Item-target tables, snapshot integration, validation, and schema migration coverage.
- The store-management and Design-stage Avalonia surfaces gain focused controls and headless view tests for meaningful bindings and selection behavior.
- The module is manual and local-first: Printify credentials, catalog import/synchronization, publishing, pricing/shipping management, mockups, and the future consolidation of store management with Settings are non-goals.
- The module is coherent because one outcome—selecting accurate design areas from the Store's maintained catalog—requires both catalog setup and Design-stage consumption, but excludes all network synchronization complexity.

## Dependencies, Risks, and Verification

### Dependencies

- Existing Store Management provides the focused administration surface, active Store selection, and draft/unsaved-change conventions.
- Existing workspace snapshot persistence and SQLite schema migration infrastructure provide local durability.
- Existing Design-stage editability rules remain authoritative for whether targets can be changed.

### Risks

- **Modeling Printify too narrowly:** preserving only width and height would prevent faithful provider import later. Mitigation: retain position, decoration method, external identifiers, variant applicability, and room for provider-bound metadata at the Integration boundary.
- **Treating Printify Choice as a fixed provider:** this could give creators false confidence in exact placement. Mitigation: model it as a separate network fulfillment kind and show a consistency warning.
- **Catalog administration overwhelming regular work:** dense setup forms could displace the creative workflow. Mitigation: keep it in Store Management, use progressive disclosure, and make the Design surface a compact selector rather than a catalog editor.
- **Destructive catalog edits orphan Item records:** removing setup data may make prior work ambiguous. Mitigation: validate references and block removal while it is selected by an Item.
- **Persistence migration regression:** the workspace repository rewrites full snapshots and maintains explicit schema versions. Mitigation: add fresh-database, upgrade, round-trip, foreign-key, and invalid-reference tests.

### Verification approach

- Domain tests protect Store ownership, fixed-provider versus Choice-network invariants, variant/design-area applicability, and Item target validation.
- Application tests use deterministic repositories to protect catalog CRUD, draft-safe failure outcomes, deletion blocking, and Design-stage target selection.
- Integration tests verify SQLite migration and complete catalog/target round trips using isolated temporary databases.
- Avalonia headless view tests cover Store Management tab construction, selection, read-only/empty/error states, and the Design-stage target controls where binding or selection behavior carries framework risk.
- Strict OpenSpec validation and `dotnet test .\FusionCanvas.sln` remain completion gates; no network, Printify account, or interactive desktop is required.
