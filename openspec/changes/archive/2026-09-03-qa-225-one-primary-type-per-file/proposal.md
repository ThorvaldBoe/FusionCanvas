## Why

The 2026-08-27 QA review found that the accepted one-primary-production-type-per-file rule is broadly violated. Bundled handwritten types make ownership, navigation, and future changes harder to reason about, so this maintenance module establishes the file layout required by the existing coding standard without changing runtime behavior.

## What Changes

- Split handwritten production files that contain multiple top-level types into one appropriately named file per type.
- Preserve namespaces, accessibility, members, generated-resource relationships, and all existing runtime/API behavior.
- Add a deterministic source-layout verification check for the affected production tree so new violations are detected during review.
- Leave user-facing behavior, persistence formats, and accepted OpenSpec capabilities unchanged.

## Capabilities

### New Capabilities

None. This is an internal maintenance change.

### Modified Capabilities

- `architecture-guidelines`: clarify that the one-primary-type-per-file requirement applies consistently to the App production layer as well as Domain, Application, and Integration.

## Impact

- Affected code: handwritten `.cs` files under `src/`, especially bundled contracts, records, enums, view models, and policy types.
- Affected tests: source-layout verification and the existing solution baseline; behavior tests should remain unchanged unless a split exposes a compile issue.
- APIs and persistence: no intentional changes.
- UX preflight: not applicable because this change has no user-facing interaction.
- Risks: namespace/file mismatches, missed partial declarations, and accidental member movement during mechanical splits; mitigated by compile/test validation and a source-layout scan.
