## ADDED Requirements

### Requirement: Stable releases publish an update contract
The release workflow SHALL publish a stable Windows `win-x64` NSIS setup program only for an explicitly created `v<Major>.<Minor>.<Build>` release tag, and SHALL publish a machine-readable manifest that identifies the same product version, installer asset, and SHA-256 digest.

#### Scenario: Maintainer publishes a stable release
- **WHEN** a maintainer pushes a valid `v<Major>.<Minor>.<Build>` tag
- **AND** the deterministic solution test gate succeeds
- **THEN** the workflow publishes a GitHub Release containing the versioned NSIS setup program, its `.sha256` checksum, and the update manifest
- **AND** the manifest version, release tag, setup filename, and checksum identify the same release

#### Scenario: Candidate build reaches main without a release tag
- **WHEN** a commit is merged to `main` without a release tag
- **THEN** candidate package and installer workflows may produce retained workflow artifacts
- **AND** no stable update manifest or user-visible stable release notification is published

#### Scenario: Release validation fails
- **WHEN** the tag, canonical application version, deterministic test gate, publish output, installer output, or checksum is missing or inconsistent
- **THEN** the release workflow fails before publishing an update manifest that points to unusable assets

### Requirement: The update manifest is explicit and portable
The update manifest SHALL contain the stable product version, supported platform identifier, installer download URL, SHA-256 digest, and release page URL in a versioned schema that can be consumed without GitHub-specific types leaking into application or domain code.

#### Scenario: Client reads a supported manifest
- **WHEN** an updater client receives a well-formed manifest for `win-x64`
- **THEN** it can compare the advertised product version with the running product version and identify the verified installer asset

#### Scenario: Client receives unsupported or malformed metadata
- **WHEN** the manifest is malformed, names an unsupported platform, has an invalid version, uses an unsafe URL, or omits the installer digest
- **THEN** the client treats the update as unavailable or failed
- **AND** it does not download or launch an installer
