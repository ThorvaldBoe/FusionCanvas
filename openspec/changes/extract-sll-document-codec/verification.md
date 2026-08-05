# Extract SLL Document Codec — Verification

## Status

Complete. The Domain `SllDocument` type no longer owns serialization; a codec at the Application/Integration boundary handles round-tripping. Build is warning-clean, the full solution baseline passes, and strict change/repository OpenSpec validation pass.

## Acceptance evidence

| # | Acceptance scenario (specs/sll-generation/spec.md) | Passing automated evidence | Result |
|---:|---|---|---|
| 1 | Domain SLL document type carries no serialization: exposes no serialize/deserialize methods and the Domain project has no JSON/persistence-framework dependency | `SllDocumentTests` (Domain) keeps only `Validate` cases; `git grep` of `src/FusionCanvas.Domain` shows no `System.Text.Json` reference and `SllDocument.cs` has no `Serialize`/`TryDeserialize` | PASS |
| 2 | SLL document round-trips through the codec | `SllDocumentCodecTests.Serialize_RoundTrips_PreservesFields` (Integration) | PASS |
| 3 | Malformed SLL input is a recoverable failure: returns failure without throwing, no partial document | `SllDocumentCodecTests.TryDeserialize_InvalidJson_ReturnsFalseWithoutThrowing`, `TryDeserialize_NullInput_ReturnsFalseWithoutThrowing` (Integration) | PASS |
| 4 | App layer uses the codec contract, not the Domain type, to serialize; no direct Integration construction in the App layer | `SllGenerationSessionViewModelTests` (11 tests) exercise the store/redisplay round-trip through the injected `ISllDocumentCodec`; the session VM holds `ISllDocumentCodec` and no App-layer VM constructs the Integration codec directly | PASS |

## Solution baseline

`dotnet test .\FusionCanvas.sln` → all green (Domain, Application, Integration, App test projects). The SLL-specific cases: Domain `SllDocumentTests` (4), Integration `SllDocumentCodecTests` (3), App `SllGenerationSessionViewModelTests` (11).

## OpenSpec validation

- `openspec validate extract-sll-document-codec --strict` → valid.
- `openspec validate --all --strict` → passes.

## Coordination / dependency note

This change modifies code introduced by `add-sll-generation`; it must be synced/archived after `add-sll-generation` so the synced `sll-generation` spec contains the base requirements before this codec requirement is layered. No schema or migration change; the SLL wire format is unchanged (the Integration codec uses the same default `System.Text.Json` options the Domain methods used).
