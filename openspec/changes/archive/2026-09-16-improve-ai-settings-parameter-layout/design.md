## Design

Use the existing `AiProfileEditorView` as the sole presentation owner. Replace the unlabeled two-column text-box grid with a responsive two-column grid of compact parameter groups. Each group contains a label, one short muted helper line, and the existing bound input. Keep Stop sequences as a full-width multiline field with its existing limit guidance. Preserve the Additional parameters expander and all `IsVisible` bindings.

The helper copy will be plain language: Top P and Min P describe narrowing token choices, Top K describes limiting candidate tokens, Top A describes adaptive token selection, frequency/presence/repetition penalties describe reducing repeated output, Seed describes reproducibility, and Stop sequences describe where generation ends.

## Implementation Plan

1. Update `src/FusionCanvas.App/Settings/AiProfileEditorView.axaml` with labeled parameter groups, helper text, semantic theme resources, and unchanged bindings.
2. Extend `tests/FusionCanvas.App.Tests/Settings/AiSettingsViewTests.cs` with a headless assertion that supported parameter labels and guidance render and unsupported fields remain absent.
3. Run focused App tests, solution tests, strict OpenSpec validation, and record criterion-level evidence in `verification.md`.

No data migration, application-layer change, or new abstraction is required. Do not reopen parameter semantics, ranges, provider behavior, or profile persistence decisions.
