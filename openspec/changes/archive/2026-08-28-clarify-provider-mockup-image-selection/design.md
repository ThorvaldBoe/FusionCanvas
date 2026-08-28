## Context

`CatalogSetupViewModel.LoadProviderMockupsAsync` currently collapses no source, source-unavailable, empty, and exceptions into one free-text `ProviderCatalogMessage`; it has no observable loading state. `StoreEditorWindow.axaml` exposes a placeholder-only ComboBox and preview message, so source provenance and unsupported local-import behavior disappear precisely when the selector is empty.

## Goals / Non-Goals

**Goals:** make provider-catalog provenance, selection instructions, unsupported local import, each load state, and a supported next action explicit and accessible beside the existing selector/preview.

**Non-Goals:** provider synchronization, provider setup navigation, retry commands, upload, drag/drop, file validation/storage, candidate contract changes, or placement/persistence changes.

## Decisions

1. Add an App-only `ProviderCatalogLoadState` enum (`Loading`, `Available`, `Empty`, `Unavailable`, `Error`) rather than infer state from strings or collection counts.
2. Expose stable presentation strings from `CatalogSetupViewModel`: permanent instructions, current state guidance, and recovery guidance. Preserve `ProviderCatalogMessage` for compatibility but make state authoritative.
3. Set `Loading` before awaiting the existing source; classify its descriptor, candidate count, and exceptions deterministically. No Application or Integration contract changes are needed.
4. Put the persistent visible label and explanation immediately before the selector, with the same text as its automation name. Put live state text after it and recovery guidance only for empty/unavailable/error.
5. State the negative capability plainly: local upload and drag/drop are not available. Do not render a drop target or ambiguous **Choose image** affordance.

## Risks / Trade-offs

- [Existing consumers rely on `ProviderCatalogMessage`] → retain and populate it while adding typed state.
- [Fast loads make loading hard to see] → expose state before awaiting and verify with a controlled pending test collaborator.
- [Error text may contain low-level details] → prefix it with user-facing recovery guidance; do not expose secrets or stack traces.
- [#201 moves the markup into a dialog] → keep logic independent and resolve the small XAML placement conflict by applying this labeled guidance inside the new dialog after #201 merges.

## Migration Plan

No data migration. Reverting the enum/properties and XAML guidance restores prior presentation without affecting templates.

## Open Questions

None. Provider setup/sync is guidance only until its independently tracked integration exists.

## Implementation Plan

1. Add `ProviderCatalogLoadState.cs` in App Stores and state/message properties in `CatalogSetupViewModel.cs`.
2. Update `LoadProviderMockupsAsync` to transition through Loading and classify available, empty, unavailable, and error results while preserving existing candidates and compatibility messages.
3. Update the Mockup Template editor XAML with a persistent label, automation name, permanent provenance/import limitation, current state, and conditional recovery text.
4. Add deterministic ViewModel tests using successful, pending, unavailable, empty, and throwing candidate sources; add Avalonia headless assertions for visible instructions, accessible selector identity, and every rendered state.
5. Run focused and full tests, strict OpenSpec validation, and criterion-level verification. Do not add integration behavior.

## Acceptance-to-Verification Mapping

| Scenario | Verification |
| --- | --- |
| User opens provider image selection | Headless test asserts visible label/instructions, automation name, and no drop/upload affordance. |
| Provider catalog is loading | Pending-source ViewModel/headless test asserts Loading before completion and persistent guidance. |
| Provider catalog provides candidates | ViewModel/headless test asserts Available, candidates, and choose-view guidance. |
| Provider catalog is empty | Empty descriptor test asserts Empty and setup/sync recovery. |
| Provider catalog is unavailable | Null/unavailable source tests assert reason plus configuration/sync recovery. |
| Provider catalog request fails | Throwing source test asserts Error, no candidates, and retry/setup guidance. |
