## 1. Tooling Foundation

- [x] 1.1 Create the warning-clean `tools/FusionCanvas.UiDescription` .NET console project and add it to `FusionCanvas.sln` without adding references to production FusionCanvas projects.
- [x] 1.2 Create the xUnit v3 `tests/FusionCanvas.UiDescription.Tests` project, reference only the tooling project and test dependencies, and add it to the solution baseline.
- [x] 1.3 Add the maintained YAML parser dependency to the tooling project, verify its license and vulnerability status, and record the exact dependency decision in the implementation notes if it differs from the design.
- [x] 1.4 Define stable command exit codes and `UiDiagnostic` identity, severity, location, ordering, and formatting contracts with focused tests.

## 2. Strict YAML Parsing

- [x] 2.1 Add schema-version-1 syntax DTOs for the document, token profile, screen, viewport, component hierarchy, sizing and placement, named states, and state overrides.
- [x] 2.2 Implement `UiDescriptionParser` for one UTF-8 YAML document with source-location capture and explicit rejection of unsupported versions and malformed scalar types.
- [x] 2.3 Reject unknown properties, duplicate mapping keys, aliases, custom tags, and multiple YAML documents with stable parser diagnostics.
- [x] 2.4 Add parser tests for the supported fixture shape, unsupported schema versions, unknown properties with locations, duplicate keys, aliases, custom tags, malformed values, and document multiplicity.

## 3. Semantic Model and Validation

- [x] 3.1 Add immutable validated model types and closed registries for schema-version-1 component kinds, variants, token names, sizing values, alignments, and overridable state properties.
- [x] 3.2 Implement validation for one root, required identities, unique component IDs, container-versus-leaf composition, and allowed child relationships.
- [x] 3.3 Implement validation for finite non-negative fixed sizing, `content` and `fill` compatibility, grid tracks and placements, minimum constraints, and supported alignments.
- [x] 3.4 Implement validation for token-profile identity and compatibility among tokens, component kinds, and variants, with no fallback for unknown values.
- [x] 3.5 Implement named-state validation for unique state names, known component targets, allowed overrides by component kind, and prohibition of structural or variant changes.
- [x] 3.6 Add focused validation tests for duplicate IDs with both locations, invalid composition, invalid placements and sizing, unknown tokens and variants, invalid state targets and overrides, and identical repeated diagnostic ordering.

## 4. Deterministic State Projection and Layout

- [x] 4.1 Implement state lookup and projection that copies the validated base model and applies only supported visibility, enabled, literal-text, and representative-list-item overrides.
- [x] 4.2 Add state-projection tests proving that allowed values change while component identity, kind, variant, ordering, and parent-child structure remain unchanged, and that unknown states fail before output.
- [x] 4.3 Define and test the `fusioncanvas-wireframe-v1` token profile with deterministic spacing, padding, component, typography-approximation, border, and neutral color metrics.
- [x] 4.4 Implement renderer-neutral geometry types and deterministic content measurement using documented invariant component and Unicode-scalar approximation metrics rather than operating-system font measurement.
- [x] 4.5 Implement stack measurement and arrangement for vertical and horizontal axes, named gaps, padding, alignment, fixed, content, fill, and minimum sizing.
- [x] 4.6 Implement grid measurement and arrangement for fixed, content, and equally shared fill tracks, explicit child placement, named gaps, deterministic rounding, and source-order arrangement.
- [x] 4.7 Detect unsatisfied minimums, non-finite geometry, overflow that violates required containment, and other layout failures with component-specific diagnostics instead of silent overlap.
- [x] 4.8 Add layout tests for the declared Design Areas collection panel, calculated flexible editor region, multiple fill tracks, content sizing, minimum failures, deterministic rounding, and viewport containment.

## 5. Canonical SVG Rendering

- [x] 5.1 Implement `SvgWireframeRenderer` for every schema-version-1 container, leaf, and variant using the arranged renderer-neutral tree.
- [x] 5.2 Emit stable `data-ui-id` groups, escaped literal text, representative list rows, visible and enabled presentation, and the renderer-owned wireframe token profile.
- [x] 5.3 Implement canonical UTF-8-without-BOM serialization with LF line endings, invariant numeric formatting, stable declarations, attributes and element order, and no timestamp, machine path, script, or external reference.
- [x] 5.4 Add renderer tests for XML validity, escaping, required semantic identifiers, visibility and enabled states, absence of external dependencies, and byte-identical repeated rendering.

## 6. Repository-Local Commands and Output Safety

- [x] 6.1 Implement strict argument parsing and the documented `validate <source.ui.yaml>` command with standard-error diagnostics, stable exit behavior, and no source mutation.
- [x] 6.2 Implement the documented `render <source.ui.yaml> --state <name> --output <wireframe.svg>` command with explicit paths and state selection.
- [x] 6.3 Implement same-directory temporary output, successful atomic move or replacement, cleanup limited to tool-created temporary files, and preservation of an existing destination on every failure path.
- [x] 6.4 Add command-level tests for successful validation, successful rendering, invalid arguments, parse and semantic failure, unknown state, layout failure, output I/O failure, nonzero exit behavior, no partial output, and unchanged existing destinations.

## 7. Complementary Issue 185 Wireframe Fixtures

- [x] 7.1 Preserve local reference copies of the approved Variant Management v1 and Design Areas v2 PNG wireframes under `docs/Visuals/ui-descriptions/references/` without altering their pixels.
- [x] 7.2 Author `docs/Visuals/ui-descriptions/manage-variants.ui.yaml` with the complete heading, breadcrumb/context, save action, available-choice cards, divider, sellable-variant command row, and tabular records from the approved wireframe.
- [x] 7.3 Author `docs/Visuals/ui-descriptions/manage-design-areas.ui.yaml` with the complete heading, collection cards, add/edit actions, fixed-plus-flexible master-detail regions, focused editor fields, measurement guidance, compatibility action, and save action from the approved wireframe.
- [x] 7.4 Declare and validate a default state for each fixture plus at least one narrow provider-unavailable or empty-collection state without structural overrides.
- [x] 7.5 Add semantic fixture tests for required regions, ordering, stable identifiers, layout roles, table structure, master-detail relationship, action variants, state content, and the absence of absolute child coordinates.
- [x] 7.6 Generate canonical SVGs under `docs/Visuals/ui-descriptions/` exclusively through the documented render command and add byte-for-byte golden-output tests.
- [x] 7.7 Perform and document a side-by-side composition review for each default SVG against its preserved source PNG, covering major regions, ordering, grouping, relative prominence, and list-or-editor relationships.
- [x] 7.8 Add `docs/ui-description-language.md` documenting the vocabulary, exact sizing and state semantics, token profile, diagnostics, exit codes, commands, fixture regeneration and comparison, limitations, and explicit boundary from Avalonia AXAML and application behavior.

## 8. Criterion-Level Verification and Completion QA

- [x] 8.1 Create `verification.md` and map every `ui-description-language` acceptance scenario to its focused automated result, command output, or documented changed-scope inspection evidence.
- [x] 8.2 Run the focused tooling test project and correct implementation or approved artifacts for every failed criterion before recording a pass.
- [x] 8.3 Regenerate every fixture/state SVG twice from clean temporary destinations and record evidence that each repeated result is byte-identical to its checked-in golden file.
- [ ] 8.4 Run `dotnet test .\FusionCanvas.sln` and record the deterministic solution-baseline result; no Avalonia headless or live desktop run is required because production views do not change.
- [x] 8.5 Run strict OpenSpec validation for `prototype-avalonia-ui-description-language` and correct every error or warning that prevents strict acceptance.
- [x] 8.6 Review the final changed-file and project-reference scope and record evidence that no production Avalonia view, view model, command, workflow, persistence model, architecture-layer dependency, or plugin contract changed.
- [x] 8.7 Perform the module learning review, extend `retrospective.md` with observed vocabulary and fidelity lessons, and leave AXAML generation, broader-screen generalization, and durable source-of-truth adoption for later discovery.
