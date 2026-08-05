## Why

`SllDocument` (a Domain record) owns JSON (de)serialization: it references `System.Text.Json` and exposes `Serialize()` / `TryDeserialize()`. Serialization is a persistence/serialization concern that the architecture guidelines assign to the Application/Integration boundary, not the Domain layer. This keeps the SLL capability's persistence boundary durable and prevents the Domain purity violation from regressing once the active `add-sll-generation` change is archived.

## What Changes

- Remove `Serialize()` and `TryDeserialize()` (and the `System.Text.Json` dependency) from the Domain record `SllDocument`.
- Add an Application contract `ISllDocumentCodec` with `Serialize(SllDocument)` and `TryDeserialize(string, out SllDocument?)`, owned by the SLL capability.
- Implement the codec in Integration (using `System.Text.Json`), mirroring the Snowclone CSV codec precedent (Application port, Integration adapter).
- Update the SLL round-trip callers — the App session view model that stores/redisplays the SLL, and the Domain tests — to use the codec instead of the Domain methods.
- No observable behavior changes: a generated SLL still persists with the item and survives reopening; the round-trip and malformed-input behavior are preserved, only relocated across the layer boundary.

## Capabilities

### New Capabilities
<!-- None. -->

### Modified Capabilities
- `sll-generation`: Adds a requirement that SLL document (de)serialization is performed through an Application-defined codec implemented outside the Domain layer, so the Domain `SllDocument` type carries no serialization logic or persistence-framework dependency.

## Impact

- **Code:**
  - `src/FusionCanvas.Domain/Concepts/SllDocument.cs` — remove `Serialize`/`TryDeserialize` and the `System.Text.Json` using; keep `Validate` and the record shape.
  - `src/FusionCanvas.Application/SllGeneration/` — add `ISllDocumentCodec` (port) and request/result shapes as needed.
  - `src/FusionCanvas.Integration/SllGeneration/` (new folder) — add the `System.Text.Json`-based `SllDocumentCodec` implementation.
  - `src/FusionCanvas.App/SllGeneration/SllGenerationSessionViewModel.cs:221,245` — replace `result.Document.Serialize()` and `SllDocument.TryDeserialize(...)` with the injected codec.
  - Composition root (`AppWorkspaceFactory` / `MainWindow.axaml.cs`) — wire the codec into the session view model.
- **Tests:**
  - `tests/FusionCanvas.Domain.Tests/Concepts/SllDocumentTests.cs` — remove serialization round-trip/invalid-JSON cases from the Domain test (Domain no longer serializes); keep `Validate` and phrase-preservation tests.
  - `tests/FusionCanvas.Integration.Tests/SllGeneration/SllDocumentCodecTests.cs` (new) — round-trip, malformed-JSON → `false`, and empty-sketch behavior at the codec boundary.
- **Dependencies:** Depends on the active `add-sll-generation` change (which introduces `SllDocument` and its callers); must be archived after it. No new NuGet packages. No persistence schema or migration change.
- **Non-goals:** Redesigning the SLL document shape, changing the SLL persistence format on disk, or moving other Domain concepts' serialization (each is a separate change if needed).
