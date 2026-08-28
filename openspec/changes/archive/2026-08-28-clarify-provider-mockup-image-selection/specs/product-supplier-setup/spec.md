## ADDED Requirements

### Requirement: Provider mockup image selection communicates source and recovery
FusionCanvas SHALL identify the Mockup Template image selector with a persistent visible label and accessible name and SHALL explain that candidates come from the active Offering's provider catalog. The guidance SHALL remain available while candidates load and when the result is available, empty, unavailable, or failed. It SHALL NOT imply that local upload or drag/drop is supported, and unavailable or failed states SHALL identify a supported provider setup or synchronization next action without fabricating candidates.

#### Scenario: User opens provider image selection
- **WHEN** the Mockup Template editor is shown
- **THEN** the selector has the visible label and accessible name **Provider mockup image**
- **AND** nearby instructions explain how to choose an Offering provider-catalog image
- **AND** state that local upload and drag/drop are not available

#### Scenario: Provider catalog is loading
- **WHEN** provider mockup candidates are being requested
- **THEN** the persistent guidance remains visible
- **AND** state text explains that provider-catalog images are loading

#### Scenario: Provider catalog provides candidates
- **WHEN** one or more provider mockup candidates are available
- **THEN** the selector exposes those candidates
- **AND** state text prompts the user to choose the provider view that matches the target Design Area

#### Scenario: Provider catalog is empty
- **WHEN** the configured provider catalog is available but contains no mockup images for the Offering
- **THEN** state text distinguishes the empty result from loading and failure
- **AND** directs the user to sync or review the Offering's provider catalog setup

#### Scenario: Provider catalog is unavailable
- **WHEN** no provider catalog source exists or the source reports that it is unavailable
- **THEN** state text explains the supplied reason when available
- **AND** directs the user to configure or sync provider catalog data before returning

#### Scenario: Provider catalog request fails
- **WHEN** loading provider mockup candidates raises an error
- **THEN** state text identifies the recoverable load failure without exposing a fabricated candidate
- **AND** directs the user to review provider setup or retry synchronization

