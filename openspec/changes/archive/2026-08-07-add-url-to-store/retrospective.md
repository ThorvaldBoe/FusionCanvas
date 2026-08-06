# add-url-to-store Retrospective

## Outcome

Users can now record an optional storefront URL for a store in the store editor's Basic info tab. The URL is persisted as store-scoped context via the existing `Store.MetadataJson` `url` key, survives workspace reload, and behaves exactly like the sibling optional context fields (description, notes, target market, brand direction, planning context) across create, update, unsaved-changes detection, and field restore. No schema migration was required, and existing workspace databases remain fully compatible.

Scope delivered exactly matched issue #142 ("add url to store"); no URL validation, link-opening, marketplace integration, or store-selector changes were included.

## Feedback-Driven Adjustments

| Initial assumption | Evidence | Correction | Classification | Applicability | Promotion |
|---|---|---|---|---|---|
| Adding an optional store context field may require a schema/column migration | Existing context fields persist through `Store.MetadataJson` with string keys and `SetOptional`; `ToContext` reads them back with `GetValueOrDefault`, absent keys default to `null` | Reuse the metadata key-value mechanism; add `url` key with no Domain/column change and no migration; empty input is stored as absent | Ordinary implementation of an established pattern | Reusable for any future optional scalar store or niche context field | Already embodied in the `store-management` accepted spec's declared context surface and in `design.md`; no separate durable source needed |
| A new Bound TextBox on a user-facing surface necessarily needs a headless view test | Existing `StoreEditorHeadlessTests` already cover editor construction; the URL TextBox is a plain string-bound `field`-class control with no focus/selection/visual-tree risk | Rely on framework-free view-model tests and the existing headless suite; verification records this rationale | One-off verification judgment | Change-specific | Recorded in `design.md` Verification approach |

## Learning Review

- Result: reusable lessons identified
- Evidence reviewed: proposal.md, delta spec, design.md (Implementation Plan + Verification approach), tasks.md, verification.md criterion-level evidence, fc-verifier pass, strict OpenSpec validation (change + --all), build (0/0), and full `dotnet test` suite (1132 passed).
- Promotions completed:
  - Store URL accepted behavior is promoted into the `store-management` capability specification via delta-spec sync.
  - The "reuse the MetadataJson optional-key mechanism for scalar context fields" rationale is captured in `design.md` and remains the established pattern for sibling fields.
- Deferred promotions: none.

Promotion confirmation: These reusable lessons are fully captured in the synced accepted specification and the change's design record; no additional durable source (ui-guidelines, ux-guidelines, architecture guidance, or OpenSpec workflow spec) requires a change, because the design pattern already existed and the capability spec now reflects the added field.
