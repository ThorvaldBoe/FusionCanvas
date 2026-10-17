## Context

FusionCanvas already has a Windows NSIS installer, a tag-triggered Stable release workflow, and an in-app updater. The release workflow currently requires Authenticode PFX secrets, creates a schema-2 manifest with a publisher certificate fingerprint, and runs a release smoke check that expects that trust policy. The app downloads the Stable manifest from GitHub's latest-release redirect and verifies the installer with `WindowsInstallerAuthenticodeVerifier`.

GitHub Actions already creates artifact attestations, but the release does not publish the local bundle beside the installer and the app does not verify it. The updater accepts only strictly newer Stable versions. Update preferences belong in application settings; workspace data is a separate persistence boundary.

The user has chosen a default Stable track and opt-in Preview track. Users accept GitHub build provenance as the installation trust signal. No Authenticode certificate will be acquired or required. Verification must happen within FusionCanvas so users do not need GitHub CLI installed to use in-app updates.

## Goals / Non-Goals

**Goals:**

- Automatically publish Stable and Preview setup assets on validated version tags after deterministic tests and a disposable installer smoke check.
- Keep every versioned GitHub Release asset available and make the current Stable release easy to find from the repository.
- Give users a persisted Stable/Preview choice and make the existing update action follow that choice without automatic install or downgrade.
- Verify a setup's digest and GitHub Sigstore provenance before allowing installer handoff.
- Give users a documented GitHub CLI command for independently verifying a downloaded release.

**Non-Goals:**

- Authenticode signing, certificate purchasing, or publisher-certificate secrets.
- macOS/Linux packages, Microsoft Store/MSIX, silent updates, or LTS branches.
- A general-purpose plugin or custom release-channel system.
- An automatic downgrade path when changing from Preview to Stable.

## Decisions

### Release assets are immutable and versioned

Each successful user release attaches a setup named `FusionCanvas-<version>-win-x64-Setup.exe`, its `.sha256` file, a version-specific update manifest, and a `.sigstore.json` bundle. The manifest identifies channel, semantic version, release tag, source commit, installer URL, SHA-256, release URL, and bundle URL. The workflow creates a public GitHub Release only after deterministic tests, version/tag checks, installer build, and disposable install smoke verification pass. Stable releases are not marked prerelease; Preview releases are.

All release assets remain attached to their versioned release. The updater discovers releases through the GitHub Releases API, filters by the chosen track, validates tags and metadata, sorts by semantic version, and reads the version-specific manifest asset. It does not depend on mutable latest-installer redirect behavior. The README links to the GitHub Releases page and current Stable release. Repository retention policy must not expire or delete user release assets.

Stable tags use `vMajor.Minor.Build`; Preview tags use `vMajor.Minor.Build-preview.N`, with `N` produced by the committed NBGV prerelease version configuration (for example, `M.m.B-preview.{height}`). The release workflow requires the Git tag, generated NBGV product version, manifest version, release title, and asset names to agree. NBGV remains the version source of truth; release run numbers do not determine application versions. The release preparation instructions must describe how `version.json` moves between preview and Stable version forms so contributors do not invent tag values.

### Track selection is an application preference

Add an `UpdateTrack` value (Stable or Preview) to `ApplicationSettings`, defaulting to Stable. Extend `JsonApplicationSettingsStore` with a backward-compatible settings version migration. Do not place this preference in workspace state or SQLite.

Place a labeled selector in the existing Settings → About update group. Keep current update status, progress, and explicit Check/Download/Install actions. Track selection saves immediately and starts a check for that track; it never downloads or installs. Disable the selector while checking, downloading, verifying, or handing off an installer. On a track change, cancel or ignore stale check results, clear any candidate or downloaded package associated with the previous track, and check the new track. Maintain keyboard focus and expose the selected track and status through accessible control labels.

Preview means all releases, including prereleases, so Preview users naturally receive a later Stable version once it outranks the preview they installed. Stable checks exclude prereleases. Both tracks use semantic-version ordering. Updates are offered only when greater than the installed version; a Stable result below an installed Preview is shown as no newer Stable update, with no downgrade action.

### GitHub release metadata drives discovery

Extend the update source to request public releases from `ThorvaldBoe/FusionCanvas`, following bounded pagination through the complete available release list. Stable candidates require `prerelease == false`; Preview accepts either value. Reject malformed tags, mismatched manifest/tag versions, unsupported platforms, noncanonical release asset URLs, and duplicate candidate versions. Apply bounded HTTP timeouts, response sizes, and pagination to avoid unbounded resource use. If the configured page limit is reached before the release list is exhausted, report a retryable discovery error instead of claiming that a partial list contains the newest eligible release. Preserve clear retryable errors for API rate limits, connectivity errors, and invalid release data.

Use the version-specific manifest as the contract for installer and bundle download. Bump its schema from 2 and remove `PublisherCertificateSha256`. Include the channel, tag/ref, source commit, bundle URI, and checksum needed to bind the downloaded bytes to the release. Restrict installer, manifest, and bundle links to the expected GitHub repository's release-download path for the exact tag. Verify the manifest before downloading; verify the installer digest and attestation after downloading and before returning `VerifiedUpdatePackage`.

### A local Sigstore verifier enforces the release policy

Replace `WindowsInstallerAuthenticodeVerifier` with an Integration adapter using the `Sigstore` .NET verifier library behind the existing `IInstallerAuthenticityVerifier` boundary. Pin the package version in `src/FusionCanvas.Integration/FusionCanvas.Integration.csproj`, matching the repository's existing per-project package version convention. Verify the downloaded installer against the attached JSON Sigstore bundle and enforce GitHub Actions issuer, `ThorvaldBoe/FusionCanvas` source repository, approved `release-windows.yml` workflow identity, exact release tag/ref, source commit, and the installer subject SHA-256. Compare the verified claims to the manifest before marking the package verified.

The first implementation task must add a small fixture-based compatibility check using a bundle produced by the same pinned `actions/attest` generation used by the release workflow. Confirm that the verifier can read the GitHub SLSA provenance statement and expose the required claims. If it cannot, stop and update this design before implementing production acceptance. Do not write custom signature, certificate-chain, transparency-log, or DSSE verification code, and do not spawn `gh` as the app's verifier.

Verification fails closed. Missing or unsupported bundle content, invalid digest, untrusted identity, wrong workflow/ref/commit, unsupported manifest, or network/verification errors never return a verified package. Remove temporary installer and bundle files on every failed path. User-facing errors identify the failed verification category and explain that setup was not launched.

### User-facing verification uses GitHub CLI

The installation documentation includes a copyable command in this form (with the published version substituted for the placeholder):

```powershell
gh attestation verify .\FusionCanvas-<version>-win-x64-Setup.exe `
  --bundle .\FusionCanvas-<version>-win-x64-Setup.exe.sigstore.json `
  --repo ThorvaldBoe/FusionCanvas `
  --signer-workflow ThorvaldBoe/FusionCanvas/.github/workflows/release-windows.yml `
  --source-ref refs/tags/v<version>
```

The guide explains the output fields needed to identify repository, workflow, source revision, and digest, and links the command to the exact release assets. It states that provenance ties the setup to a repository commit and build workflow; it does not certify code quality.

### Authenticode trust is removed from the updater path

Remove the release-workflow steps that read PFX secrets, sign with SignTool, require `Get-AuthenticodeSignature`, and place a publisher fingerprint in the manifest. Replace the release smoke check's Authenticode expectation with checks for version, digest, bundle validity, and disposable installation. Remove the production Authenticode verifier and update tests/composition. Do not preserve an optional Authenticode requirement because no project certificate exists and the user selected GitHub provenance as sufficient trust.

## Risks / Trade-offs

- [The selected .NET Sigstore library may not support the exact bundle and predicate shape emitted by GitHub's pinned `actions/attest` action] → Require a fixture-based proof as the first implementation task; stop before modifying release acceptance if the claims cannot be verified. Keep the verifier behind the application port so a maintained compatible implementation can replace it.
- [The public GitHub Releases API is rate limited and may be temporarily unavailable] → Use read-only requests with clear retryable errors, conditional caching where supported, bounded pagination, and retain the current installed version without offering an unverifiable installer.
- [NBGV's committed prerelease configuration can be changed incorrectly during release preparation] → Document the preview and Stable `version.json` forms and reject every tag/version mismatch before installer publication.
- [GitHub artifact attestations provide provenance, not source-code review or a guarantee of defect-free software] → State this accurately in release verification guidance and retain human review/release gates.
- [The app's trust policy is tied to the current public repository and workflow path] → Make those expected identities centralized constants with tests; changing repository/workflow identity requires a deliberate code change and release.

## Migration Plan

1. Validate the Sigstore library against a representative GitHub `actions/attest` bundle and pin the compatible version.
2. Update the release workflow to build/tag-check, smoke-test, attest, save the output bundle, and create Stable or Preview GitHub Releases with immutable assets. Remove certificate secret dependencies.
3. Publish a new updater release whose schema accepts only the new manifest and bundle policy. Existing releases without bundles remain downloadable manually but are not installable through this updater.
4. Upgrade application settings documents to the new version while defaulting missing `UpdateTrack` to Stable and preserving all unrelated settings.
5. Rollback is a normal release of the previous application version if updater code fails. Since new releases use the new manifest schema, rollback to the old updater must not be used to process the new release; users can continue using an already installed version or download the setup and follow documented GitHub verification.

## Open Questions

None. Repository review during implementation may confirm exact `Sigstore` API names and package version, but it must not reopen the decisions on release tracks, tag forms, no Authenticode requirement, verification policy, or selector behavior.

## Implementation Plan

1. **Prove the verifier contract.** Add the pinned `Sigstore` package to the Integration project and a narrowly scoped fixture/contract check for a GitHub Actions in-toto bundle. Confirm that local verification can enforce the expected issuer, repository, workflow, release ref, source commit and artifact digest. Record the verified package/version and fixture provenance. Stop and revise the design if any claim is unavailable.
2. **Define release tags and metadata.** Update `version.json`/release guidance and `application-versioning` tests for Stable and Preview tags. Add a workflow preflight that compares `GITHUB_REF_NAME` with NBGV version and verifies release track semantics before packaging.
3. **Publish immutable assets.** Update `.github/workflows/release-windows.yml` or split reusable Stable/Preview workflows as needed; build versioned setup and schema-3 manifest, checksum setup, smoke-test install in a disposable directory, attest the exact setup, preserve `bundle-path`, and attach all assets to correctly flagged GitHub Releases. Remove PFX/signing secret handling. Update README/install docs with Releases link and manual verification steps.
4. **Model track and release policy.** Add the Stable/Preview enum and release metadata fields to Application update contracts; update `UpdateManifestValidator`, semantic-version parsing/ordering, channel eligibility, manifest/tag binding, and downgrade policy. Keep business policy in Application; keep GitHub HTTP/API details in Integration.
5. **Discover GitHub releases.** Replace latest-only manifest redirect logic in `GitHubUpdateSource` with bounded release API discovery, filtering and sorting. Fetch the selected version's versioned manifest, installer and bundle only from canonical release URLs. Handle pagination, rate limits, invalid tags/assets, timeouts and stale check cancellation.
6. **Verify before handoff.** Replace `WindowsInstallerAuthenticodeVerifier` with a Sigstore bundle adapter implementing `IInstallerAuthenticityVerifier`; inject it in `AppServicesFactory`. Bind verified bundle claims to the manifest and digest; clean temporary files on failures. Update disposable release smoke scripts for the new validation rules.
7. **Persist and present the preference.** Add `UpdateTrack` to `ApplicationSettings`, `SettingsDocument`, defaults and normalization. Update `UpdateViewModel`/`SettingsViewModel` to save selection, cancel stale checks, clear old candidates, recheck the selected track, preserve explicit download/install actions, and display no-downgrade state. Add the selector in `SettingsWindow.axaml` About section and keep busy-state/focus/accessibility semantics.
8. **Verify acceptance and reconcile artifacts.** Add deterministic tests at Application, Integration, and App layers; headless Avalonia view tests for selector binding and disabled state; workflow static/preflight tests where existing conventions support them; run hosted release validation after implementation. Update artifacts for any approved requirement corrections, map every scenario below to observed evidence, run `openspec validate --strict`, and run `dotnet test .\FusionCanvas.sln`.

### Acceptance Scenario Verification Map

| Requirement / scenario | Planned verification |
| --- | --- |
| User releases: Stable release publishes its setup | Workflow artifact/preflight validation plus hosted Stable release evidence: release flag, required assets, digest and bundle. |
| User releases: Preview release publishes its setup | Workflow artifact/preflight validation plus hosted Preview release evidence: prerelease flag and required assets. |
| User releases: Failed release gate does not publish a user release | Workflow dependency/condition test and a deliberately failing candidate job in CI; confirm no release API object is created. |
| User releases: User finds the current download and release history | Documentation link check and manual rendered README/installation page review. |
| Track eligibility: Default update track is Stable | Application settings default and migration unit tests. |
| Track eligibility: Stable discovery excludes prereleases | Update selection unit tests with stable/prerelease fixture releases. |
| Track eligibility: Preview discovery includes previews and later Stable releases | Update selection unit tests for prerelease-only, later Stable, and older Stable cases. |
| Track eligibility: Release ordering is deterministic | Application tests for out-of-order API results, semantic precedence, invalid tags, and duplicates; Integration tests for complete pagination and safe failure when the page cap is reached. |
| Track selection: User opts into Preview | UpdateViewModel test for save, recheck, no download, and no install. |
| Track selection: Saved track is restored | JSON settings store round-trip test. |
| Track selection: Existing settings default safely | Versioned JSON migration test preserves other fields and defaults Stable. |
| Track selection: Track selector is unavailable during an update operation | UpdateViewModel busy-state tests plus Avalonia headless enabled/binding test. |
| Update action: Update button installs selected track's newest release | Application selection/update tests and Integration download/verifier test. |
| Update action: Track change invalidates the old candidate | UpdateViewModel cancellation/stale-result tests; verify prior candidate cannot be installed. |
| Update action: Stable is older than installed Preview | Semantic version and view-model state tests prove no downgrade action is offered and explanatory status is shown. |
| Update action: Selected track has no newer release | Application result and About status tests. |
| Provenance: Valid release installer is accepted | Integration test using fixture bundle from the pinned GitHub attestation format and matching manifest. |
| Provenance: Installer digest does not match | Downloader/verifier tests for manifest digest mismatch and attested subject mismatch. |
| Provenance: Provenance identity or source does not match | Sigstore policy tests for wrong repository, workflow, ref and source commit. |
| Provenance: Bundle is absent or cannot be verified | Missing, malformed, unsupported and cryptographic failure tests; assert no package/handoff. |
| Provenance: Failed verification cleans up the download | Integration tests assert temporary installer and bundle removal for each failed verification path. |
| User verification: User verifies a downloaded installer | Documentation command/link check plus manual command against a hosted release bundle. |
| Versioning: Contributor creates a Stable release | Version/tag convention tests and generated-version/tag match preflight test. |
| Versioning: Contributor creates a Preview release | Preview suffix/tag/package version and prerelease flag tests. |
| Versioning: Release tag and generated version disagree | Workflow preflight test confirms failure before release creation. |

No Appium scenario is planned: the user journey is short and reversible, and its selection, focus, disabled control, persistence, and status outcomes are covered deterministically by the App headless view lane. Installer launching remains in the existing disposable Windows smoke lane.
