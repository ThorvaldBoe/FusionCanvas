## Why

Issue #107 calls for Idea and Concept AI assistance that is grounded in FusionCanvas's Design Triangle theory rather than producing generic creative copy. The formal framework is available as four canonical documents plus its README, while the application currently ships only a short Concept-only placeholder and Ideation does not receive the theory at all.

## What Changes

- Replace the placeholder embedded Design Triangle guidance with one canonical, combined Markdown framework asset assembled from the supplied canonical documents in their declared order.
- Make the framework available through an application-facing read-only source so both Ideation and Concept refinement use the same runtime content without exposing it in the UI or persisting it with workspace data.
- Make both Ideation modes request concise Idea-stage output that respects the framework's social-meaning, audience-recognition, and wearer-signal principles, while retaining their existing Basic and Snowclone response contracts.
- Strengthen Concept initialization and per-corner refinement prompts to use the complete framework while retaining their existing strict response formats and current-triangle semantics.
- Add deterministic prompt-assembly and embedded-resource coverage, including proof that operational/secret data remains excluded.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `ideation`: Both Basic and Snowclones AI requests use the bundled Design Triangle framework and preserve their concise candidate contracts.
- `concept-refinement`: The bundled runtime guidance becomes the canonical Design Triangle framework and is used by every Concept AI request.

## Impact

- **Application:** Share the guidance-source dependency between `AiIdeaGenerator` and `ConceptRefinementService`; prompt instructions distinguish Idea-stage generation from Concept-stage triangle work.
- **Integration:** Replace `AI/DesignTriangleGuidance.md` with the combined embedded framework asset and continue providing it through the Integration implementation of the Application contract.
- **App composition:** Supply the one guidance source instance to both AI workflows. No new settings, persistence, dialog, or visible UI are introduced; the UX preflight is therefore not applicable beyond preserving existing flows and action states.
- **Tests:** Extend Application prompt-capture tests and Integration embedded-resource tests. No Domain model, SQLite schema, workspace/export format, network contract, or migration changes are required.
- **Dependencies and scope:** This change depends on the production `AiIdeaGenerator` introduced by the active `integrate-ideation-openrouter-snowclones` change. It deliberately excludes SLL generation, ASCII sketches, image generation, visual layout UI, framework editing, prompt/response persistence, and new AI providers. The shared full framework asset is the intentional non-blocking foundation for those later features.
- **Risks:** The full framework is materially larger than the placeholder, so prompts must clearly state their narrow output contract; user-authored context remains data-delimited and must never become system authority. Verification will assert both framework inclusion and unchanged output/secret-safety rules.
