## MODIFIED Requirements

### Requirement: Release tag convention is documented
FusionCanvas SHALL document release tag conventions that keep the Git tag, application version, release title, and artifact filename aligned for Stable and Preview releases.

#### Scenario: Contributor creates a Stable release
- **WHEN** a contributor documents or performs a Stable release
- **THEN** the convention states the Git tag form `vMajor.Minor.Build`
- **AND** the application version, release title, and artifact filename use the same `Major.Minor.Build` value

#### Scenario: Contributor creates a Preview release
- **WHEN** a contributor documents or performs a Preview prerelease
- **THEN** the convention states the Git tag form `vMajor.Minor.Build-preview.N`, where `N` is a positive prerelease sequence number
- **AND** the application package version and versioned artifact filename retain the same prerelease suffix
- **AND** the GitHub Release is marked as a prerelease

#### Scenario: Release tag and generated application version disagree
- **WHEN** a release workflow processes a tag whose version does not match the version generated from that Git commit
- **THEN** release validation fails before publishing user-facing assets
