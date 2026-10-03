## Context

Iteration 1 added `OfferingReadinessSummary` in `FusionCanvas.Application.Catalog`, derived from active normalized Variants, Design Areas, Mockup Templates, and the authoritative Mockup Template readiness evaluator. Design already loads normalized Offering and Design Area projections but currently exposes only the selected name/provider and separate artwork-generation guidance.

## Goals / Non-Goals

**Goals:**

- Make Store setup blockers visible in Design at the moment they matter.
- Use exactly the same derived readiness and blocker evidence as Store Management.
- Keep the information read-only, current-on-load, and scoped to the selected normalized Offering.
- Preserve the existing artwork-generation readiness policy and stale-configuration recovery.

**Non-Goals:**

- No automatic repair, navigation command, dialog, AI recommendation, Printify sync, or catalog mutation.
- No change to which configurations, colors, design files, or Listing templates are eligible.
- No readiness claim about Item-specific selected colors, artwork files, or Listing outputs.
- No persistence changes.

## Decisions

### 1. Extend the existing Design load projection

`DesignStageState` will carry an optional `OfferingReadinessSummary?`. `DesignStageService.BuildState` will calculate it only when the selected configuration resolves to an active normalized `BlueprintOffering`; legacy-only configurations continue to use the existing Design state unchanged.

### 2. Reuse, do not duplicate, readiness rules

The service will call the existing `OfferingReadinessBuilder`. The App will reuse the existing `OfferingReadinessMessageTranslator` so the same issue kinds and template blockers have the same wording in Store and Design.

### 3. Keep Design-specific readiness separate

The new catalog status is shown alongside, not substituted for, `ArtworkGenerationGuidance`. “Ready for mockup generation” never means that the Item has selected Colors or assigned artwork.

### 4. Progressive disclosure and read-only safety

The Design surface will show a compact status and ready-template count, then an issue list only when blockers exist. The panel has no commands or editable controls, so read-only or protected Items cannot accidentally mutate catalog setup.

## Risks / Trade-offs

- **Legacy projection ambiguity:** Legacy-only Offerings cannot be evaluated by the normalized builder. The optional state remains absent and existing Design guidance remains authoritative.
- **Stale data:** The projection is rebuilt with every Design load and is not persisted; it cannot become a stored readiness flag.
- **Terminology confusion:** Copy will explicitly say catalog/mockup setup and retain the per-Item disclaimer.

## Implementation Plan

1. Add the optional `OfferingReadinessSummary` property to `DesignStageState`.
2. In `DesignStageService.BuildState`, resolve the selected active normalized Offering and call `OfferingReadinessBuilder` without saving or changing the existing state construction.
3. Expose summary text, ready-template count, issue guidance, and visibility properties on `DesignStageToolViewModel`; preserve null/legacy fallback wording.
4. Move the existing readiness translator to an App-shared namespace if needed, then bind a compact panel in `MainWindow.axaml` before the Design color working set. Include an automation id and accessible name.
5. Add framework-free Design state/presentation tests for incomplete, ready, and legacy/read-only cases.
6. Extend deterministic Avalonia headless coverage to assert the panel and guidance are visible without changing existing Design controls.
7. Run focused tests, strict OpenSpec validation, and the solution baseline; record any existing test-host limitation in `verification.md`.

## Acceptance-to-Verification Mapping

| Acceptance scenario | Planned verification |
| --- | --- |
| Design shows incomplete normalized Offering blockers | Design application state test and headless binding test |
| Design shows ready-template count without claiming Item readiness | Design state/presentation test |
| Legacy or unavailable configuration preserves existing behavior | Design service regression test |
| Read-only Design does not gain mutation controls | Existing protected-item test plus headless panel inspection |
| Existing artwork readiness and slot workflow remain unchanged | Existing Design stage test suite and solution baseline |
