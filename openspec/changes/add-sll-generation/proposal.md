## Why

A creator with a complete concept in the Concept stage has a balanced Design Triangle (idea, phrase, graphic direction) but no concrete, inspectable layout of the final design. Issue #108 asks for SLL (Sketch Layout Language) generation: turning the complete triangle into a visual ASCII-art sketch of the design. Issue #107 deliberately excluded this and shipped the canonical Design Triangle framework — including a full "Sketch Layout Language" and "Generating SLL" contract — as the non-blocking foundation this module now builds on.

## What Changes

- Add a new SLL generation section inside the Concept stage surface, below the existing "Refine with AI" section, that becomes available only when the design triangle is complete (deterministic completeness score of 100).
- Generate a full minimal SLL artifact from the triangle and creative context, following the framework's minimal command semantics: assumptions (if any), communication intent, normalized Design Triangle, one plain-ASCII composition sketch, execution notes, and validation with the largest risk.
- Persist the generated SLL with the item so it survives reopen, committed through the existing Concept-stage automatic-save draft path.
- Support a single generate action with regeneration (replacing the current SLL), gated on SLL-purpose AI availability, with single-operation concurrency and cancellation mirroring Concept refinement.
- Add a new `AiRequestPurpose.Sll` AI purpose with its own profile + availability gating and a corresponding AI settings editor tab, mirroring the existing Concept purpose pattern.

## Capabilities

### New Capabilities

- `sll-generation`: AI-assisted generation of a Sketch Layout Language (SLL) artifact from a complete Concept-stage Design Triangle — availability gating, a minimal SLL output contract, persistence to the item, and single generate + regenerate behavior.

### Modified Capabilities

- `concept-refinement`: Extend the Concept stage surface with the SLL generation section (rendered below the refinement section, same stage visibility). No existing refinement behavior changes.

## Impact

- **Domain:** The SLL artifact is a structured document (title, version, assumptions, product, communication, triangle, composition, ASCII sketch, notes, validation). A serialization/parsing rule for the SLL document lives in the Domain/Application boundary and is fully testable without frameworks. No change to `DesignTriangleScore`.
- **Application:** New `ISllGenerationService` (+ `SllGenerationService`) and `ISllAccessStatus` (+ `ConfiguredSllAccessStatus`) shipped beside the Concept refinement siblings. The full-minimal SLL output is parsed from the AI response into a small SLL document model. Reuses `IDesignTriangleGuidanceSource` (the canonical `PoDDesignFramework.md`) and the repository for creative context and persistence.
- **AI purpose:** Add `AiRequestPurpose.Sll`, a `Sll` profile to `AiConfigurationSettings`/`AiPurposeProfileSettings`, a resolver branch, and an AI-settings editor + readiness surface mirroring the Concept purpose.
- **Integration:** No SQLite schema or migration. The SLL is stored as an item metadata field, committed through the existing Concept-stage automatic-save path.
- **App:** New `SllGenerationSessionViewModel`, a section in `MainWindow.axaml`, and wiring in `AppWorkspaceFactory`/`AppWorkspaceRuntime` and `MainWindowViewModel`, all mirroring the Concept refinement patterns.
- **Tests:** Domain serialization/validation tests; Application prompt-assembly + parse + persistence tests with deterministic collaborators; Avalonia headless view tests for the new section's state; Integration persistence coverage. No external-service tests.
- **Non-goals:** Editing the sketch/notes in-place, multiple persisted SLL variants, image generation, print-provider-specific precision, exposing the internal layout model, and a visible UI for the framework document. Regeneration replaces the current SLL rather than appending variants.

## Why this scope is reviewable

The module is one cohesive outcome — "turn a complete triangle into a persisted, regenerable ASCII SLL" — and every element either reuses an established pattern (Concept refinement) or the canonical framework contract. It has no dependency on other in-flight capabilities, is independently verifiable at each layer, and deliberately defers richer SLL editing and variant management to later modules.
