# Verification

## Automated checks

| Check | Result | Evidence |
| --- | --- | --- |
| Stable and Preview tag/version preflight | PASS | `tools/updates/Test-FusionCanvasReleaseVersion.ps1` covers Stable, Preview, tag/package mismatch, malformed version, zero preview sequence, and unsupported `rc` suffix. |
| PowerShell syntax | PASS | PowerShell parser accepted the release smoke test, version test, and release-version module. |
| Sigstore compatibility | PASS, representative fixture | Sigstore .NET 1.1.1 verified an upstream GitHub Actions SLSA v0.3 bundle and matching artifact using the fixture's trusted root. Verified repository URI, tag ref, source commit, workflow path, predicate type, and subject digest. The fixture is from the Sigstore .NET repository, not a bundle emitted by this repository's exact pinned `actions/attest` revision. Integration tests also verify the configured certificate identity and reject mismatched installer digest, predicate type, repository, workflow path, and tag ref. |
| Application and integration policy | PASS | Application tests cover semantic version parsing/ordering, Stable/Preview eligibility, no-downgrade behavior, and manifest validation. Integration tests cover release API pagination/cap, malformed release data, canonical manifest/asset URLs, cancellation, download digest, missing bundle, verifier failure, cleanup, certificate identity policy, and provenance subject/predicate rejection. Final Integration result: 460/460. |
| Settings and UI | PASS | Full solution run: App tests 1,031/1,031. These cover Stable default/migration/round-trip, track selection save/recheck, and selector presence and busy state in an Avalonia headless view. |
| Release tag/version helper | PASS | `Test-FusionCanvasReleaseVersion.ps1` completed successfully. |
| OpenSpec strict validation | PASS | `openspec validate publish-release-tracks-and-verify-attestations --strict` reports the change is valid. |
| Solution baseline | PASS | `dotnet test .\\FusionCanvas.sln -m:1 --no-restore` passed: Domain 294, Application 713, Integration 451, App 1,031, UI-description 29. After adding final verifier-policy tests, Integration was rerun and passed 460/460. `TEMP` and `TMP` pointed to `.test-tmp` because the sandbox denies the default Windows temp directory. |

## Acceptance scenarios

| Scenario group | Result and evidence |
| --- | --- |
| Stable and Preview releases publish versioned setup, checksum, manifest, and provenance bundle | PASS for Preview; Stable remains pending. Hosted tag `v0.2.0-preview.7` published [release assets](https://github.com/ThorvaldBoe/FusionCanvas/releases/tag/v0.2.0-preview.7): versioned setup, SHA-256 file, update manifest, and Sigstore bundle. Workflow run [39145562006](https://github.com/ThorvaldBoe/FusionCanvas/actions/runs/39145562006) completed successfully on commit `b725ffdc209f3de6ef2c5e8e46288a0dae39cafb`. |
| Failed deterministic or installer gates prevent publishing | PASS. `v0.2.0-preview.6` passed deterministic tests, build, and attestation but its disposable install trust smoke failed before the Create GitHub Release step; no Release object was created for that tag. After forwarding the explicit machine-wide mutation opt-in only in the disposable hosted runner, the `v0.2.0-preview.7` trust smoke and release passed. |
| Users find current Stable and full release history | README links to GitHub Releases and `/releases/latest`; contributor guidance documents Stable and Preview tag and verification steps. |
| Stable default, Stable exclusion, Preview inclusion, deterministic order | Covered by settings migration and Application update policy tests. |
| Track selection saves, restores, rechecks, and does not auto-install | Covered by settings-store, UpdateViewModel, and Avalonia headless tests. |
| Update uses the newest eligible release and never downgrades | Covered by Application release-selection tests, Integration download/verifier tests, and Preview-on-Stable no-downgrade tests. |
| Manifest and release binding; malformed data, pagination, URL, API, cancellation handling | Covered by Application validator and GitHubUpdateSource Integration tests. |
| Valid provenance and rejection of digest/identity/ref/commit/bundle failures; cleanup | Production verifier enforces GitHub Actions certificate identity and extensions plus in-toto subject/predicate claims. The upstream representative fixture verifies with the pinned library. Downloader tests cover manifest digest mismatch, missing bundle, verification failure, and temporary-file cleanup. A checked-in cryptographic negative-fixture matrix is not present; those policy branches are enforced by verifier validation. |
| User independently verifies downloaded installer | PASS. The hosted `v0.2.0-preview.7` release gate ran `gh attestation verify` with repository, signer workflow, source ref, and the release bundle as part of `Test-FusionCanvasRelease.ps1`; the complete trust smoke passed. README and CONTRIBUTING document the same command for users. |
| Stable/Preview tag and generated-version agreement | PASS for Preview `v0.2.0-preview.7`. PowerShell preflight tests pass for both tracks and mismatch/malformed cases; the hosted workflow invoked the helper before packaging and published the matching prerelease. |

## Hosted release evidence

Hosted Preview evidence is now recorded for `v0.2.0-preview.7`: prerelease flag, all four versioned assets, checksum validation, `gh attestation verify`, disposable installer smoke, and successful GitHub Release creation. The failed `v0.2.0-preview.6` candidate also confirms the release object is not created when installer trust smoke fails. Stable publishing remains unverified by a hosted stable tag.

## Environment notes

- NuGet vulnerability metadata requests could not reach `api.nuget.org`; restore/build reports NU1900. Existing ImageSharp vulnerability advisories also appear in restore output.
- The sandbox blocks writes to the default Windows temp location. The solution baseline is configured to use `.test-tmp` under the workspace.
