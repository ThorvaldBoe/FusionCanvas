## Why

Mockup Template creation is currently impossible in the production application because Save requires a provider-catalog image while production deliberately supplies no provider-catalog integration. Manual catalog setup must remain useful offline: users need to preserve partial Mockup Template work as a Draft and complete it over time, while downstream preview/render workflows must accept only configurations that are Ready for use.

## What Changes

- Let users create and edit a Mockup Template without provider integration, provider synchronization, a template image, an image-space mapping, a target Design Area, or Color applicability. A nonblank template name and editable Offering context are the minimum creation requirements.
- Derive one authoritative readiness result from persisted template configuration. A template is **Draft** until it has every input required by the existing render contract; it is **Ready for use** only when its current revision has a usable image reference, positive image dimensions, an in-bounds positive mapping, an active same-Offering Design Area, at least one active same-Offering Color, compatible sellable Variants, and no known image/Color incompatibility.
- Keep lifecycle state derived rather than independently user-editable or separately persisted, preventing stored status from drifting from configuration.
- Separate “Save Draft” eligibility from “Ready for use” eligibility. Save re-evaluates after every editable field and remains unavailable only while busy, read-only, outside a valid Offering context, or missing the minimum identity.
- Show Draft/Ready status and a concise, complete readiness checklist inside the focused Mockup Template dialog and on the selected template summary. Missing provider data is readiness guidance, not a creation blocker.
- Preserve the draft and show recoverable in-dialog guidance after validation or persistence failure. Successful create/update persists exactly once, refreshes/selects the saved template, and closes the dialog.
- Persist and migrate partial templates safely by allowing readiness-related relationships and revision image/mapping fields to be absent without weakening validation when those fields are supplied.
- Keep provider-catalog integration optional. When provider data is available it may prefill or complete fields, but the save path does not re-fetch or require a provider catalog.
- Add focused Domain, Application, ViewModel, SQLite migration/round-trip, and Avalonia headless tests, including the production no-provider path and criterion-level Draft/Ready transitions.

This is one cohesive delivery module because it changes one outcome end to end: a manually managed Mockup Template can always be preserved, while render consumers receive a trustworthy readiness gate. The domain rule, persistence representation, application save path, focused editor feedback, and deterministic verification must move together to avoid another UI-only state that cannot be persisted.

Dependencies:

- The normalized Blueprint Offering, Design Area, Color Option Value, Mockup Template revision, and image-space mapping models already introduced by the catalog/mockup setup work.
- Existing managed workspace data and SQLite migration infrastructure.

Non-goals:

- Implementing Printify or another provider API, catalog synchronization, credentials, or network access.
- Adding local image import/upload, drag-and-drop, image editing, composition, or rendering.
- Making Draft templates available to customer-facing preview/render selection.
- Changing Design Area, Color, Variant, archive, or cross-Offering ownership rules outside their effect on readiness.

Primary workflow and UX placement:

- Store owners occasionally create or refine templates in the existing Store Editor-owned Mockup Template dialog; no persistent main-workspace footprint is added.
- The focused dialog keeps progressive configuration available, clearly distinguishes save eligibility from readiness, preserves unsaved-change safeguards, and keeps validation, persistence errors, keyboard flow, and success selection within the active modal context.

Key risks are migration of required foreign keys to nullable Draft configuration, keeping revisions attributable when incomplete configuration changes, and preventing any future render/query path from treating “saved” as “Ready for use.” Verification therefore covers migration, round-trip, derived readiness, command transitions, exactly-once persistence, dialog state, and strict regression tests.

## Capabilities

### New Capabilities

- `mockup-template-readiness`: Defines partial Draft persistence, derived Ready-for-use criteria, revision behavior, and the gate that protects preview/render consumers.

### Modified Capabilities

- `product-supplier-setup`: Changes the focused Mockup Template editor from provider-gated creation to manual Draft saving with explicit readiness feedback and optional provider assistance.
- `local-sqlite-persistence`: Allows partial Mockup Template configuration to round-trip and introduces a safe versioned migration for nullable readiness relationships.

## Impact

- **Domain:** `MockupTemplate`, `MockupTemplateRevision`, `MockupTemplatePolicy`, and related summaries gain optional configuration and a single readiness evaluation; the template name and Offering identity remain required.
- **Application:** Mockup Template create/update requests and services accept partial configuration, validate supplied relationships without a live provider dependency, return readiness details, and expose only Ready templates to future render eligibility queries.
- **Integration:** SQLite schema, migration, mappings, snapshot validation, and workspace-package compatibility preserve nullable target/image/mapping configuration and all existing complete templates.
- **App:** `CatalogSetupViewModel`, presentation models, and `MockupTemplateEditorWindow` separate Save Draft state from readiness, show all unmet readiness requirements and in-dialog errors, and retain existing modal/focus/discard behavior.
- **Tests:** Domain, Application, Integration, ViewModel, and Avalonia headless suites gain transition, persistence, migration, failure, and successful-save coverage. No new external dependency or network requirement is introduced.
