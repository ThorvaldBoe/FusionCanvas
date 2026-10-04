# Windows package pipeline Specification

## Purpose

Defines the gated, downloadable Windows candidate package produced from the `main` branch.

## ADDED Requirements

### Requirement: Candidate packages require deterministic tests

The Windows package workflow SHALL run the deterministic solution test workflow before packaging and SHALL skip packaging when that workflow fails or is cancelled.

#### Scenario: Tests pass after a merge to main

- **WHEN** a commit is pushed to `main`
- **AND** all deterministic test projects pass
- **THEN** the package workflow publishes a Windows candidate artifact

#### Scenario: Tests fail after a merge to main

- **WHEN** a commit is pushed to `main`
- **AND** the deterministic test workflow fails
- **THEN** the package job does not publish a candidate artifact

### Requirement: Candidate package is self-contained and versioned

The Windows package workflow SHALL publish the FusionCanvas App as a self-contained `win-x64` Release ZIP whose filename uses Nerdbank.GitVersioning's canonical NuGet package SemVer, including prerelease or build metadata when present.

#### Scenario: Package is created

- **WHEN** the deterministic gate succeeds
- **THEN** the workflow publishes `FusionCanvas-<SemVer>-win-x64.zip`
- **AND** the ZIP contains the published application and its runtime dependencies

#### Scenario: Version metadata is unavailable or malformed

- **WHEN** the build has no usable canonical package version
- **THEN** the packaging step fails
- **AND** no misleadingly named candidate artifact is uploaded

### Requirement: Candidate package integrity is published

The Windows package workflow SHALL publish a SHA-256 checksum and a signed GitHub artifact provenance attestation for the ZIP.

#### Scenario: Consumer verifies package integrity

- **WHEN** a candidate ZIP is uploaded
- **THEN** a matching `.sha256` file is uploaded
- **AND** a GitHub artifact attestation is created for the ZIP

### Requirement: Candidate packaging does not imply installation or release

The candidate package workflow SHALL not create a GitHub Release, installer, automatic update, or release channel.

#### Scenario: Candidate artifact is produced

- **WHEN** the package workflow completes successfully
- **THEN** the result is a retained workflow artifact
- **AND** no installer or GitHub Release is created by this workflow
