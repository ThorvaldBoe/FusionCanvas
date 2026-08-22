# Verification

Status: Planned. Complete during implementation and final QA; do not mark a scenario passed without concrete evidence.

## Criterion-level evidence

| Capability | Scenario | Result | Evidence |
| --- | --- | --- | --- |
| Blueprint Offering List | User opens a Blueprint with Offerings | Not run | Planned view-model and headless tests. |
| Blueprint Offering List | User opens Blueprint Basics | Not run | Planned headless disclosure/same-window test. |
| Blueprint Offering List | User opens a Blueprint without Offerings | Not run | Planned headless empty-state test. |
| Blueprint Offering List | User reviews an archived Store | Not run | Planned view-model and headless read-only tests. |
| Catalog progressive disclosure | User opens the catalog editor | Not run | Planned navigation/headless test. |
| Catalog progressive disclosure | User opens a Blueprint | Not run | Planned navigation/headless test. |
| Catalog progressive disclosure | User opens a Blueprint Offering | Not run | Planned navigation/headless test. |
| Catalog progressive disclosure | User opens a focused management surface | Not run | Planned navigation/context test. |
| Offering overview | User reviews an Offering overview | Not run | Planned view-model/headless test. |
| Offering overview | Offering overview preserves the approved composition | Not run | Planned headless hierarchy test. |
| Offering overview | User changes a fixed Print Provider | Not run | Existing behavior plus focused regression test. |
| Offering overview | User reviews incomplete setup | Not run | Planned summary-state test. |
| Offering overview | User reviews blocked setup | Not run | Planned prerequisite-route test. |
| Offering overview | User reviews Provider identity | Not run | Planned terminology test. |
| Offering overview | User reviews a Provider-Network Offering | Not run | Planned variable-network test. |
| Offering overview | User returns from focused management | Not run | Planned refresh/focus test. |
| Variant management | User opens Variant management | Not run | Planned navigation/headless test. |
| Variant management | User scans available choices | Not run | Planned projection/headless test. |
| Variant management | User manages values for one Option | Not run | Planned disclosure/cancellation test. |
| Variant management | User enables provider-catalog choices | Not run | Existing behavior plus regression test. |
| Variant management | User scans sellable Variants | Not run | Planned Option-kind projection test. |
| Variant management | User starts one Variant draft | Not run | Planned draft-state test. |
| Variant management | User starts a bulk Variant draft | Not run | Planned draft-state test. |
| Variant management | User creates one sellable Variant | Not run | Existing Application behavior plus UI regression test. |
| Variant lifecycle | User cancels a Variant draft | Not run | Planned cancellation/focus test. |
| Variant lifecycle | User closes Option Value management | Not run | Planned cancellation/focus test. |
| Variant lifecycle | User leaves with unsaved Variant changes | Not run | Existing discard-guard regression test. |
| Variant lifecycle | User retires a referenced Variant | Not run | Existing dependency-policy regression test. |
| Variant lifecycle | Provider catalog is unavailable | Not run | Planned unavailable-state test. |
| Design Areas | User opens Design Area management | Not run | Planned projection/headless test. |
| Design Areas | Design Area management preserves master-detail composition | Not run | Planned headless region test. |
| Design Areas | User reviews maximum size and artwork guidance | Not run | Planned hierarchy test. |
| Design Areas | User creates a Design Area for all Variants | Not run | Existing behavior plus disclosure test. |
| Design Areas | User limits a Design Area to compatible Variants | Not run | Existing behavior plus disclosure test. |
| Design Areas | User reviews a lifecycle action | Not run | Planned action-prominence/dependency test. |
| Design Area dimensions | User reviews maximum design dimensions | Not run | Planned view-model/headless test. |
| Design Area dimensions | User reviews recommended artwork guidance | Not run | Planned headless grouping test. |
| Design Area dimensions | Secondary physical dimensions cannot be derived | Not run | Planned view-model fallback test. |
| Design Area dimensions | User enters invalid maximum dimensions | Not run | Existing validation regression test. |
| Mockup Templates | User opens Mockup Template management | Not run | Planned projection/headless test. |
| Mockup Templates | Mockup Template management preserves master-detail composition | Not run | Planned headless region test. |
| Mockup Templates | Provider image is unavailable | Not run | Planned unavailable-preview test. |
| Mockup Templates | User creates a template from a provider-catalog image | Not run | Existing behavior plus regression test. |
| Mockup Templates | Target Design Area is incompatible | Not run | Existing compatibility regression test. |
| Mockup Templates | Offering has no Design Areas | Not run | Existing blocked-route regression test. |

## Commands

- Focused tests: not run.
- `dotnet test .\FusionCanvas.sln`: not run.
- `openspec validate align-catalog-management-with-description-designs --strict`: passed for the proposal package on 2026-08-22; rerun after implementation and final evidence updates.

## Limitations and optional evidence

- Deterministic tests are the completion gate. Optional live desktop review may supplement visual judgment but does not replace scenario evidence.
- Semantic UI descriptions and wireframes are illustrative references, not pixel-perfect acceptance artifacts.
