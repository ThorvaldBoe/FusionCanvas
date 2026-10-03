## Why

The Design stage can show a selected Offering and an empty or partial slot grid, but it does not identify whether the underlying Store catalog is incomplete or whether a Mockup Template is usable. This makes a creator leave Design, inspect Store settings, and infer the repair path from several disconnected screens. Iteration 2 adds one read-only, Offering-scoped explanation at the point where the problem is encountered.

## What Changes

- Add the existing derived Offering readiness projection to the Design-stage load state when the selected configuration resolves to a normalized Blueprint Offering.
- Show a compact catalog/mockup readiness summary and named next-step guidance before the Design color and slot workflow.
- Reuse the same readiness policy and creator-facing blocker translation already used by Store Management.
- Preserve Design artwork readiness, configuration selection, stale-configuration recovery, persistence, and Listing eligibility behavior.

## Capabilities

### New Capabilities

- `design-offering-readiness-guidance`: Presents normalized Offering setup readiness and actionable catalog blockers in the Design stage.

### Modified Capabilities

None. This iteration adds presentation of derived state without changing existing Design or Listing eligibility requirements.

## Impact

- **Application:** Extend `DesignStageState` with an optional `OfferingReadinessSummary` derived from the current workspace snapshot.
- **App:** Add read-only Design-stage status and guidance bindings, sharing the existing readiness translator.
- **Tests:** Add Design application and deterministic headless view coverage for incomplete and ready normalized Offerings, including read-only preservation.
- **Persistence:** No schema, migration, or write-path changes.
- **UX:** The message belongs in the main Design workflow because it is encountered while selecting colors and assigning artwork; it is compact and progressive rather than a new setup editor. No live desktop/Appium journey is warranted for this read-only text/state projection.
