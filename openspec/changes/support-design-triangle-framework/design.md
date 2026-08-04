## Context

FusionCanvas currently embeds a short `DesignTriangleGuidance.md` resource in Integration. `ConceptRefinementService` receives it through the Application-owned `IDesignTriangleGuidanceSource`, but production Ideation's `AiIdeaGenerator` has no such dependency and puts all instructions plus user-authored creative data in one user message. The supplied canonical framework comprises the README plus four Markdown pages; its later SLL material is needed as theory/context now but SLL generation is explicitly future work.

This is an AI prompt-behavior change, not a new UI workflow. Creators continue using the same frequent Ideation dialog and inline Concept refinement actions; no new control, focus transition, draft state, confirmation, loading state, or workspace footprint is introduced. Existing busy, unavailable, cancellation, and recoverable-error behavior remains authoritative.

## Goals / Non-Goals

**Goals:**

- Ship one code-addressable embedded canonical PoD Design Framework resource.
- Use the same framework source in both Ideation modes and every Concept refinement request.
- Keep Idea outputs concise and Concept outputs parseable while making their requested reasoning framework-aware.
- Preserve secret-safe, user-content-as-data prompt boundaries and existing AI purpose/profile routing.
- Leave a reusable source and clear theory boundary for later SLL or visual-sketch generation.

**Non-Goals:**

- Generating, storing, editing, or displaying SLL sketches; building a visual sketch generator; image generation; or changing the Design stage UI.
- Adding AI settings, new provider requests, structured output, prompt/response persistence, or a database/file-format migration.
- Altering candidate creation/rejection, snowclone selection, Concept result parsing, completeness, history, or action availability.

## Decisions

### D1: One full embedded framework asset replaces the placeholder

Create `src/FusionCanvas.Integration/AI/PoDDesignFramework.md` by combining the supplied README and four canonical pages in the README's order, retaining headings and source material as Markdown. The Integration project embeds it under a stable logical resource name, and its `EmbeddedDesignTriangleGuidanceSource` loads it. The existing Application contract remains the focused, testable boundary because it already represents a real Integration-to-Application resource boundary.

Keep the present contract/type name for this delivery module to minimize unrelated churn. A later SLL generator may reuse it or rename/generalize it in its own change if a broader API becomes necessary.

Alternative: duplicate a shortened framework into each prompt builder. Rejected because it creates two sources of truth and loses the supplied formal theory. Alternative: load the source documents from an external path at runtime. Rejected because shipping must be local, deterministic, and independent of a contributor machine.

### D2: System authority holds framework and output rules; user content stays delimited data

`AiIdeaGenerator` gains `IDesignTriangleGuidanceSource` through constructor injection. It emits a system message containing the framework and mode-specific non-negotiable output contract, then preserves its serialized `<creative-context>` user message for user-authored guidance, metadata, ideas, rejections, and Snowclone guidance. The framework directs reasoning; it cannot cause user context to override response rules.

`ConceptRefinementService` retains its system-message placement, but replaces the placeholder resource content and adds explicit framework-aware evaluation instructions that do not change its exact labeled Initialize response or single-value refinement response contracts.

Alternative: attach the full framework to each user message. Rejected because user content and system instruction would remain conflated and it weakens the existing prompt-injection boundary.

### D3: Framework-aware instructions are stage-specific

Basic Ideation asks for one concise working Idea, including a social proposition: subject/situation, wearer signal, intended viewer inference or effect, and shared audience context. Snowclones still asks for only the completed phrase, but uses the framework to favor meaningful identity/experience/attitude/tension. Neither mode creates a Concept triangle, Design Pyramid realization, or SLL.

Concept initialization asks the model to derive a coherent triangle, and per-corner actions ask it to preserve or deliberately revise the triangle's social proposition, phrase/graphic relationship (reinforcement, completion, or contrast), and graphic semantic role. Existing parsers and action behavior remain unchanged.

### D4: No UI or persistence changes

The framework is an app-shipped internal asset. Do not expose a viewer, editor, setting, navigation item, workspace asset record, or prompt-history record. No user-visible state changes are caused by loading the resource; a missing embedded resource remains a deterministic Integration failure covered by the source implementation/tests.

## Risks / Trade-offs

- [Full framework consumes more prompt tokens] → Include it exactly once per request as a system message, keep output rules concise, and retain one-request-per-existing-operation behavior.
- [Theory could make Idea output too detailed] → Explicitly prohibit full Concept, design-specification, and SLL output; assert this instruction in prompt-capture tests.
- [User guidance or snowclone text attempts to override instructions] → Keep all user-authored material in serialized, delimited user context and retain existing metadata sanitization.
- [Future SLL scope leaks into this module] → Embed the canonical SLL theory now but add no SLL domain types, persistence, UI, or generation endpoint.
- [Concurrent active Ideation integration change overlaps implementation] → Treat its production `AiIdeaGenerator` implementation as a prerequisite; rebase/reconcile implementation only after its artifacts/code are accepted.

## Migration Plan

No persisted data changes. Deployment replaces the embedded resource content and changes in-memory request assembly only. Rollback restores the prior resource and prompt constructors; no workspace migration or cleanup is required.

## Open Questions

None. The supplied README identifies the four canonical pages and their ordering; historical source documents are excluded.

## Implementation Plan

1. **Integration framework asset** — Add `src/FusionCanvas.Integration/AI/PoDDesignFramework.md` containing the canonical README and four files in order; update `FusionCanvas.Integration.csproj` embedded-resource path/logical name and `EmbeddedDesignTriangleGuidanceSource` constant if needed. Remove the replaced placeholder resource only as part of this bounded asset replacement. Extend `EmbeddedDesignTriangleGuidanceSourceTests` to assert the full canonical section set and non-empty load.
2. **Application Ideation prompt boundary** — Update `src/FusionCanvas.Application/Ideation/AiIdeaGenerator.cs` to depend on `IDesignTriangleGuidanceSource`, place framework/output authority in an `AiMessageRole.System` message, and retain serialized creative context in the User message. Add Basic/Snowclone framework-aware mode instructions without altering `IdeaGenerationResult`, purposes, response translation, retry behavior, or snowclone placeholder validation.
3. **Application Concept prompts** — Update `ConceptRefinementService` system instructions only, preserving the source dependency, user context serialization/sanitization, requests, parsers, result types, and cancellation behavior. Make instructions explicitly testable for Initialize, Fine tune, and Change semantics.
4. **Composition** — Update `AppWorkspaceFactory` to construct one embedded source and pass it to both `AiIdeaGenerator` and `ConceptRefinementService`; update constructor fixtures/call sites in tests. No App visual-tree change is expected, so an Avalonia headless test is not applicable; framework behavior is entirely below the UI boundary.
5. **Verification** — Extend `AiIdeaGeneratorTests` with captured system/user message assertions for both modes, system/user role separation, canonical framework markers, compact stage-specific contracts, and operational-field exclusion. Extend Concept service tests for canonical guidance and action-specific instructions. Run targeted Application/Integration tests, `dotnet build .\FusionCanvas.sln`, `dotnet test .\FusionCanvas.sln`, and strict change/all OpenSpec validation. Record every delta-spec scenario in `verification.md` during implementation.

**Decisions not to reopen:** retain the existing guidance-source contract name; bundle the complete canonical framework rather than a prompt-specific summary; do not expose it in the UI; use it as system authority; and keep SLL generation deferred.

## Planned Acceptance Verification

| Acceptance scenario | Verification method |
| --- | --- |
| Basic request uses concise framework-aware idea contract | Capturing `AiIdeaGeneratorTests` asserts system framework markers and Basic-only output prohibitions alongside preserved user context |
| Basic works with empty guidance | Application test captures an empty-guidance request and asserts framework plus resolved context remain present |
| Snowclone request uses framework but returns phrase-only | Capturing generator test asserts framework markers, phrase-only contract, template context, and unresolved-placeholder behavior remains covered |
| Concept Initialize/Fine tune/Change use canonical framework | `ConceptRefinementServiceTests` capture each request and assert framework markers, action-aware quality instructions, existing role/result format, and context |
| Secrets/operational data remain excluded | Existing adversarial metadata tests extended across both call paths; request role separation test confirms user content is not system authority |
| Framework is embedded and no UI is added | Integration resource test asserts all canonical headings; App markup/composition diff plus existing UI tests establish no visible control/state change (headless view test not applicable) |
