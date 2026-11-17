# Design: Reactivate Archived Blueprints

## Concept and behavior

Blueprint archive currently marks the Blueprint and all catalog-owned descendants archived. The editor has no reverse operation. Add a confirmed restore operation at the same Blueprint detail level. On success, preserve selection and reload the editor so the active Blueprint and its restored Offering graph are visible.

Restoration follows `RestoreOfferingCascadeAsync`: use the Blueprint identity to find its Store-scoped Offerings and all owned descendant records, then clear `IsArchived` across that graph in the same repository mutation. Do not create records or alter identifiers, ownership, relationships, content, or Item references. Require an editable Store, explicit confirmation, an existing archived Blueprint, and reject an already-active Blueprint. An archived Store remains read-only.

## Implementation plan

1. **Application contract and mutation**
   - Add `RestoreBlueprintCascadeRequest` and `ICatalogSetupService.RestoreBlueprintCascadeAsync`.
   - In `CatalogSetupService`, validate Store writability, confirmation, Store-scoped Blueprint identity, and archived state.
   - Gather owned Offering, Option, and Mockup Template identities and atomically restore the Blueprint, Offerings, Options, Option Values, Variants, Placeholders, Mockup Templates, template-color records, and source-image records. Preserve all other fields and records.
   - Update current and timestamps consistently with existing catalog restore operations where the involved records expose them.

2. **Store Editor view model and view**
   - Add a pending Blueprint restore state, confirmation message, command, and state notifications in `ProductCatalogEditorViewModel`.
   - Expose the action only for an archived selected Blueprint in an editable Store; disable it during another operation or unsaved changes according to existing command guards.
   - On confirmation call the service, report recoverable errors, clear confirmation on success, reload, and retain the restored Blueprint selection.
   - Add the archived-only **Reactivate Blueprint** button beside the archive/delete lifecycle actions and a focused confirmation panel explaining that the catalog configuration will return to active use.

3. **Tests and verification**
   - Add application service tests for full cascade restoration, stable identity/link preservation, external Item relationship preservation, cancellation, invalid/missing/already-active records, archived Store, and atomic persistence failure.
   - Extend Store Editor headless tests for action visibility, confirmation/cancel and successful command path, archived Store read-only behavior, and retained selection.
   - Run focused tests, `dotnet test .\FusionCanvas.sln -m:1`, and `openspec validate --strict`; record scenario-level evidence.

## Edge cases and compatibility

- Restoration must not recreate or normalize compatibility-projection records; it only changes lifecycle state on records already owned by the Blueprint.
- Protected external relationships are retained unchanged. They are blockers for permanent deletion, not for restore.
- If mutation persistence fails, repository transaction semantics must leave the entire snapshot unchanged.
- Descendant archive provenance does not exist. Restore behavior intentionally mirrors the established Offering restore cascade and restores all owned descendants.
- No schema migration is needed; lifecycle state already persists as `IsArchived`.

## Decisions not to reopen

- Reactivation restores the full owned catalog cascade, matching Offering reactivation semantics.
- The action is named **Reactivate Blueprint** in the UI.
- Reactivation requires a confirmation because it changes availability of the entire catalog graph.
- No Appium journey is added; deterministic service and headless UI coverage are the acceptance evidence for this narrow state-dependent command.
