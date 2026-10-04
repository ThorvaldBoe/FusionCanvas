## ADDED Requirements

### Requirement: Customer-facing content carries persistent risk awareness
FusionCanvas SHALL associate customer-facing AI-assisted content and customer-facing artwork assets with a persistent advisory content-risk review state. The default state SHALL be `Unreviewed`, SHALL show an awareness warning, and SHALL NOT be presented as safe, approved, legally cleared, or infringement-free.

#### Scenario: AI-generated customer-facing text is applied
- **WHEN** AI-generated text is accepted into a customer-facing Item field such as a phrase or title
- **THEN** FusionCanvas creates or updates an `Unreviewed` content-risk state for that field
- **AND** the field's surface shows the default IP, safety, and marketplace-awareness warning

#### Scenario: Generated artwork is assigned to a customer-facing Design slot
- **WHEN** generated artwork is assigned to a Design slot that can be used in a customer-facing product
- **THEN** FusionCanvas associates the artwork Asset with a content-risk state before or as part of the assignment
- **AND** the Design surface shows the warning until a review result is available

#### Scenario: Customer-facing uploaded artwork is imported
- **WHEN** a user imports or assigns artwork that can be used in a customer-facing Design slot
- **THEN** FusionCanvas associates the artwork Asset with a content-risk state
- **AND** the operation remains available when review is unavailable because review is advisory rather than an automatic block

#### Scenario: Private reference material is not customer-facing
- **WHEN** a user imports an asset whose purpose remains private reference material
- **THEN** FusionCanvas does not present it as customer-facing content-risk review
- **AND** promoting or assigning that asset to a customer-facing destination creates the applicable review state

### Requirement: Review findings distinguish IP, safety, and marketplace suitability
FusionCanvas SHALL represent advisory findings in separate categories for intellectual-property risk, harmful or inappropriate content, and marketplace suitability. A finding SHALL explain that it is a signal for human review rather than a legal or platform determination.

#### Scenario: IP signal is detected
- **WHEN** a configured analyzer identifies a possible brand, logo, protected character, franchise, copyrighted work, or confusingly similar reference
- **THEN** FusionCanvas records an IP finding with a bounded explanation
- **AND** the UI presents it separately from safety and marketplace findings

#### Scenario: Harmful-content signal is detected
- **WHEN** a configured safety analyzer identifies possible sexual, hateful, harassing, violent, self-harm, illicit, or otherwise harmful content
- **THEN** FusionCanvas records a safety finding with the detected category
- **AND** the UI presents a clear warning without claiming that the analyzer exhaustively determines marketplace policy compliance

#### Scenario: No signal is detected
- **WHEN** an analyzer completes without finding an obvious signal
- **THEN** FusionCanvas records `NoObviousSignalDetected`
- **AND** the UI continues to show the limitation that the result is not clearance

### Requirement: Content-risk review is attempted at customer-facing application boundaries
FusionCanvas SHALL review customer-facing AI-assisted text when it is applied to a relevant field and SHALL review generated or uploaded artwork when it becomes eligible for a customer-facing Design destination. Working-only content MAY remain unreviewed until it crosses such a boundary.

#### Scenario: Concept refinement produces a phrase or graphic direction
- **WHEN** an AI Concept refinement action applies a Phrase or Graphic direction that can influence customer-facing artwork
- **THEN** FusionCanvas creates a review state for the applied value
- **AND** a failed or unavailable review does not silently remove the awareness warning

#### Scenario: Title optimization produces a title
- **WHEN** AI title optimization applies a title used for customer-facing listing preparation
- **THEN** FusionCanvas creates or refreshes a review state for the title
- **AND** the title remains editable while the warning is visible

#### Scenario: Artwork review occurs after generation or import
- **WHEN** artwork generation or customer-facing artwork import completes
- **THEN** FusionCanvas attempts the configured review using the final managed artwork representation
- **AND** no provider-original or temporary payload is treated as the authoritative reviewed asset

### Requirement: Review availability and failure remain advisory and visible
FusionCanvas SHALL preserve the customer-facing warning when no analyzer is configured, an analyzer cannot run, the provider refuses the request, the request times out, or the result is malformed. Such conditions SHALL produce `ReviewUnavailable` or an equivalent recoverable state and SHALL NOT be interpreted as a negative result.

#### Scenario: No review provider is configured
- **WHEN** customer-facing content reaches a review boundary without an available analyzer
- **THEN** FusionCanvas persists `ReviewUnavailable`
- **AND** the content remains usable with the default awareness warning

#### Scenario: Review provider fails
- **WHEN** a configured analyzer fails, is cancelled, or returns an invalid result
- **THEN** FusionCanvas keeps the content operation's confirmed state intact
- **AND** reports that review could not be completed while retaining the warning

#### Scenario: Review result is advisory
- **WHEN** a user sees one or more review findings
- **THEN** FusionCanvas lets the user inspect the findings and continue working
- **AND** does not automatically block, delete, reject, publish, or label the content as legally cleared

### Requirement: Reviews are fingerprinted and become stale after content changes
FusionCanvas SHALL persist a non-secret content fingerprint with each review and SHALL treat a review as stale when the reviewed text, image bytes, or relevant customer-facing destination changes.

#### Scenario: Reviewed text changes
- **WHEN** a user edits a reviewed customer-facing text value
- **THEN** the prior result no longer represents the current value
- **AND** the state returns to `Unreviewed` with the awareness warning

#### Scenario: Reviewed artwork changes
- **WHEN** a new artwork Asset replaces the reviewed artwork in a customer-facing slot
- **THEN** the new Asset has its own review state
- **AND** the replaced Asset's review is not reused for the new bytes

#### Scenario: Workspace reload restores review state
- **WHEN** the workspace is closed and reopened after a review attempt
- **THEN** the warning, state, findings, fingerprint, and review timestamp reload with the associated content

### Requirement: Review providers receive bounded and privacy-aware inputs
FusionCanvas SHALL send only the content and minimal review context required by the selected analyzer, SHALL honor the active AI privacy policy where the analyzer uses an external provider, and SHALL exclude credentials, file-system paths, database identifiers, and unrelated operational metadata from analyzer prompts, diagnostics, and persisted findings.

#### Scenario: Text review request is assembled
- **WHEN** customer-facing text is sent to an external analyzer
- **THEN** the request contains the text and review instructions needed to classify IP, safety, and suitability signals
- **AND** it excludes unrelated workspace identity and operational fields

#### Scenario: Image review request is assembled
- **WHEN** customer-facing artwork is sent to an image-capable analyzer
- **THEN** the request contains the final managed image bytes or an explicitly approved bounded representation
- **AND** the request excludes the original source path, credentials, and unrelated workspace data

#### Scenario: Raw provider output contains sensitive or excessive data
- **WHEN** an analyzer returns raw content, oversized data, credentials, or operational details
- **THEN** FusionCanvas bounds, sanitizes, or discards that material
- **AND** persists only typed findings, safe explanations, provider identity, and optional usage metadata

### Requirement: Risk warnings use progressive disclosure and remain accessible
FusionCanvas SHALL show a compact default warning near the customer-facing content and SHALL provide a keyboard-reachable details action for findings, review method, timestamp, limitations, and recovery guidance. Warning, unavailable, and finding states SHALL use shared theme resources and remain distinguishable in Light and Dark appearances.

#### Scenario: User reviews a compact warning
- **WHEN** a customer-facing content surface contains unreviewed or reviewed content
- **THEN** the warning is visible without opening a separate administration window
- **AND** the primary creative workflow remains usable

#### Scenario: User opens review details by keyboard
- **WHEN** the user activates the review-details action without a pointer
- **THEN** the findings and limitations are reachable in a predictable focus order
- **AND** focus returns to the invoking content or details action when the details view closes

#### Scenario: Review states change appearance
- **WHEN** the application theme changes or a review transitions between unreviewed, findings, no-obvious-signal, and unavailable states
- **THEN** the state remains visually and textually distinguishable
- **AND** no state relies on color alone
