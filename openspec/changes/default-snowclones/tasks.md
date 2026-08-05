## 1. Bundled starter content

- [ ] 1.1 Replace the single data row in `src/FusionCanvas.Integration/Snowclones/Resources/starter-snowclones.csv` with the 31 curated rows from design D1, keeping the `Phrase,Guidance` header and UTF-8 encoding and the same embedded resource name (`EmbeddedBundledSnowcloneSource.ResourceName`). Quote (RFC 4180) every field containing a comma, quote, CR, or LF, matching the existing single row's style.
- [ ] 1.2 Confirm every shipped phrase satisfies `SnowcloneTemplatePolicy.Validate` (at least one brace-delimited placeholder, no newlines, no nested/unmatched braces, non-blank placeholders and guidance) and that all 31 phrases normalize to distinct duplicate keys.

## 2. Pinning test update

- [ ] 2.1 Update `tests/FusionCanvas.Integration.Tests/Snowclones/SnowcloneCsvCodecTests.cs` `EmbeddedStarterResource_UsesTheNormalCsvContract` to read the bundled resource through the normal CSV codec, assert `Rows.Count == 31`, assert the exact set of curated phrases (case-insensitive), assert representative guidance for at least one default, and assert `SnowcloneTemplatePolicy.Validate(phrase, guidance).IsValid` for every decoded row.
- [ ] 2.2 Confirm the service/view-model tests that use stub bundled sources remain unchanged and pass (no edits to `SnowcloneLibraryService` or its view models).

## 3. Verification and validation

- [ ] 3.1 Run `dotnet test .\FusionCanvas.sln` and confirm the baseline passes, including the updated resource test.
- [ ] 3.2 Run `openspec validate default-snowclones --strict` and the repository-required validation scope; correct any errors in the implementation or approved artifacts and rerun.
- [ ] 3.3 Create `verification.md` mapping every acceptance scenario in `specs/snowclone-library/spec.md` to its exact test result and evidence, and record the dependency/coordination note that this change archives only after `snowclone-library` is synchronized/archived.
