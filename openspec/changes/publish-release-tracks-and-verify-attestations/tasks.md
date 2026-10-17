## 1. Prove verifier compatibility and define release versioning

- [x] 1.1 Add and pin the Sigstore .NET verifier package in the Integration project; verify a fixture emitted by the pinned GitHub `actions/attest` workflow format and record how repository, workflow, ref, commit, and subject digest are asserted. Stop and revise design if the claims cannot be enforced.
- [x] 1.2 Implement Stable and Preview tag/version preflight rules using NBGV output; test accepted `vMajor.Minor.Build` and `vMajor.Minor.Build-preview.N` tags plus mismatched and malformed versions.
- [x] 1.3 Update contributor release documentation and versioning delta so tag, NBGV package version, release title, track, and asset names remain aligned.

## 2. Publish Stable and Preview releases

- [x] 2.1 Update the tagged Windows release workflow to run deterministic tests, validate track/tag/version, package the versioned NSIS installer, and pass the disposable installation smoke check before release publication.
- [x] 2.2 Generate a versioned update manifest and SHA-256 checksum without Authenticode fields; attest the exact installer, retain `bundle-path`, and attach the setup, checksum, manifest, and `.sigstore.json` bundle to the GitHub Release.
- [x] 2.3 Remove PFX secret reads, SignTool use, Authenticode assertions, and certificate fingerprint metadata from the production release path and smoke script.
- [x] 2.4 Publish Stable releases as non-prereleases and Preview releases as prereleases while retaining every versioned release asset; update README/install guidance with Releases and current Stable links plus `gh attestation verify` instructions.
- [ ] 2.5 Verify candidate workflow behavior for a valid Stable tag, valid Preview tag, failed deterministic gate, failed installer smoke check, and mismatched generated version; confirm failed cases do not publish releases.

## 3. Implement release discovery and update-track policy

- [x] 3.1 Add update track and versioned release metadata contracts; remove the Authenticode certificate field and bump/update the manifest schema validator.
- [x] 3.2 Implement semantic version parsing, Stable/Preview eligibility, deterministic release ordering, and strict no-downgrade update policy in Application.
- [x] 3.3 Replace latest-only manifest redirect discovery with bounded GitHub Releases API discovery and version-specific manifest resolution; constrain asset URLs and handle invalid releases, rate limits, timeout, pagination, and cancellation in Integration.
- [x] 3.4 Add focused Application and Integration tests for ordering, channel eligibility, tag/manifest binding, malformed releases, pagination/API failures, and manifest/asset URL restrictions.

## 4. Enforce attestation verification before installation

- [x] 4.1 Replace the Authenticode verifier with a Sigstore bundle verifier adapter that enforces repository, release workflow, issuer, tag/ref, source commit, and subject digest policy.
- [x] 4.2 Download and validate the installer plus bundle, compare verified claims with the manifest, and return a verified package only after all checks pass.
- [x] 4.3 Remove temporary setup and bundle files for every failed validation path; return actionable verification errors and prevent installer handoff.
- [x] 4.4 Update App composition and release smoke scripts to use the new verifier; add positive, tampered digest, missing/malformed bundle, wrong identity/ref/commit, unsupported bundle, and cleanup tests.

## 5. Persist and expose the selected track

- [x] 5.1 Add application-level `UpdateTrack` settings with Stable default and backward-compatible JSON migration that preserves existing settings.
- [x] 5.2 Update `UpdateViewModel` and Settings integration to save the selection, invalidate stale candidates, recheck the selected track, preserve explicit download/install actions, and explain the no-downgrade Stable case.
- [x] 5.3 Add the labeled Stable/Preview selector to Settings → About, including disabled busy state and accessible label/status.
- [x] 5.4 Add settings persistence and update-view-model tests for default, migration, restore, save/recheck, stale results, selected track update, busy states, and no downgrade.
- [x] 5.5 Add Avalonia headless view coverage for selector binding, track changes, focus/keyboard behavior, and disabled state during update operations.

## 6. Complete criterion-level verification and delivery readiness

- [ ] 6.1 Run the full acceptance scenario map in `design.md`; record actual CI, hosted release, fixture, test, and documentation evidence for each scenario and correct any failed criterion.
- [x] 6.2 Run `openspec validate --strict` and correct all change artifact errors.
- [x] 6.3 Run `dotnet test .\FusionCanvas.sln` and resolve failures before the module is considered complete.
- [x] 6.4 Review changed scope for trust-policy drift, ensure no Authenticode secret dependency remains, and confirm release assets and verification guidance match the accepted specs.
