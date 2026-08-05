# Extract SLL Document Codec — Design

## Context

The active `add-sll-generation` change introduced a Domain record `SllDocument` (`src/FusionCanvas.Domain/Concepts/SllDocument.cs`) that owns JSON (de)serialization via `System.Text.Json`: `Serialize()` returns `JsonSerializer.Serialize(this)` and `TryDeserialize(string, out SllDocument?)` wraps `JsonSerializer.Deserialize`. The architecture guidelines assign serialization to the Application/Integration boundary, not Domain (Domain "does not reference UI, integration, persistence, marketplace, AI provider, or plugin host projects"; serialization is a persistence/serialization concern). The SLL round-trip is currently driven from the App layer: `SllGenerationSessionViewModel` calls `result.Document!.Serialize()` to store the SLL text in inspector state (`SllGenerationSessionViewModel.cs:221`) and `SllDocument.TryDeserialize(...)` to re-display it (`:245`). Domain tests `SllDocumentTests.cs` also exercise the Domain serialization methods.

This change relocates SLL (de)serialization out of Domain without altering observable behavior, mirroring the Snowclone CSV precedent (Application port `IItemCsvCodec`, Integration adapter `ItemCsvCodec`).

## Goals / Non-Goals

**Goals:**
- Remove `System.Text.Json` and `Serialize`/`TryDeserialize` from the Domain record `SllDocument`.
- Introduce an Application contract `ISllDocumentCodec` owned by the SLL capability, implemented in Integration.
- Preserve the exact round-trip and malformed-input behavior currently covered by `SllDocumentTests` (round-trip equality, invalid JSON → `false`, empty sketch handling).
- Keep the App session view model free of Integration types: it consumes the Application codec contract, injected at the composition root.
- Make the SLL serialization boundary a durable, testable architecture requirement so the Domain purity violation does not regress.

**Non-Goals:**
- Changing the on-disk SLL persistence format or the `ItemMetadataCodec` key under which it is stored.
- Redesigning the `SllDocument` record shape or its `Validate` invariant (Domain behavior stays; only serialization methods move).
- Generalizing a cross-cutting "Domain serialization" rule across other concepts (e.g., other metadata). Each concept that needs this is a separate change.
- Moving `SllDocument.Validate` (a Domain invariant) out of Domain — it stays.

## Decisions

### 1. Codec contract shape mirrors the current Domain methods
`ISllDocumentCodec` exposes `string Serialize(SllDocument document)` and `bool TryDeserialize(string json, out SllDocument? document)`, matching the existing method signatures so callers and tests port mechanically with no behavior change. The codec is a pure transform with no dependencies on `IWorkspaceRepository` or workspace state — it is a stateless utility behind a contract (a real test seam and a persistence-framework boundary), satisfying the "abstractions protect a real boundary" guideline.

### 2. Implementation lives in Integration under a capability folder
`SllDocumentCodec` is added under `src/FusionCanvas.Integration/SllGeneration/SllDocumentCodec.cs`, using `System.Text.Json` with the same options the Domain methods used (default `JsonSerializer` behavior, since the current Domain methods use defaults). The Integration project already references Application; no new project reference is needed.

### 3. App session view model receives the codec via injection
`SllGenerationSessionViewModel` already receives its collaborators by constructor injection. Add `ISllDocumentCodec` as a required constructor dependency and remove the `SllDocument.Serialize()`/`TryDeserialize()` static calls. The composition root (`AppWorkspaceFactory` / `MainWindow.axaml.cs`) constructs `SllDocumentCodec` and injects it. The view model tests pass a fake/real codec. This keeps the App layer depending only on Application contracts and removes the implicit Domain-as-serializer usage.

### 4. Tests relocate, not just rename
- `SllDocumentTests` (Domain) keeps `Validate` / phrase-preservation cases only; the serialization round-trip and invalid-JSON cases move to a new `SllDocumentCodecTests` (Integration) because serialization is now an Integration concern. This is the testing-baseline-mandated placement (integration-facing behavior tested at the boundary).
- A focused Application-level test is not required: the codec is a stateless transform verified at the Integration boundary, and the App view model's use of it is covered by existing framework-free VM tests once the codec is injected as a fake.

### 5. Decisions not to reopen
- The SLL persistence format (JSON of the `SllDocument` shape) is unchanged; only the owner of the (de)serialization code moves.
- `SllDocument.Validate` remains a Domain invariant.
- The codec uses default `System.Text.Json` options to preserve the current wire format exactly.

## Risks / Trade-offs

- **Round-trip fidelity** — the move must preserve the exact JSON shape so already-persisted SLL strings still deserialize. Mitigation: the Integration codec uses the same default `JsonSerializer` options as the current Domain methods; the relocated round-trip test pins equality of a serialized-then-deserialized document.
- **Composition-root wiring** — a missed injection point would break SLL display at runtime. Mitigation: the existing SLL headless view tests (`SllSectionHeadlessTests`) and the session VM tests cover the round-trip path; build is warning-clean.
- **Archive ordering** — this change modifies code introduced by `add-sll-generation`; it must be archived after `add-sll-generation` so the synced `sll-generation` spec contains the base requirements before this codec requirement is layered on.

## Implementation Plan

Layered order, each step verifiable, ending with the full baseline.

### Application contract (framework-free)
1. Add `src/FusionCanvas.Application/SllGeneration/ISllDocumentCodec.cs` — interface with `string Serialize(SllDocument document)` and `bool TryDeserialize(string json, out SllDocument? document)`. (One primary type per file.)
2. (No request/result records needed — the codec is a stateless transform over the existing `SllDocument` Domain type.)

### Integration
3. Add `src/FusionCanvas.Integration/SllGeneration/SllDocumentCodec.cs` implementing `ISllDocumentCodec` with `System.Text.Json` (default options), preserving the exact current behavior of `SllDocument.Serialize`/`TryDeserialize` (including `JsonException` → `false` on deserialize).

### Domain
4. Edit `src/FusionCanvas.Domain/Concepts/SllDocument.cs` — remove the `using System.Text.Json;`, the `Serialize()` method, and the `TryDeserialize` static method. Keep `Validate` and the record shape and the nested `Sll*` records. Verify the Domain project no longer references any serialization framework and remains warning-clean.

### App (UI)
5. Edit `src/FusionCanvas.App/SllGeneration/SllGenerationSessionViewModel.cs` — add `ISllDocumentCodec` as a constructor dependency; replace `result.Document!.Serialize()` (line ~221) with `_codec.Serialize(result.Document!)` and `SllDocument.TryDeserialize(sllText, out var document)` (line ~245) with `_codec.TryDeserialize(sllText, out var document)`.
6. Wire the codec at the composition root: construct `SllDocumentCodec` in `AppWorkspaceFactory` (or `MainWindow.axaml.cs` where the SLL session view model is created) and inject it. No Integration reference leaks into the VM.

### Tests (mirror production, xUnit v3)
7. Add `tests/FusionCanvas.Integration.Tests/SllGeneration/SllDocumentCodecTests.cs` covering: round-trip equality of a representative document; `TryDeserialize` on invalid JSON returns `false` with `null`; empty-sketch / unlabeled-phrase mutation cases (port the relevant assertions from the current `SllDocumentTests`).
8. Edit `tests/FusionCanvas.Domain.Tests/Concepts/SllDocumentTests.cs` — remove the serialization round-trip and invalid-JSON cases; keep `Validate` and phrase-preservation tests. Ensure Domain tests remain framework-free.
9. Update `SllGenerationSessionViewModelTests` (App.Tests) to inject a codec (real `SllDocumentCodec` or a fake) so the SLL store/redisplay path is exercised; keep the tests framework-free.

### Verification gates
10. `dotnet build .\FusionCanvas.sln` warning-clean; `dotnet test .\FusionCanvas.sln` green; `openspec validate --changes extract-sll-document-codec` and `openspec validate` pass; acceptance scenarios mapped to evidence in `verification.md`.
