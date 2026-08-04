# Verification — support-design-triangle-framework

## Summary

The shared framework asset and prompt changes are implemented. No UI, persistence, schema, or provider contract changed. All deterministic tests pass.

## Criterion Evidence

| Capability / scenario | Result | Evidence |
| --- | --- | --- |
| Ideation Basic request includes canonical framework and asks for one socially meaningful Idea | Pass | `AiIdeaGeneratorTests.Generate_BasicModeUsesFrameworkAndProhibitsLaterStageArtifacts`; captured System message contains framework marker, wearer-signal instruction, and Concept/Design/SLL prohibitions. |
| Ideation Basic request preserves resolved context when guidance is empty | Pass | Existing `AiIdeaGeneratorTests` context capture plus serialized `<creative-context>` assertion; implementation retains store, niche, group, Idea, and rejection fields. |
| Ideation Snowclone request includes framework and remains phrase-only | Pass | `AiIdeaGeneratorTests.Generate_UsesIdeationPurposeAndDelimitsSnowcloneGuidanceAsCreativeContext`; captured System/User roles, framework marker, completed-phrase instruction, template context, and unresolved-placeholder behavior are covered. |
| Snowclone batch reuse behavior remains unchanged | Pass | Existing Ideation Application tests pass; this change only alters request System instructions. |
| Concept Initialize request includes canonical guidance and coherent-triangle rules | Pass | `ConceptRefinementServiceTests.InitializeAsync_CapturedRequest_ContainsGuidanceAndCreativeContextAndNoOperationalFields`; System message assertions include wearer signal, viewer inference, and semantic role while existing labeled response parsing passes. |
| Concept Fine tune/Change requests preserve triangle relationships and action semantics | Pass | `ConceptRefinementServiceTests.RefineAsync_CapturedRequest_ContainsGuidanceAndTriangleAndNoOperationalData`; existing Fine tune/Change success and parsing tests pass with new framework-aware System instructions. |
| Operational and secret data remains excluded | Pass | Existing adversarial metadata tests and Ideation operational-key assertions pass; user-authored context remains serialized in the User message and never becomes System authority. |
| Framework is embedded and not exposed in UI | Pass | `EmbeddedDesignTriangleGuidanceSourceTests.Load_ReturnsCanonicalFrameworkContent` verifies all canonical sections; changed scope contains no App markup or UI controls. |
| Canonical framework preserves UTF-8 source text | Pass | The rebuilt asset contains representative `—` and `“` characters and zero `â` mojibake markers; the Integration resource test now asserts all three conditions. |

## Commands

- `dotnet restore .\FusionCanvas.sln` — passed.
- Targeted Application filter (`AiIdeaGeneratorTests|ConceptRefinementServiceTests`) — passed: 18 tests.
- Targeted Integration filter (`EmbeddedDesignTriangleGuidanceSourceTests`) — passed: 1 test.
- `dotnet build .\FusionCanvas.sln --no-restore` — source compilation reached all projects, but the sandbox denied Avalonia telemetry log access at `C:\Users\boe74\AppData\Local\AvaloniaUI\BuildServices\buildtasks.log`.
- `dotnet build .\FusionCanvas.sln --no-restore -p:UsedAvaloniaProducts=` — passed, 0 errors (pre-existing warnings only).
- `dotnet test .\FusionCanvas.sln --no-restore -p:UsedAvaloniaProducts= -v minimal` — passed: 874 tests, 0 failed, 0 skipped.
- `openspec validate support-design-triangle-framework --strict` — passed.
- `openspec validate --all --strict` — passed: 40 items.
- Post-review UTF-8 integrity check — passed: representative em dash and curly quote present; mojibake marker count is 0.
- `dotnet build .\tests\FusionCanvas.Integration.Tests\FusionCanvas.Integration.Tests.csproj --no-restore -p:UsedAvaloniaProducts= -m:1` — passed: 0 warnings, 0 errors.
- `dotnet test .\tests\FusionCanvas.Integration.Tests\FusionCanvas.Integration.Tests.csproj --no-build --filter 'FullyQualifiedName~EmbeddedDesignTriangleGuidanceSourceTests' -v minimal` — passed: 1 test.

## Scope Review

- The framework is an embedded Integration asset loaded through the existing Application-facing source contract.
- Ideation and Concept prompts use System-level framework/output authority; user-authored creative context remains delimited data.
- No credentials, identifiers, timestamps, paths, or provenance are added to requests.
- No UI, workspace persistence, database schema, export format, SLL generator, ASCII sketch generation, or image generation was added.
- The later visual sketch generator can reuse the shipped framework source without requiring changes to this module.
- Review feedback found and corrected a source-encoding defect; the correction is captured in `retrospective.md` and guarded by the Integration resource test.

## Limitations

The default build command without `-p:UsedAvaloniaProducts=` cannot complete in this sandbox because Avalonia BuildServices attempts to append telemetry outside the workspace. The equivalent no-telemetry build and full solution test baseline pass.
