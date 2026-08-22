## Why

FusionCanvas UI concepts currently move through independently authored wireframes, HTML or image mockups, and Avalonia AXAML, so implementation can silently reinterpret hierarchy, sizing, alignment, component variants, and interaction states. A small machine-readable UI description prototype will test whether one structural source can produce a repeatable wireframe and remove design judgment from the wireframe-to-layout handoff before the project invests in AXAML generation or a broader design tool.

## What Changes

- Introduce a versioned, human-readable YAML format for describing bounded Avalonia-oriented screens through semantic design tokens, component roles, hierarchy, layout constraints, representative content, and named interaction states.
- Define an initial vocabulary only for the controls and patterns needed to describe the approved Issue #185 Variant Management and Design Areas wireframes as complementary representative fixtures.
- Add schema validation with actionable diagnostics for unsupported versions, unknown component kinds or tokens, duplicate identifiers, invalid sizing rules, missing references, and structurally invalid state overrides.
- Add a deterministic renderer that converts a valid description and named state into a standalone SVG wireframe without AI interpretation, network access, or an interactive desktop.
- Add checked-in descriptions, source-wireframe references, and deterministic expected SVG outputs for the Variant Management and Design Areas default states, plus narrow alternate-state examples that exercise state projection without changing hierarchy.
- Document the prototype vocabulary, authoring rules, renderer command, known limitations, and the boundary between semantic UI structure and application behavior.
- Keep production Avalonia views and accepted end-user workflows unchanged during this spike.

The module depends only on the repository's existing .NET toolchain, current FusionCanvas UI guidance, and the user-approved Issue #185 wireframes. It is cohesive because the format, validator, renderer, fixtures, and tests together answer one question: can a single constrained description express distinct real FusionCanvas screen compositions precisely enough to regenerate recognizable, repeatable wireframes?

Non-goals are production AXAML generation, runtime UI loading, view-model or command generation, changing the Issue #185 production screens, pixel-perfect visual regression, arbitrary Avalonia control support, responsive breakpoint design, a graphical editor, and migration of additional screens.

Primary risks are accidentally creating a second general-purpose UI framework, overfitting the vocabulary to one layout family, treating SVG similarity as proof of production Avalonia fidelity, and allowing ambiguous defaults that reintroduce interpretation. The spike limits those risks through two complementary fixtures, a deliberately small vocabulary, explicit layout semantics, deterministic golden-output tests, structural assertions, documented visual comparison, and deferred capabilities.

Verification will use focused parser, semantic-validation, layout, and renderer tests; byte-stable or canonically stable SVG output for repeated identical inputs; fixture-state assertions; strict OpenSpec validation; and the repository solution test baseline. UX preflight is not applicable because this change adds developer tooling and documentation without changing a user-facing FusionCanvas workflow.

## Capabilities

### New Capabilities

- `ui-description-language`: Defines the prototype source format, validation behavior, deterministic wireframe rendering, representative fixture states, and developer-facing command contract.

### Modified Capabilities

None.

## Impact

- Adds repository-local developer tooling and focused automated tests, likely under a new `tools/` capability and matching test project or test folder.
- Adds two `.ui.yaml` sources, preserved reference-wireframe images, generated SVG fixtures, comparison evidence, and authoring documentation; generated artifacts must remain clearly identified and reproducible.
- May introduce one narrowly scoped YAML parsing dependency if the implementation review confirms that using a maintained parser is safer than a custom subset parser.
- Does not change `FusionCanvas.Domain`, `FusionCanvas.Application`, `FusionCanvas.Integration`, production persistence, production Avalonia views, or public plugin contracts.
- Establishes evidence for a later, separately proposed decision about Avalonia AXAML generation; it does not pre-approve that later capability.
