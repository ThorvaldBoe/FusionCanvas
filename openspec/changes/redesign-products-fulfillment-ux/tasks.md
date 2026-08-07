## 1. Catalog editor navigation state

- [x] 1.1 Add Products overview, Product detail, and fulfillment offering detail state to `StoreManagementViewModel`, with guarded Back, breadcrumb, open-product, and open-offering transitions.
- [x] 1.2 Add derived Product and offering summary counts from refreshed `ProductSupplierSetupState`; keep selection and empty-state behavior coherent after create, delete, or remove operations.
- [x] 1.3 Route every level or selection transition through the existing Save, Discard, and Cancel safeguard, including cancellation of unsaved new Product/offering drafts.
- [x] 1.4 Add section expansion state and focus-target notifications for Basics, Variants, Printable areas, Advanced, and newly opened add forms without changing persistence contracts.

## 2. Progressive-disclosure view

- [x] 2.1 Refactor the Products & fulfillment section of `StoreEditorWindow.axaml` from the three-column simultaneous editor into the Products overview with explicit "New product" action, product summaries, and useful empty state.
- [x] 2.2 Add Product detail layout with breadcrumb/back navigation, compact Product details, fulfillment offering list, summary counts, and one primary "Add fulfillment offering" action.
- [x] 2.3 Add fulfillment offering detail layout with breadcrumb/back navigation, summary card, Basics, Variants, Printable areas, and Advanced sections; keep the selected Product context visible.
- [x] 2.4 Replace generic creation/removal labels with "Add variant", "Add printable area", "Remove variant", "Remove printable area", "Delete offering", and "Delete product" where applicable.
- [x] 2.5 Make fixed-provider fields conditional, preserve the Printify Choice network warning without a fabricated provider, and place external identifiers in Advanced.
- [x] 2.6 Convert always-visible variant and printable-area forms into disclosed, labeled forms; preserve applicable-variant selection semantics and default-to-all behavior.
- [x] 2.7 Ensure disclosure controls, breadcrumbs, back actions, forms, confirmations, and saves are keyboard reachable with predictable focus and accessible names.

## 3. Focused verification

- [x] 3.1 Add view-model tests for level navigation, derived counts, post-mutation selection, empty aftermath, section state, and guarded Save/Discard/Cancel transitions.
- [x] 3.2 Add Avalonia headless tests for overview/product/offering visibility, empty and populated states, explicit action labels, Choice conditional controls, disclosure behavior, focus, and keyboard reachability.
- [x] 3.3 Add or update focused tests for variant creation, printable-area creation, applicable variants, and destructive confirmation paths without changing existing service/domain expectations.
- [x] 3.4 Review the implementation against every scenario in `specs/product-supplier-setup/spec.md` and correct the delta artifacts if an acceptance condition is not testable or has changed.
- [x] 3.5 Run `openspec validate` and resolve all validation findings.
- [ ] 3.6 Run the full solution baseline `dotnet test .\\FusionCanvas.sln` and confirm no domain, persistence, or external-integration scope was introduced.
