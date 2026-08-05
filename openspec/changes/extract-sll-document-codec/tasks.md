## 1. Application codec contract

- [x] 1.1 Add `src/FusionCanvas.Application/SllGeneration/ISllDocumentCodec.cs` defining `string Serialize(SllDocument document)` and `bool TryDeserialize(string json, out SllDocument? document)`. One primary type per file; namespace `FusionCanvas.Application.SllGeneration`.
- [x] 1.2 Confirm the Application project compiles with no new project references.

## 2. Integration codec adapter

- [x] 2.1 Add `src/FusionCanvas.Integration/SllGeneration/SllDocumentCodec.cs` implementing `ISllDocumentCodec` with `System.Text.Json` (default options), preserving the exact current behavior of `SllDocument.Serialize`/`TryDeserialize` (`JsonException` → `false`, `null` result on failure).
- [x] 2.2 Verify the Integration project remains the only layer referencing `System.Text.Json` for SLL.

## 3. Remove serialization from the Domain type

- [x] 3.1 Edit `src/FusionCanvas.Domain/Concepts/SllDocument.cs` — remove `using System.Text.Json;`, `Serialize()`, and `TryDeserialize(...)`. Keep `Validate` and the record shape and nested `Sll*` records.
- [x] 3.2 Grep the Domain project for any remaining `System.Text.Json` or serialization usage; confirm it is framework-free and warning-clean.

## 4. Update App callers and composition root

- [x] 4.1 Edit `src/FusionCanvas.App/SllGeneration/SllGenerationSessionViewModel.cs` — add `ISllDocumentCodec` as a constructor dependency; replace `result.Document!.Serialize()` with `_codec.Serialize(...)` and `SllDocument.TryDeserialize(...)` with `_codec.TryDeserialize(...)`.
- [x] 4.2 Wire `SllDocumentCodec` at the composition root (`AppWorkspaceFactory` and/or `MainWindow.axaml.cs` where the SLL session view model is constructed) and inject it. No Integration type is referenced from the view model.
- [x] 4.3 Confirm `dotnet build .\FusionCanvas.sln` is warning-clean.

## 5. Tests

- [x] 5.1 Add `tests/FusionCanvas.Integration.Tests/SllGeneration/SllDocumentCodecTests.cs` — round-trip equality; invalid JSON → `false`/`null`; empty-sketch and unlabeled/labeled phrase mutation cases ported from the current Domain tests.
- [x] 5.2 Edit `tests/FusionCanvas.Domain.Tests/Concepts/SllDocumentTests.cs` — remove serialization round-trip and invalid-JSON cases; keep `Validate` and phrase-preservation tests; ensure Domain tests stay framework-free.
- [x] 5.3 Update `tests/FusionCanvas.App.Tests/SllGenerationSessionViewModelTests.cs` to inject a codec (real `SllDocumentCodec` or a fake) and assert the SLL store/redisplay path still works.
- [x] 5.4 Add/keep an `SllSectionHeadlessTests` check that the persisted SLL re-displays after the round-trip if not already covered.

## 6. Verification gates

- [x] 6.1 `dotnet test .\FusionCanvas.sln` is green across all four test projects.
- [x] 6.2 `openspec validate --changes extract-sll-document-codec` and `openspec validate` pass.
- [x] 6.3 Complete `verification.md` mapping each acceptance scenario to evidence (codec tests, Domain purity grep, headless/VM round-trip test, build/test commands).
