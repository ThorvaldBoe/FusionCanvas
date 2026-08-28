# Provider Mockup Image Selection Guidance Verification

## Acceptance Evidence

| Acceptance scenario | Method | Result | Evidence and limitations |
| --- | --- | --- | --- |
| User opens provider image selection | Avalonia headless test | Pass | Production XAML exposes the visible **Provider mockup image** label, matching automation name, persistent Offering provider-catalog instructions, and explicit local-upload/drag-drop limitation. No upload or drop control was introduced. |
| Provider catalog is loading | Controlled ViewModel test | Pass | `ProviderImageSelection_ExposesLoadingBeforePendingSourceCompletes` holds the source await, observes typed `Loading` and loading guidance, then completes deterministically. |
| Provider catalog provides candidates | ViewModel/headless tests | Pass | Existing focused setup plus `ProviderImageSelection_RendersAvailableEmptyUnavailableAndErrorGuidance` verify `Available`, real candidates, selector binding, and target Design Area guidance. |
| Provider catalog is empty | ViewModel/headless tests | Pass | Empty available descriptors produce `Empty`, no candidates, distinct empty text, and sync/review recovery. |
| Provider catalog is unavailable | ViewModel/headless tests | Pass | Null and explicitly unavailable sources preserve the supplied reason and show configure/sync recovery without candidates. |
| Provider catalog request fails | ViewModel/headless tests | Pass | Throwing collaborators produce `Error`, recoverable failure/retry guidance, and an empty candidate collection. |

## Required Gates

- Focused state/guidance command: 5 passed, 0 failed.
- Solution baseline: 1,410 passed, 0 failed, 0 skipped (Domain 232; Application 384; Integration 189; App 578; UI-description 27).
- Strict OpenSpec: 54 passed, 0 failed.
- `git diff --check`: passed with expected line-ending normalization notices only.

## Supplemental Review

The implementation is App-only and intentionally supplies guidance rather than a provider-setup navigation command until that integration exists independently.
