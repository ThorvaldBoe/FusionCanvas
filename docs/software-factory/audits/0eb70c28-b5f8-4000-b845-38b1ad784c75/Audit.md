# Software Factory Audit — PR #984

- Audit ID: `0eb70c28-b5f8-4000-b845-38b1ad784c75`
- Status: `BLOCKED`
- Baseline readiness: `PROVISIONAL` (UI and Values baselines are drafts; Security source-extract definition is working)
- Started at: `2026-11-22T17:03:32Z`
- Ended at: `2026-11-22T17:27:35Z`
- Auditor/reviewer: Codex / pending human review
- Request scope: `FULL_AUDIT` of all six axes, scoped to the 69 files in merged PR [#984](https://github.com/ThorvaldBoe/FusionCanvas/pull/984), plus directly related callers, registrations, tests, persistence, Printify contracts, and current specifications.
- Included axes and baselines: Architecture & Code Structure FC-ARCH 2.0.0 Active / definition 2.0 Active; Functionality & Logic 1.2 Active / definition 1.2 Active; UX 1.1 Active / definition 1.2 Active; UI 1.5 Draft normative baseline / definition 1.5 Draft; Security & Privacy 1.0 Active / definition 1.2 Working; Values, Vision & Taste 1.1 Draft normative baseline / definition 1.2 Draft.
- Excluded axes and reasons: None. Full-repository certification and files outside PR #984’s changed scope/dependency neighborhood are out of scope.
- Profile: `DEEP_CHANGE`; source revision `09d3089f734e99025056cfdfeeb36101de702d9d`; worktree clean at audit start.
- Progress checkpoint: `FINALIZE`; 68 source items inventoried across six extracts (45 architecture types, 6 functionality capabilities, 4 UX workflows, 4 UI surfaces, 5 security surfaces/flows, and 4 values decisions); 68 manually routed with no candidates filtered; 63 item reviews completed; 5 Security & Privacy units have Pass C outstanding. Approximate process coverage: 93% by source-item count, not a compliance score. Criterion-pair totals are not exact because manual routing grouped checks by source role and capability; no classifications were silently suppressed.
- Parent checkpoint: Five axis runs reached profile assessment; Security & Privacy is blocked on the mandatory automated dependency/secret sweep. Resume by rerunning the package/security inventory when NuGet vulnerability data is reachable and completing Pass C.

## Axis runs

| Axis | Audit run ID | Standard / definition | Extract | Result |
|---|---|---|---|---|
| Architecture & Code Structure | `b09c5e9c-83d1-41ce-8585-7460394a6ba0` | FC-ARCH 2.0.0 Active / 2.0 Active | [source-extract.json](b09c5e9c-83d1-41ce-8585-7460394a6ba0/source-extract.json), [run assessment](b09c5e9c-83d1-41ce-8585-7460394a6ba0/Audit.md) | Complete; one Major finding |
| Functionality & Logic | `68c84b8d-e09e-49bd-bd0d-a1c613cdeac2` | 1.2 Active / 1.2 Active | [source-extract.json](68c84b8d-e09e-49bd-bd0d-a1c613cdeac2/source-extract.json), [run assessment](68c84b8d-e09e-49bd-bd0d-a1c613cdeac2/Audit.md) | Complete; one functional finding; cross-axis UX finding also affects acceptance |
| UX | `c2893744-695d-4151-9480-08e6cf17b80c` | 1.1 Active / 1.2 Active | [source-extract.json](c2893744-695d-4151-9480-08e6cf17b80c/source-extract.json), [run assessment](c2893744-695d-4151-9480-08e6cf17b80c/Audit.md) | Complete; cross-axis finding and disposed spec challenge |
| UI | `d375a700-1935-4fce-aeec-df41ffd7c76e` | 1.5 Draft / 1.5 Draft | [source-extract.json](d375a700-1935-4fce-aeec-df41ffd7c76e/source-extract.json), [run assessment](d375a700-1935-4fce-aeec-df41ffd7c76e/Audit.md) | Provisional; no finding; no native rendering claim |
| Security & Privacy | `dfa083ae-9110-471f-85aa-d733d1db2cbd` | 1.0 Active / 1.2 Working | [source-extract.json](dfa083ae-9110-471f-85aa-d733d1db2cbd/source-extract.json), [run assessment](dfa083ae-9110-471f-85aa-d733d1db2cbd/Audit.md) | Blocked; Pass C unavailable |
| Values, Vision & Taste | `3ec82ba9-823f-47f3-a088-e2e05d2fb8b9` | 1.1 Draft / 1.2 Draft | [source-extract.json](3ec82ba9-823f-47f3-a088-e2e05d2fb8b9/source-extract.json), [run assessment](3ec82ba9-823f-47f3-a088-e2e05d2fb8b9/Audit.md) | Provisional; aligns with user-approved direction |

## Findings and challenges

1. **F-ARCH-001 — Major, ARCH-008:** application code now writes and parses Printify-specific JSON in `OfferingOptionValue.MetadataJson` for color swatches. Keep the Printify representation and serialization at the integration/persistence boundary and expose typed swatch data to the use case. Tracked in [issue #986](https://github.com/ThorvaldBoe/FusionCanvas/issues/986).
2. **F-FUNC-001 — Low:** resolving a migrated legacy Color does not revalidate that its current Offering still has an active sellable Variant. A catalog refresh between load and resolve can persist an unusable selection. Tracked in [issue #987](https://github.com/ThorvaldBoe/FusionCanvas/issues/987).
3. **F-UX-001 — Medium:** the More Options dialog lists `ListingOtherColors` only, excluding favorites, despite the acceptance scenario requiring the complete configured Color collection. This prevents comparing or bulk-selecting favorite and nonfavorite Colors in the same surface. Tracked in [issue #985](https://github.com/ThorvaldBoe/FusionCanvas/issues/985).
4. **CH-UX-001 — Disposed:** the delta’s requirement says More Options contains “other” Colors while its scenario says “every” Color including favorites. Earlier user discussion described More Options as the full provider color set; the audit evaluates against the explicit complete-collection scenario and records the discrepancy as F-UX-001. A future OpenSpec delta should make this wording consistent.
5. **CH-SEC-001 — Open / blocking:** the required package vulnerability query cannot reach `api.nuget.org`; no complete local secret/dependency scanner is installed. Source review and a manual package-version/advisory check do not substitute for the required codebase-wide Pass C.

GitHub issue creation was authorized after the review. Issues [#985](https://github.com/ThorvaldBoe/FusionCanvas/issues/985), [#986](https://github.com/ThorvaldBoe/FusionCanvas/issues/986), and [#987](https://github.com/ThorvaldBoe/FusionCanvas/issues/987) were created with the `audit-finding` label. No implementation changes were made.

## Verification and evidence

- `dotnet build .\FusionCanvas.sln` — passed, 0 errors. Restore emitted NU1900 because the NuGet vulnerability service could not be reached; existing analyzer warnings also appeared.
- `dotnet test .\FusionCanvas.sln -m:1 --no-restore` — 2,598 passed, 0 failed: Domain 301, Application 740, Integration 473, App 1,055, UI Description 29.
- `python -m unittest discover -s tools/software-factory/tests -v` — 11 passed.
- `openspec validate separate-artwork-variants-from-listing-colors --strict` — passed.
- `git diff --check` — passed for the audit worktree.
- `dotnet list .\FusionCanvas.sln package --include-transitive --no-restore` — package inventory completed; ImageSharp 4.1.3 is present. `dotnet list ... package --vulnerable --include-transitive --no-restore` failed at DNS resolution for `api.nuget.org`; result is unknown, not a clean scan. No `gitleaks`, `trivy`, or `semgrep` executable was available.
- Official ImageSharp advisories reviewed include [GHSA-j3p4-wp97-rph4](https://github.com/SixLabors/ImageSharp/security/advisories/GHSA-j3p4-wp97-rph4) and [GHSA-ffp7-56pq-64mr](https://github.com/SixLabors/ImageSharp/security/advisories/GHSA-ffp7-56pq-64mr), which list 4.1.2 as patched for their stated ranges. This limited check is not represented as the missing package scan.
- Printify’s [API reference](https://developers.printify.com/) documents `show-out-of-stock` for provider variants and `is_available` as stock status; changed client tests cover the query contract.
- Appium source compiled in the PR verification, but the native Windows scenario was not run because the local Appium endpoint was unavailable. Headless UI coverage passed; no claim is made about native rendering or assistive-technology output.
- Prior Printify listing-import Security audit `e40e7d86-c50b-4948-9e91-66b5261b4a1e` was blocked on the same class of package-sweep limitation. This run rechecked the changed import/client surfaces and records the current Pass C limitation; no prior Security certification is treated as current.

## Jev routing

- Adapter: repository `jev_audit.py` SHA-256 `406ce3b96369f2d5ac5acf376306d2ef99b675ab776e187f3dda8845980818cc`; requested Jev model `typesafe/jev-1.13`, OpenRouter, threshold `0.2`, profile `DEEP_CHANGE`.
- First invocation failed with `CLI_ERROR`: invocation `fd27a52d-8353-433f-abe2-db912ef9bfd5`; log [`b09c5e9c.../jev/fd27a52d-8353-433f-abe2-db912ef9bfd5.jsonl`](b09c5e9c-83d1-41ce-8585-7460394a6ba0/jev/fd27a52d-8353-433f-abe2-db912ef9bfd5.jsonl). Jev decisions remain `UNKNOWN`; none were treated as successful classifications.
- Manual fallback bypass records after the failed invocation: Architecture `5ff8a52b-b8bc-46c8-8dfb-f36e65803c6a`; Functionality `a7483059-fcd9-4800-aae5-ae2158632373`; UX `f467cb9e-629e-4aeb-85f9-4492ce3b52e2`; UI `a207ee46-0637-42c8-8784-e3496967d9ee`; Security `cbf64748-14d5-4c03-ae12-dceefab54c18`; Values `b8f8d9b3-2ecb-4bd2-99b2-84db36d2fe49`. Logs are in each run’s `jev/` directory. Manual routing retained all candidate checks; no confidence scores were invented.

## Final coverage and disposition

- Six requested axes were inventoried and manually routed. Architecture, Functionality, and UX have active baselines and completed scoped reviews. UI and Values results are provisional because their normative baselines are drafts. Security & Privacy remains blocked until Pass C completes.
- The overall audit remains `BLOCKED`; process completion does not imply compliance. Resume with the same audit/run IDs after NuGet vulnerability data or an equivalent approved codebase-wide dependency scanner is available.
