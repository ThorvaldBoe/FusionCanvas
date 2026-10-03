# Verification

## Acceptance evidence

| Acceptance scenario | Result | Evidence |
| --- | --- | --- |
| Design shows incomplete normalized Offering blockers | PASS | `DesignStageServiceTests.LoadDesignStageStateAsync_ReportsIncompleteNormalizedOfferingReadiness`; the state reports `NeedsAttention`, names `Front mockup`, and exposes `MissingMapping`. |
| Design shows ready-template count without claiming Item readiness | PASS | `DesignStageServiceTests.LoadDesignStageStateAsync_ReportsReadyNormalizedOfferingWithoutClaimingItemReadiness`; the state reports one ready template and no Item artwork assertion. `DesignStageToolViewModelTests.LoadAsync_NormalizedOfferingReadinessIsVisibleWithoutReplacingArtworkGuidance` verifies separate Design guidance remains present. |
| Legacy or unavailable configuration preserves existing behavior | PASS | Existing legacy Design service coverage remains green in the focused `DesignStageServiceTests` run; the projection is nullable and is only created for an active normalized Offering. |
| Read-only Design does not gain mutation controls | PASS | Existing protected/stale Design service and headless coverage remains in the focused test surface; the new panel has no commands or editable controls. |
| Existing artwork readiness and slot workflow remain unchanged | PASS | `DesignStageToolViewModelTests` passed 9/9, including artwork readiness cases; the existing Design controls remain present in the new headless scenario's assertions. |

## Commands and results

- `dotnet test .\tests\FusionCanvas.Application.Tests\FusionCanvas.Application.Tests.csproj --no-build --no-restore --nologo --filter "FullyQualifiedName~DesignStageServiceTests"` — PASS, 41/41.
- `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --no-restore --nologo --filter "FullyQualifiedName~DesignStageToolViewModelTests"` — PASS, 9/9.
- `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --no-build --no-restore --nologo --filter "FullyQualifiedName~NormalizedOfferingReadiness_ShowsAccessibleGuidanceBeforeDesignControls"` — BLOCKED by the existing native Avalonia/xUnit test-host crash (exit code `-1073741571`); no product assertion failure was emitted. The repository baseline previously exhibited the same crash after 112 App tests.
- `openspec validate design-offering-readiness-guidance --strict` — PASS.
- `git diff --check` — PASS.

The full solution baseline is intentionally not marked complete for this iteration because the App test host's native crash prevents a reliable solution-wide result. The focused non-headless suites and compile completed successfully; the limitation is retained for follow-up rather than hidden behind aggregate results.
