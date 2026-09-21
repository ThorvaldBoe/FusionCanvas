## Why

The Concept stage exposes SLL generation actions, but a creator who does not already know the term has no concise explanation of what an SLL is or why the generated sketch matters. The IssueTracker decision resolves the product and UX direction: add a compact information box with an info icon, explanatory copy, and a distinct background directly in the SLL section so the explanation is available while the creator is deciding whether to generate or review an SLL.

## What Changes

- Add a persistent, compact SLL explanation information box to the Concept-stage SLL generation section.
- Present an info icon, the approved explanatory copy, and a visually distinct background using shared theme resources.
- Keep the explanation visible in editable and read-only Concept-stage review states, without enabling or changing SLL generation behavior.
- Give the information box and icon meaningful accessible names/description so the explanation is available to keyboard and assistive-technology users.
- Preserve the existing Generate, Regenerate, stale-state, unavailable-state, and error-state behavior and layout order.

The approved IssueTracker copy is retained verbatim: “SLL stands for Symbolic Layout Languate. It's a rough visual sketch of the proposed design.” The current spelling is treated as approved product copy for this module; copy editing is outside scope.

## Capabilities

### New Capabilities

None. The explanation is part of the existing SLL generation capability rather than a separate workflow.

### Modified Capabilities

- `sll-generation`: require an always-available, accessible explanatory information box in the Concept-stage SLL section without changing generation gates or persistence semantics.

## Impact

- Affects the Concept-stage Avalonia view in `src/FusionCanvas.App/Views/MainWindow.axaml` and potentially small presentation/accessibility types or resources in `src/FusionCanvas.App`.
- Adds or updates headless UI coverage in `tests/FusionCanvas.App.Tests` for placement, visibility in editable/read-only states, theme resources, accessible naming, and preservation of existing SLL controls.
- No Application or Domain API, persistence schema, AI request, serialization format, or external dependency changes are expected.
- The module is intentionally small and reviewable: one explanatory affordance in one existing surface, with no new interaction, mutation, or data lifecycle.
