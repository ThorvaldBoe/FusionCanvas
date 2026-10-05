## Why

The Windows updater currently trusts a GitHub-hosted manifest and a SHA-256 digest, but it does not define the exact release paths, redirect behavior, stale-package rules, or installer publisher identity that make those checks production-safe. Issue #836 closes that release-readiness gap before in-app updates are treated as a stable channel.

## What Changes

- Define a versioned release trust policy for the FusionCanvas stable Windows channel.
- Accept only the repository-owned HTTPS manifest and canonical versioned GitHub Release asset/tag paths; reject unexpected hosts, path shapes, query/fragment mutations, and redirect chains.
- Require a strictly newer stable release for download and application, and fail closed for stale, malformed, unsupported, or policy-version-incompatible metadata.
- Extend release metadata with the expected Authenticode publisher identity and verify the downloaded installer’s Windows signature and signer certificate identity after byte-integrity verification.
- Update the stable release workflow to produce the policy-compliant manifest and require a configured signing certificate without committing certificate material or credentials.
- Add deterministic application/integration tests for URL and schema policy, redirect handling, stale releases, publisher verification, cleanup, and fail-closed behavior.
- Add a disposable tagged-release verification harness that exercises the generated manifest and installer trust checks without using the contributor’s normal workspace.

## Capabilities

### New Capabilities

- `release-trust-policy`: Defines the stable Windows release source, asset, redirect, version, publisher-authenticity, fail-closed, and evolution rules used by the updater.

### Modified Capabilities

None. The updater and stable-release behavior being tightened is still carried by the active `add-in-app-updates` delivery package and has not yet been synced into `openspec/specs/`; this change adds the durable trust contract that package must consume.

## Impact

- Extends update contracts and validation in `src/FusionCanvas.Application/Updates`.
- Adds Authenticode verification and redirect-aware GitHub release transport in `src/FusionCanvas.Integration/Updates`.
- Updates updater composition, release workflow, installer verification tooling, and focused Application/Integration tests.
- Does not change workspace data, database schema, or the normal creative-workspace UI; trust failures remain actionable update errors in the existing About/update surface.
- Requires a maintainer-provided Windows code-signing certificate in the hosted release environment. No certificate, private key, or secret value is added to the repository.
- This is a non-primary-workspace user-facing safety behavior. The update action remains explicit and occasional; no new Appium journey is warranted because the changed decisions are deterministic and the existing installer smoke path covers the native installer boundary.
