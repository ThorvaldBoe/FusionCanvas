# Verification

## Result

Passed. The stale listing-configuration blocker is resolved for otherwise editable Design-stage Items. Recovery is explicit, same-Store and active-Offering constrained, atomic at the existing workspace snapshot boundary, and leaves unrelated creative and downstream state intact.

## Acceptance evidence

| Scenario | Result | Evidence |
| --- | --- | --- |
| Otherwise editable Item has a stale configuration | Pass | `LoadDesignStageStateAsync_PreservesStaleConfiguredOfferingAndColorsReadOnly`, `LoadDesignStageStateAsync_StaleConfigurationExposesOnlyActiveSameStoreRecoveryCandidates`, and `StaleConfiguration_ShowsOnlyEnabledRecoveryMutationAndEscapeReturnsFocus` prove stale identity, same-Store candidates, and disabled ordinary mutations. |
| Item is protected for another reason | Pass | `LoadDesignStageStateAsync_ProtectedItemDoesNotExposeRecoveryAuthority` and `RecoverStaleConfigurationAsync_ProtectedItemLeavesSnapshotUnchanged` prove recovery is unavailable and persistence is unchanged. Existing workflow-policy coverage in the full Application baseline covers the other protected lifecycle states. |
| No active replacement is available | Pass | `StaleConfiguration_WithoutCandidateShowsStoreEditorGuidance` proves an empty disabled selector plus actionable Store Editor guidance; the application candidate test proves archived and other-Store Offerings are excluded. |
| Creator chooses a replacement | Pass | `StaleConfiguration_SelectionRequiresConfirmationAndCancellationDoesNotPersist` proves selection opens named reset/preservation confirmation and makes zero recovery calls. The headless test verifies accessible control names and initial Replace focus. |
| Creator cancels recovery | Pass | `StaleConfiguration_SelectionRequiresConfirmationAndCancellationDoesNotPersist` and `StaleConfiguration_ShowsOnlyEnabledRecoveryMutationAndEscapeReturnsFocus` prove no mutation, Escape cancellation, and focus return. |
| Creator confirms a valid replacement | Pass | `RecoverStaleConfigurationAsync_ValidReplacementResetsOnlyOfferingSpecificRelationships` compares reset collections and preserved Items, metadata, Assets, Supporting Image links, workflow/lifecycle state, and unrelated Item relationships. |
| Replacement has different Placeholders or Variants | Pass | The valid-replacement fixture uses a different design area name, position, and dimensions; its assertions prove no old color, row, slot, or Asset assignment is inferred or reattached. |
| Recovery validation or persistence fails | Pass | `RecoverStaleConfigurationAsync_InactiveOrCrossStoreReplacementLeavesSnapshotUnchanged`, `RecoverStaleConfigurationAsync_SaveFailureDoesNotReplaceRepositorySnapshot`, and `ConfirmStaleConfigurationRecovery_FailureKeepsStaleStateAndActionableError` prove validation/save failures preserve the stale snapshot and retain an actionable retry state. |
| Recovery succeeds | Pass | `ConfirmStaleConfigurationRecovery_SuppressesDuplicatesAndRefreshesAuthoritativeState` and `StaleConfiguration_RoutedConfirmationRestoresNormalEditingAndFocus` prove one submission, authoritative refresh, normal editability, empty replacement-derived state, and normal-selector focus. |
| Recovered Item is reopened | Pass | The valid-replacement application test reloads the service from repository state and proves the replacement and cleared state persist. Existing `ProductCatalogPersistenceTests` round-trip the affected normalized catalog and Design-stage snapshot collections through SQLite. |
| Stale Design configuration is intentionally recovered | Pass | The valid-replacement preservation comparison covers the modified `basic-product-workflow` requirement and proves that only the confirmed Offering-specific relationships are reset. |

## Commands

- `dotnet test .\tests\FusionCanvas.Application.Tests\FusionCanvas.Application.Tests.csproj --no-restore --filter FullyQualifiedName~DesignStageServiceTests -v:minimal` — passed, 35 tests.
- `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --no-restore --filter FullyQualifiedName~DesignStageToolViewModelTests -v:minimal` — passed, 4 tests.
- `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --no-build --no-restore --filter FullyQualifiedName~DesignStageToolHeadlessTests -v:minimal` — passed, 29 tests.
- `openspec validate recover-stale-listing-configuration --strict` — passed.
- `dotnet test .\FusionCanvas.sln -m:1 --no-restore -v:minimal` — passed, 1,687 tests (Domain 255, Application 487, Integration 243, App 675, UI Description 27).

## Completion QA

- Architecture: recovery policy and snapshot mutation remain in Application; the Avalonia view model owns presentation state; code-behind contains only routed actions and focus adaptation. Dependencies still point inward.
- Security: no new external input, network, secret, SQL, or filesystem path surface was introduced. Replacement IDs are revalidated against authoritative same-Store active catalog state immediately before save.
- Persistence: no schema migration was required. The operation builds one replacement snapshot and invokes the repository once; save failure leaves the repository snapshot unchanged and no managed-file deletion is invoked.
- UI/accessibility: normal, stale-with-candidates, stale-without-candidates, pending, busy, cancel, failure, and success states are deterministic. Recovery controls have accessible names and tested keyboard/focus behavior. Ordinary Design mutation remains read-only while stale.
- Drift: the implementation matches both delta specs. Normal configuration selection retains its public behavior and now shares a corrected Item-owned relationship reset that no longer removes unrelated Items' slot assignments.

## Limitations

- No live Windows desktop check was run; deterministic Avalonia headless coverage is the required gate and passed.
- Preserved downstream Listing/mockup records remain historical and are not asserted compatible with the replacement Offering, as specified.
