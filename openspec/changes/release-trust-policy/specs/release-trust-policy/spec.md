## ADDED Requirements

### Requirement: Stable update metadata uses a canonical release policy

The updater SHALL accept only schema 2 metadata for the stable Windows x64 channel. The discovery document SHALL come from the repository-owned HTTPS `latest.json` endpoint, and the manifest SHALL identify the same repository, stable version, canonical versioned installer asset, and canonical release tag without query strings, fragments, alternate hosts, or insecure schemes.

#### Scenario: Canonical schema 2 manifest is accepted

- **WHEN** the updater receives schema 2 metadata for `win-x64`
- **AND** the product version is a stable `Major.Minor.Build` value
- **AND** the installer and release URIs are the exact GitHub Release paths for that version
- **AND** the SHA-256 and publisher certificate fingerprints are valid 32-byte hexadecimal values
- **THEN** the manifest is eligible for version comparison and download

#### Scenario: Legacy or malformed metadata is rejected

- **WHEN** the updater receives schema 1, an unknown schema, a malformed version, a missing fingerprint, or an unsupported platform
- **THEN** the manifest is not eligible for update discovery, download, or application
- **AND** the current installation remains unchanged

#### Scenario: URL policy rejects lookalike paths

- **WHEN** a manifest uses another host, HTTP, a different repository path, a query, a fragment, a release path for another version, or an unexpected installer filename
- **THEN** the manifest is rejected before any installer download begins

### Requirement: Discovery redirects are bounded and canonical

The stable GitHub source SHALL disable automatic redirects and SHALL allow at most one redirect from the repository’s `releases/latest/download/latest.json` endpoint. The redirect target SHALL be an HTTPS versioned `latest.json` asset in the same repository; every extra hop or non-canonical target SHALL fail closed.

#### Scenario: The expected GitHub latest redirect is followed once

- **WHEN** the discovery endpoint returns one redirect to `releases/download/v<stable-version>/latest.json`
- **AND** the final response is successful and contains a canonical schema 2 manifest
- **THEN** the source returns the manifest for validation and version comparison

#### Scenario: An unexpected redirect chain is rejected

- **WHEN** discovery redirects to another host, an insecure URI, a non-release path, or a second redirect
- **THEN** the source fails the update check
- **AND** no manifest is exposed as an installable update

### Requirement: Stale releases cannot be downloaded or applied

The updater SHALL require a candidate product version to be strictly newer than the current product version at discovery, download, and application boundaries. Equal or older manifests and verified packages SHALL be treated as stale and SHALL not be handed to the installer launcher.

#### Scenario: A newer stable release remains installable

- **WHEN** the candidate version is greater than the current version and all trust checks pass
- **THEN** the updater may download and present the candidate as ready to install

#### Scenario: An equal or older candidate is stale

- **WHEN** the candidate version is equal to or older than the current version
- **THEN** the updater reports no installable update or rejects the direct operation
- **AND** the installer launcher is not called

### Requirement: Installer authenticity is verified beyond byte integrity

The updater SHALL verify the downloaded installer’s SHA-256 bytes and SHALL additionally require a valid Windows Authenticode signature whose signer certificate SHA-256 fingerprint exactly matches the publisher fingerprint in the validated manifest. A missing, invalid, untrusted, unsigned, or mismatched signature SHALL fail closed.

#### Scenario: A signed installer with matching publisher identity is accepted

- **WHEN** the downloaded installer has the manifest’s SHA-256 digest
- **AND** Windows reports a valid Authenticode signature
- **AND** the signer certificate fingerprint matches the manifest publisher fingerprint
- **THEN** the updater returns a verified package eligible for the existing explicit install handoff

#### Scenario: Byte-valid but unsigned or wrong-publisher installer is rejected

- **WHEN** the downloaded bytes match the manifest digest
- **AND** the installer is unsigned, untrusted, invalidly signed, or signed by a certificate whose fingerprint differs from the manifest
- **THEN** the updater deletes the downloaded package
- **AND** it does not launch the installer

### Requirement: Stable release production fails closed and is independently verifiable

The stable tagged-release workflow SHALL sign the Windows installer with a maintainer-provided certificate, SHALL emit schema 2 metadata containing the signer certificate fingerprint, and SHALL publish no release when signing or policy validation fails. Before publication it SHALL verify the generated assets in a disposable installation directory.

#### Scenario: A tagged release passes the trust gate

- **WHEN** deterministic tests pass, the tag matches the stable application version, signing succeeds, the checksum and publisher fingerprint match the generated installer, and the disposable installer verification succeeds
- **THEN** the workflow publishes the installer, checksum, and schema 2 manifest to the matching GitHub Release

#### Scenario: Release trust prerequisites are missing

- **WHEN** signing secrets, a valid signature, canonical metadata, or disposable installation verification is unavailable or fails
- **THEN** the workflow fails before release publication
- **AND** it does not publish a misleading update manifest

#### Scenario: A future policy schema is introduced

- **WHEN** a release uses a schema version that the running client does not explicitly support
- **THEN** the client rejects it without attempting download or installation
- **AND** support for the new policy is introduced only through a later client change
