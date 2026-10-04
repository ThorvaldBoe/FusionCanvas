## Context

The existing generation service already persists attributable mockups as ordinary managed `Asset` records linked to the Item. The Listing surface loses the value of those results by reducing them to filenames. The extension should complete the result lifecycle around the existing asset boundary: inspect, use outside the workspace, remove, and invalidate when the source changes.

## Conceptual model

```text
Design asset + assignment + Color + template revision
                         │
                         ▼
                 Generated mockup asset
                         │
        ┌────────────────┼────────────────┐
        ▼                ▼                ▼
     inspect          save copy          remove
                         │
                         ▼
                 downstream listing work

Design mutation ────────┴──── invalidates derived output
```

The generated image is a disposable, attributable derivative. It is not a second source Design and must never be silently mistaken for current work after its source changes.

## Decisions to preserve

- Keep generated outputs in existing `Asset`/`AssetLink` storage; do not add a parallel mockup table unless implementation proves the current model cannot support the required lifecycle.
- Use the existing output metadata (`itemId`, Color, template ID/revision, and Design asset ID) as the invalidation basis.
- Keep Listing generation protected by the existing Item status/stage policy. Preview and save-copy are non-destructive read operations; removal follows the existing content-mutation policy.
- Treat deletion as an explicit, confirmed operation. Do not auto-delete because a thumbnail was clicked or a template was selected.
- Do not regenerate automatically after invalidation. The creator must review the updated Design and explicitly apply a template again.
- Prefer the existing focused asset-preview interaction for enlarged view unless headless/runtime exploration shows it cannot satisfy Listing context and keyboard requirements.

## Affected layers and responsibilities

- Domain: only add a derived-output/invalidation policy if the existing Item workflow policy cannot express the rule without UI or persistence knowledge. Do not place file deletion or preview behavior in Domain.
- Application: extend the mockup-generation boundary or add a focused mockup-consumption service for listing outputs, preview stream access, safe export-copy, confirmed removal, and invalidation. Coordinate repository snapshots and managed-file cleanup atomically as far as the existing ports permit.
- Integration: reuse `IWorkspaceFileReader`, `IWorkspaceFileOutputStore`, and `IWorkspaceFileDeleter`/managed cleanup implementations. Keep path validation and file operations behind application-facing ports.
- App: replace the filename-only `ItemsControl` with a compact gallery ViewModel/item model. Bind thumbnail, attribution, preview, save-copy, remove, busy, unavailable, confirmation, empty, and stale-after-design-change states. Keep UI code free of asset lifecycle decisions.
- Design mutation paths: identify every operation that changes the effective Design asset or assignment. Call the invalidation boundary after the mutation has a successful persisted result, and surface the affected-output count to the active workflow or the next Listing load.

## State and edge cases

1. No outputs: explain that mockups have not been generated and point to the existing template/apply action.
2. Populated outputs: render thumbnails and compact attribution; do not require opening a file browser to judge them.
3. Missing/unreadable output: keep an attributable placeholder with recovery guidance; do not silently omit it.
4. Preview busy: disable conflicting actions for that output, keep the rest of Listing usable where safe, and restore focus on close.
5. Download failure: retain the managed output and report a retryable destination/file error.
6. Delete confirmation: explain that the generated output and managed copy will be removed, while source Design/template assets remain.
7. Partial delete cleanup: persisted removal wins; surface cleanup diagnostics without reintroducing the output as current.
8. Design replacement/removal: invalidate precise dependents when metadata permits; otherwise invalidate all Item mockups.
9. Protected Item: preserve the existing read-only policy; do not make destructive output actions a loophole.
10. Selection/tab changes: operations must remain bound to the originating Item and not apply to a newly selected context.

## Implementation plan

1. Confirm the source Design mutation boundaries and choose precise-versus-item-wide invalidation behavior for each path; record any unresolved product decision before coding.
2. Add application contracts and result records for output listing, preview/open, export-copy, removal, and invalidation diagnostics, reusing existing managed-file ports.
3. Implement repository/file-store orchestration with snapshot-safe removal, best-effort managed-file cleanup, missing-file handling, and metadata-based dependent selection.
4. Integrate invalidation into every accepted Design mutation that can change a generated mockup's source or assignment, without deleting source Design assets.
5. Replace the Listing filename list with a gallery ViewModel and controls, using the existing preview window pattern where appropriate. Preserve protected-state behavior and keyboard reachability.
6. Add framework-free application tests for precise and fallback invalidation, removal, missing files, export-copy, protected Items, and context isolation; add Integration file-boundary tests.
7. Add Avalonia headless tests for thumbnail/empty/error/read-only states, preview/remove/download command wiring, confirmation, focus/selection behavior, and stale-result communication. Decide whether one real-desktop journey adds material evidence after headless coverage.
8. Run criterion-level verification, `openspec validate`, and the full solution baseline before implementation is considered complete.

## Decisions not to reopen during implementation

- Generated mockups remain ordinary Item-linked managed assets.
- Generation remains explicit and local-first.
- Removal is explicit and confirmed.
- Source Design assets are never deleted as a consequence of mockup removal or invalidation.
- A stale derivative is not silently retained merely because it could still be displayed.
