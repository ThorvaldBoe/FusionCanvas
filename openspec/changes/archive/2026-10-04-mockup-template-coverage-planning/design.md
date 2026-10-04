## Context

The current Mockup Template editor persists managed local source-image entries and evaluates readiness through `MockupTemplateSourcePolicy`. The resolver already produces one result per compatible Variant, but the Store editor and Listing stage reduce that information to a generic Draft message. A creator can therefore see uploaded images and Color summaries while still having no clear indication of which Variant coverage is absent.

This module adds a derived coverage-planning experience inside the existing focused Mockup Template editor. It must support both the existing image-first workflow (upload, then assign metadata) and a metadata-first workflow (derive required applicability, then upload or assign an image). Both routes must persist only real source-image entries and must continue to use the existing exact-one readiness authority.

The primary workflow is occasional Store setup and correction, not daily Listing work. Coverage planning belongs in the focused Mockup Template dialog. Listing receives a compact read-only summary and recovery guidance; it does not become a second coverage editor.

## Goals / Non-Goals

**Goals:**

- Make Template readiness explainable at the compatible-Variant level.
- Show resolved, missing, ambiguous, and incomplete coverage without weakening the readiness gate.
- Generate useful derived coverage requirements with Color-first, Size-unrestricted defaults when safe.
- Let each derived requirement start an upload or existing-image assignment with applicability preselected.
- Support an exemplar source image for safe applicability defaults and optional mapping reuse.
- Allow future bulk-upload matching to consume the same derived requirements.
- Preserve current revision, managed-asset, archived-record, and guarded-editor behavior.

**Non-Goals:**

- Persisting empty source-image records or fake image Assets.
- Supporting one Mockup Template with multiple target Design Areas or multi-area composition in one output.
- Automatically classifying images with AI, parsing filenames as authoritative metadata, or synchronizing provider catalogs.
- Changing the exact-one source resolution policy or allowing Draft Templates into Listing generation.
- Replacing the separate bulk-upload work; this module provides the shared coverage target it can use.

## Decisions

### 1. Use a derived coverage plan, not persisted empty source images

Coverage requirements are derived from the active Template target Design Area, active compatible Variants, current source-image entries, and a selected grouping strategy. A requirement becomes a persisted source-image entry only after the creator uploads a valid managed raster Asset or assigns an existing compatible source image.

This preserves the current invariant that `MockupTemplateSourceImage` references a real managed Asset, avoids orphaned or fake files, and means catalog changes can invalidate and regenerate a plan without a migration for placeholder records.

Alternative rejected: allowing nullable source Asset identities. That would spread incomplete-asset handling through persistence, revisions, preview, and generation for a problem that can be represented by derived requirements.

### 2. Keep one authoritative resolution path

The coverage planner consumes the same per-Variant results produced by `MockupTemplateSourcePolicy.Resolve`. The planner may group missing results for presentation, but it must retain the underlying Variant IDs and source-image match IDs. Readiness remains `ReadyForUse` only when every compatible Variant resolves exactly once.

Alternative rejected: a UI-only coverage calculation. Duplicating matching semantics in the view model would allow the editor, Listing, and generation to disagree.

### 3. Default to Color groups with unrestricted secondary options

The default grouping strategy groups compatible Variants by Color when a Color-only applicability condition safely covers the group. Size and other option values remain unrestricted in that requirement. The UI must display this explicitly as `Size: Any` or equivalent; an empty control must not be ambiguous.

When Color grouping is not safe or the offering lacks a usable Color Option, the planner falls back to the smallest safe grouping, such as Color + Size or individual Variants. The selected strategy is visible and can be changed without modifying confirmed source-image records.

### 4. Treat an exemplar as a pattern, not as a source of unsafe pixel data

The creator may choose an existing source row as an exemplar. Generated requirements inherit the target Design Area and safe applicability shape, replacing the exemplar's Color value with the missing Color value where appropriate. Pixel mapping is reused automatically only when image dimensions are compatible. Otherwise the new row remains visibly incomplete and requires explicit mapping; no out-of-bounds mapping is copied.

### 5. Make the focused editor the primary planning surface

The dialog keeps its existing master-detail image table. A compact coverage summary appears above the table, with an expandable detail view for missing and ambiguous requirements. Each requirement has one primary next action: `Add mockup image` or `Assign existing image`. Existing upload, selection, archive, and mapping controls remain the detailed editing surface.

The main Listing surface shows only a concise read-only summary such as `24/27 Variants covered; 3 missing`. It links the creator back to Store settings and must remain keyboard and assistive-technology accessible.

### 6. Preserve guarded draft and context behavior

Coverage-plan choices and new assignments participate in the existing Mockup Template draft baseline. Switching a requirement, Template, Offering, or Store with meaningful unsaved changes uses the established save/discard/cancel behavior. A catalog change that makes the plan stale does not rewrite confirmed source configuration; the editor asks for an explicit refresh before applying newly derived assignments.

## Risks / Trade-offs

- **[Risk] Grouped requirements hide an irregular Variant.** → Keep the underlying Variant IDs, show the affected option-value summaries, and fall back to finer grouping whenever a Color-only group is not safe.
- **[Risk] A creator assumes “Complete” rows guarantee Template readiness.** → Show both per-row completeness and aggregate exact-one coverage; label missing and ambiguous Variant coverage separately.
- **[Risk] Exemplar mapping is invalid for a different image size.** → Reuse mappings only for compatible dimensions; otherwise require a new mapping and retain clear incomplete status.
- **[Risk] Catalog edits invalidate a previously generated plan.** → Treat plans as derived, detect stale context, preserve confirmed data, and require explicit regeneration.
- **[Risk] Bulk upload work develops a competing matching model.** → Define a shared application-facing coverage requirement/result contract and make both workflows consume the same authoritative resolver.
- **[Risk] The feature expands into multi-area mockup composition.** → Keep each Template's single target Design Area explicit and record multi-area output as a later module.
- **[Trade-off] Derived plans do not preserve manually rearranged empty rows across sessions.** → Prefer deterministic regeneration from current catalog and source data in this module; add persistence only if a concrete workflow demonstrates that derived requirements are insufficient.

## Migration Plan

No database migration is required for the first implementation. Existing Templates, source-image entries, applicability records, revisions, and Assets remain valid. The new planner reads them and presents derived requirements.

Implementation should be backward-compatible with Templates that have no local source images, legacy provider-image revisions, archived source rows, or incomplete applicability. Such Templates show the appropriate existing Draft blockers and can enter the new coverage plan when they have an active target Design Area.

If the bulk-upload feature is implemented separately, it should adopt the coverage result/requirement contract rather than adding a second persisted planning model. Rollback is a code rollback; no data transformation is needed.

## Open Questions

There are no unresolved high-impact questions for this module. The following decisions are intentionally fixed for implementation:

- Coverage requirements are derived, not empty persisted source-image records.
- Color-first with unrestricted Size is the default grouping when safe.
- A Template has one target Design Area in this module.
- Mapping reuse is dimension-safe only.
- Draft Templates remain excluded from Listing generation.

## Implementation Plan

### Domain and application foundation

1. Add pure coverage result/value types for grouping strategy, requirement status, affected Variant identity, applicability option values, and matched source-image IDs. Keep the types independent of Avalonia and persistence.
2. Add a domain/application coverage planner that consumes the target Design Area, active Variants, active source images, source-image applicability, and `MockupTemplateSourcePolicy.Resolve` results.
3. Implement grouping modes: Color-first with unrestricted secondary options, Color plus selected options, and individual Variant fallback. Preserve the underlying Variant membership for every group.
4. Return explicit statuses for resolved, missing, ambiguous, and incomplete rows. Do not convert a missing requirement into a fake `MockupTemplateSourceImage`.
5. Extend the existing source-image application boundary with a read/plan operation and assignment metadata sufficient for the focused editor. Reuse `AddAsync`, `UpdateAsync`, managed file import, revision snapshots, and ownership validation for writes.

### Store editor behavior

6. Add a coverage summary/panel view model to the existing `CatalogSetupViewModel` or its focused editor state, with loading, no-target, complete, incomplete, ambiguous, stale, archived/read-only, and recoverable-error states.
7. Add grouping-strategy selection and a `Generate coverage plan` action. Preserve confirmed source rows when the plan regenerates.
8. Add requirement actions for upload and existing-image assignment. Prepopulate Color and unrestricted secondary options from the requirement; retain the current selected-row mapping editor and guarded draft baseline.
9. Add exemplar selection and safe applicability/mapping defaults. Reuse pixel mappings only when dimensions match; otherwise show the normal incomplete mapping state.
10. Add accessible names, help text, keyboard traversal, focus placement, and focus return for coverage actions. Keep the coverage detail progressively disclosed so the existing source-image table remains usable at normal and narrow supported sizes.
11. Ensure catalog/Offering/Template changes invalidate the derived plan and require explicit refresh without overwriting confirmed persisted source configuration.

### Listing diagnostics and shared bulk-upload seam

12. Extend the application-facing Listing diagnostic result with compact coverage counts and grouped affected Variant guidance, while retaining all existing blockers and the ready-only selector.
13. Render the summary as ordinary text and accessible controls in Listing; keep repair actions in Store settings.
14. Define the application-facing coverage requirement/result shape that the separate bulk-upload workflow can consume. Do not implement filename inference or bulk-upload orchestration in this module unless required to prove the shared seam.

### Verification and review

15. Add domain tests for safe Color grouping, Size wildcard behavior, finer fallback grouping, exact-one ambiguity, archived/incomplete rows, and stale plan detection.
16. Add application tests for plan generation, requirement prefill, dimension-safe exemplar mapping, assignment through existing persistence services, revision preservation, and no empty Asset/source-row creation.
17. Add App view-model tests for coverage states, commands, draft preservation, stale refresh, and Listing summaries.
18. Add deterministic Avalonia headless tests for focused coverage-panel construction, bindings, visibility, accessible names, routed assignment actions, focus behavior, and supported narrow sizing. Avoid tests that only assert static text.
19. Add one supplemental real-desktop scenario-pack journey if the existing Appium lane is available: create or open a Template, generate a Color-first plan, assign/upload a missing image, save, close/reopen, and confirm the Listing summary/selector updates from persisted state. Keep the deterministic baseline authoritative.
20. Run criterion-level verification for every scenario in the delta specs, strict `openspec validate`, and `dotnet test .\FusionCanvas.sln -m:1` before implementation is considered complete.

### Acceptance-to-verification mapping

| Acceptance area | Planned evidence |
| --- | --- |
| Explainable resolved/missing/ambiguous/incomplete coverage | Domain planner tests and application state tests |
| Color-first and finer grouping strategies | Domain grouping tests with irregular Variant matrices |
| Upload/assign requirement prefill | Application service tests and view-model command tests |
| Exemplar applicability and mapping safety | Domain/application tests with matching and mismatched image dimensions |
| Partial save and revision behavior | Source-image persistence tests using isolated repository/file fixtures |
| Stale-plan and guarded draft behavior | View-model tests plus Avalonia headless interaction tests |
| Listing counts and affected Variant guidance | Listing application/view-model tests and focused headless binding test |
| Keyboard/accessibility and narrow editor behavior | Avalonia headless visual-tree, routed-input, accessible-name, and sizing tests |
| Bulk-upload shared seam | Contract-level application tests; full bulk-upload behavior remains outside this module |


