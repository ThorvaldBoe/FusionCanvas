# Mockup Template Coverage Planning Specification

## Purpose

Provide explainable, metadata-first coverage planning for Mockup Templates while preserving the authoritative exact-one source-image readiness policy.

## Requirements

### Requirement: Mockup Template coverage is explainable in the focused editor

The focused Mockup Template editor SHALL expose a coverage summary derived from the authoritative source-image resolution policy. The summary SHALL distinguish resolved, missing, ambiguous, and incomplete source-image conditions for every concrete Variant compatible with the Template's active target Design Area, and SHALL identify affected Variants by creator-facing names and applicable option values.

#### Scenario: Creator opens a Draft Template with missing coverage

- **WHEN** an editable Template has a target Design Area and one or more compatible Variants resolve to no complete source image
- **THEN** the editor shows the resolved and missing counts
- **AND** lists each missing Variant or safely grouped missing coverage requirement
- **AND** provides an action to start image assignment for the affected requirement

#### Scenario: Creator opens a Template with ambiguous coverage

- **WHEN** a compatible Variant resolves to more than one complete source image
- **THEN** the editor identifies the Variant and the conflicting source-image applicability
- **AND** provides guidance to narrow or merge applicability
- **AND** does not imply that the Template is ready

#### Scenario: Creator opens a Template with incomplete source rows

- **WHEN** an active source-image entry lacks applicability or a valid mapping
- **THEN** the editor identifies the incomplete entry and the missing condition
- **AND** keeps the entry editable without fabricating a readiness result

#### Scenario: Coverage becomes complete

- **WHEN** the persisted source-image configuration causes every compatible Variant to resolve to exactly one complete source image
- **THEN** the editor reports complete coverage and Ready for use
- **AND** the existing authoritative eligibility query can expose the Template to Listing

### Requirement: Metadata-first coverage planning creates actionable derived requirements

The focused Mockup Template editor SHALL provide a metadata-first action that derives missing coverage requirements from the selected Template's target Design Area, active compatible Variants, and an explicit grouping strategy. Derived requirements SHALL remain planning rows until a real managed source Asset is assigned and SHALL NOT create empty persisted source-image records.

#### Scenario: Creator generates a default coverage plan

- **WHEN** the creator requests a coverage plan for a Template with an active target Design Area
- **THEN** the editor groups compatible Variants by Color where one Color-only applicability safely covers all grouped Variants
- **AND** leaves Size and other options unrestricted in those groups
- **AND** shows one actionable requirement for each uncovered group

#### Scenario: Color grouping is unsafe

- **WHEN** a Color-only group would combine Variants that cannot share one source-image applicability or would conceal irregular option coverage
- **THEN** the editor splits the plan into the smallest safe option-value groups or individual Variant requirements
- **AND** explains why the finer grouping was used

#### Scenario: Creator chooses a more specific grouping

- **WHEN** the creator selects a grouping strategy such as Color and Size or individual Variant
- **THEN** the plan regenerates its derived requirements using that strategy
- **AND** preserves existing assigned source-image records and does not silently broaden their applicability

#### Scenario: Catalog context changes while planning

- **WHEN** active Variants, Option Values, or the target Design Area change while the editor is open
- **THEN** the editor marks the derived plan stale and requires an explicit refresh before applying new assignments
- **AND** preserves confirmed source-image configuration

### Requirement: Coverage requirements support prefilled image assignment

Each missing coverage requirement SHALL provide an accessible route to upload a new local raster image or assign an existing compatible managed source image. The assignment flow SHALL preselect the requirement's applicability values and Template context, while retaining normal mapping validation and guarded draft behavior.

#### Scenario: Creator uploads an image for a missing Color group

- **WHEN** the creator activates Add mockup image for a missing Color group
- **THEN** the upload flow opens in the current Template context
- **AND** preselects the Color applicability and leaves Size unrestricted when the requirement represents all Sizes
- **AND** selects the new source row for mapping and further metadata editing

#### Scenario: Creator assigns an existing source image

- **WHEN** the creator chooses an existing managed source image compatible with the current Store and Template
- **THEN** the editor applies the requirement's applicability through the existing source-image update path
- **AND** shows the resulting coverage impact before the Template is saved

#### Scenario: Assignment uses an exemplar

- **WHEN** the creator generates requirements from an exemplar source row
- **THEN** the new requirements inherit the exemplar's safe applicability pattern and target Design Area
- **AND** mapping reuse is offered only when image dimensions are compatible or a ratio-aware mapping can be derived
- **AND** incompatible pixel mappings require explicit correction

#### Scenario: Creator saves a partially completed plan

- **WHEN** one or more derived requirements remain unassigned or one or more source rows remain incomplete
- **THEN** the Template remains persistable as a Draft
- **AND** the editor preserves assigned assets and identifies the remaining requirements
- **AND** Listing continues to exclude the Template from ready-only selection

### Requirement: Image-first and metadata-first setup share one coverage authority

Bulk upload, individual upload, existing-image assignment, and metadata-first coverage planning SHALL converge on the same managed source-image entries, applicability conditions, revisions, and authoritative resolution policy. No workflow SHALL weaken exact-one readiness or create an independent compatibility calculation.

#### Scenario: Bulk upload completes a generated plan

- **WHEN** the creator uploads multiple images after generating a coverage plan
- **THEN** the upload workflow can match or assign images to the derived requirements
- **AND** each accepted assignment persists through the existing source-image and revision path
- **AND** the coverage summary updates from the authoritative resolver

#### Scenario: A source image is changed after planning

- **WHEN** the creator changes applicability, mapping, or archive state for an assigned source image
- **THEN** the coverage plan and readiness summary recompute from current persisted configuration
- **AND** prior revisions remain attributable and unchanged

#### Scenario: A Template has no target Design Area

- **WHEN** the creator requests coverage planning without an active target Design Area
- **THEN** the editor explains that a target Design Area is required before compatible Variant requirements can be derived
- **AND** does not create placeholder source-image records
