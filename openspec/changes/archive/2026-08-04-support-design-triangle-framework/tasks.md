## 1. Canonical Embedded Framework

- [x] 1.1 Combine the supplied README and four canonical PoD Design Framework Markdown documents into the ordered `PoDDesignFramework.md` Integration asset, replacing the placeholder guidance resource without including historical `Sources` material.
- [x] 1.2 Update the Integration project resource declaration and embedded source implementation so the Application-facing guidance contract loads the canonical asset deterministically.
- [x] 1.3 Extend Integration tests to prove the runtime asset is non-empty and contains the canonical social-meaning, Design Triangle, Design Pyramid, SLL, and SLL-generation sections.
- [x] 1.4 Preserve canonical UTF-8 text while composing the framework asset and add regression coverage for representative typographic punctuation and absent mojibake markers.

## 2. Framework-Aware AI Requests

- [x] 2.1 Inject the existing Application guidance-source contract into `AiIdeaGenerator` and emit the shared framework plus non-negotiable mode-specific output rules as a System message while retaining creative context as serialized User data.
- [x] 2.2 Implement Basic-mode instructions for one concise, socially meaningful Idea direction and prohibit full Concept, design-specification, and SLL output.
- [x] 2.3 Implement Snowclones-mode instructions that preserve the completed-phrase-only and placeholder contracts while favoring audience-relevant identity, experience, attitude, or tension.
- [x] 2.4 Strengthen Concept initialization and per-corner refinement system instructions to use the canonical framework's social proposition, carrier relationship, and graphic-role tests without changing parsing or action semantics.
- [x] 2.5 Update application composition and affected fixtures/call sites to share one embedded framework source between Ideation and Concept refinement.

## 3. Focused Automated Coverage

- [x] 3.1 Extend `AiIdeaGeneratorTests` for system/user role separation, canonical-framework inclusion, Basic and Snowclone output contracts, empty guidance, and secret/operational-field exclusion.
- [x] 3.2 Extend `ConceptRefinementServiceTests` for canonical-framework inclusion and framework-aware Initialize, Fine tune, and Change instructions while preserving existing result contracts and sanitization coverage.
- [x] 3.3 Confirm no Avalonia headless view test is required because the change does not alter markup, bindings, controls, focus, or any user-visible UI state; retain existing UI regression coverage through the solution baseline.

## 4. Completion Verification

- [x] 4.1 Run targeted Integration and Application test projects; correct every failed scenario and record criterion-level evidence in `verification.md`.
- [x] 4.2 Run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; record results, warnings, and any environment limitation in `verification.md`.
- [x] 4.3 Run `openspec validate support-design-triangle-framework --strict` and `openspec validate --all --strict`; resolve package defects.
- [x] 4.4 Perform changed-scope architecture, prompt-injection/security, source-asset, specification-drift, and future-SLL non-scope review before requesting human acceptance.
