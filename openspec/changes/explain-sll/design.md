## Context

The Concept stage already renders the SLL generation section below Concept refinement. It contains the Generate and Regenerate actions, unavailable/error guidance, busy and stale states, and the read-only generated sketch. The IssueTracker decision resolves the missing explanatory affordance: an information box with an info icon, explanatory text, and a separate background color.

This is a presentation-only module. It must help a creator understand SLL before using the existing generation actions without adding a second help surface, changing SLL data, or moving the primary Concept workflow into a dialog. The explanation is relevant during both editable work and read-only review, so its visibility is independent of AI availability, triangle completeness, current SLL presence, and editability. The approved copy is used verbatim, including its current spelling; copy editing is outside this change.

## Goals / Non-Goals

**Goals:**

- Place one compact information box immediately below the existing SLL heading and before availability/error guidance and actions.
- Show an info icon and the approved explanation in a visually distinct, theme-aware background.
- Keep the explanation visible whenever the Concept-stage SLL section is visible, including read-only review and unavailable/empty states.
- Make the box and icon discoverable to keyboard and assistive-technology users without adding a focusable action.
- Preserve the existing SLL action order, gating, stale handling, persistence, and AI behavior.

**Non-Goals:**

- No tooltip-only replacement, expandable panel, modal, navigation, or link to the bundled framework document.
- No changes to SLL generation prompts, AI availability, completeness scoring, serialization, persistence, or stale-SLL decisions.
- No new Domain, Application, Integration, or persistence abstractions.
- No copy-editing or terminology expansion beyond the approved sentence.

## Decisions

### 1. Use an inline information box in the existing SLL section

The explanation belongs beside the SLL actions in the primary Concept workspace because it is a lightweight, occasional orientation aid used when a creator encounters the term. It should not consume a permanent navigation surface or open a focused editor. An inline box remains visible while the creator decides whether to generate, regenerate, reset, or keep an SLL.

**Alternative considered:** a tooltip on the “Generate SLL sketch” heading or button. Rejected because tooltip content is discoverable only through pointer/hover behavior and is less reliable for keyboard and assistive-technology users.

**Alternative considered:** an expandable information panel. Rejected because the approved explanation is one sentence and does not justify another disclosure state or focus transition.

### 2. Keep the information box presentation-only and always visible with the SLL section

The box has no command, selection, draft, unsaved state, cancellation, or destructive action. Its visibility follows `ShowsConceptStageTool` exactly, not `CanGenerate`, `CanRegenerate`, `IsBusy`, `IsStale`, or AI availability. Existing blocked-state guidance remains separate and keeps ownership of actionable prerequisites.

### 3. Use shared theme resources and an accessible static description

The box uses the existing surface, border, primary-text, and secondary-text theme resources rather than hard-coded colors. The info glyph is decorative reinforcement for the adjacent text but receives an accessible name/description through the surrounding semantic container so screen readers announce the purpose and explanation once. The box is not focusable because it has no action.

### 4. Preserve the approved copy exactly

The rendered text is: “SLL stands for Symbolic Layout Languate. It's a rough visual sketch of the proposed design.” The spelling is an explicit IssueTracker decision for this module. A later copy-edit may change it through a separate reviewed change.

## Risks / Trade-offs

- **[Risk]** A static box adds vertical height to the Concept surface. → Keep it compact, place it directly under the heading, and avoid duplicating existing disabled-state guidance.
- **[Risk]** An icon may be announced twice or become an accidental tab stop. → Use a non-focusable static glyph and one accessible name/description on the containing information element; verify the visual tree and automation properties in headless tests.
- **[Risk]** Hard-coded colors could reduce contrast in one theme. → Use the existing shared theme resources and test both theme-resource bindings and visible text/icon content.
- **[Risk]** Future copy edits may be mistaken for implementation drift. → Record the approved sentence as a fixed acceptance value and keep copy changes out of this module.

## Migration Plan

No data or schema migration is required. Deployment is a view-only update. Rollback removes the information box and leaves all SLL state and persisted documents unchanged.

## Open Questions

None. The IssueTracker decision fixes the form, placement, visual distinction, and copy for this module.

## Implementation Plan

1. Update `src/FusionCanvas.App/Views/MainWindow.axaml` in the existing SLL generation section. Add one `Border` or equivalent compact container after the “Generate SLL sketch” heading and before unavailable/error guidance, with a non-focusable info glyph and a `TextBlock` containing the approved sentence.
2. Bind the container visibility to the same Concept-stage visibility condition as the SLL section. Use existing `SubtleSurfaceBrush`/`ElevatedSurfaceBrush`, `ControlBorderBrush`, `PrimaryTextBrush`/`SecondaryTextBrush`, or the closest existing shared resources; do not introduce a new color token unless review shows the current palette cannot provide sufficient distinction.
3. Add explicit automation properties to the container and/or text so the information is exposed as one meaningful static explanation. Do not add a click handler, command, view-model property, persistence field, or focus target.
4. Add or extend `tests/FusionCanvas.App.Tests/SllSectionHeadlessTests.cs` to verify the box appears in editable and read-only Concept surfaces, contains the approved copy and info affordance, exposes an accessible name/description, uses a distinct themed surface, and does not change existing Generate/Regenerate control ordering or enabled-state behavior.
5. Run the focused SLL view-model/headless tests, strict OpenSpec validation, and the full `dotnet test .\FusionCanvas.sln -m:1` baseline. Record criterion-level results in `verification.md`.

### Planned acceptance verification

| Acceptance scenario | Planned verification |
| --- | --- |
| Explanation is visible in editable Concept stage | `SllSectionHeadlessTests` constructs the normal Concept surface and asserts the information box, icon, and approved copy are visible before the SLL actions. |
| Explanation remains visible when SLL generation is unavailable or read-only | Headless cases with incomplete/unavailable SLL state and a read-only Concept review assert the box remains visible while existing actions retain their current disabled behavior. |
| Explanation uses distinct shared theme styling | Headless visual-tree/resource assertions verify the information container has a non-default themed background/border and remains present after theme setup. |
| Explanation is accessible without adding a new interaction | Headless automation assertions verify one meaningful accessible description/name, the info glyph is not a separate focus target, and existing SLL action order is unchanged. |
| Existing SLL behavior is unchanged | Existing `SllGenerationSessionViewModelTests` and `SllSectionHeadlessTests`, plus the full solution baseline, continue to pass without new AI or persistence calls. |

### Decisions not to reopen during implementation

- Use one inline information box with an info icon and distinct background.
- Place it immediately below the existing SLL heading in the Concept stage.
- Use the approved sentence verbatim, including current spelling.
- Keep it static and visible in all states where the SLL section is visible.
- Do not alter SLL generation, persistence, AI requests, serialization, or existing disabled-state guidance.
