# Verification

## Status

Implementation is complete and remains limited to the App presentation layer plus deterministic headless UI tests.

## Planned evidence

| Acceptance scenario | Planned evidence |
| --- | --- |
| Acceptance scenario | Evidence | Result |
| --- | --- | --- |
| Keyboard operation | `SectionVisible_ForConceptStage` and `AssertSllExplanation` in `tests/FusionCanvas.App.Tests/SllSectionHeadlessTests.cs` verify Generate precedes Regenerate and the explanation container/icon are not focusable; the icon is not hit-testable and contains no buttons. | Pass |
| Theme coherence | `AssertSllExplanation` verifies the rendered explanation has non-null background and border resources. The XAML uses `AccentSoftBackgroundBrush`, `AccentSoftBorderBrush`, and `AccentSoftForegroundBrush`, which are defined by the shared Light and Dark theme dictionaries. | Pass |
| Explanation visible during normal Concept work | `SectionVisible_ForConceptStage` and `ExplanationVisible_ForEditableConceptStage` verify the visible box, info icon, exact approved copy, and placement before the Generate action during Concept work. | Pass |
| Explanation remains available when blocked | `ActionsDisabled_WhenSllAiUnavailable`, `BusyIndicator_VisibleWhenBusy`, `ErrorMessage_VisibleWhenSet`, and `StaleMarker_HiddenWhenNoCurrentSll` verify the box remains visible while unavailable, busy, error, and no-current/stale-marker states retain their existing gating or guidance. Existing SLL generation view-model tests cover incomplete, stale-output, reset, and keep transitions. | Pass |
| Explanation accessible without separate interaction | `AssertSllExplanation` verifies one meaningful `AutomationProperties.HelpText` value, a named container, a non-focusable container, a non-focusable/non-clickable icon, and no nested buttons. | Pass |

## Scope and completion gates

The change is limited to the App presentation layer and its deterministic UI tests. No Application, Domain, Integration, persistence, AI, prompt, serialization, or migration behavior changed, so those test layers are not needed for this presentation-only module.

## Commands and results

| Command | Result |
| --- | --- |
| `dotnet test .\\tests\\FusionCanvas.App.Tests\\FusionCanvas.App.Tests.csproj --no-restore --filter FullyQualifiedName~SllSectionHeadlessTests -v minimal` | Pass — 7 passed, 0 failed, 0 skipped. |
| `openspec validate explain-sll --strict` | Pass — change is valid. |
| `dotnet test .\\FusionCanvas.sln -m:1` | Pass — 1,694 passed, 0 failed, 0 skipped across the solution test projects. Existing analyzer warnings remain outside this change. |

Changed-scope review: the XAML adds only a static, non-interactive explanation bound to the existing `ShowsConceptStageTool` visibility state. The test changes are headless UI assertions. No new persistence, external-service, prompt, serialization, security-sensitive, or cross-layer dependency was introduced.
