# Extract SLL Document Codec Retrospective

## Outcome

Moves JSON (de)serialization of the SLL document out of the Domain `SllDocument` record into an Application contract (`ISllDocumentCodec`) implemented in Integration, preserving the exact wire format and round-trip behavior.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| `SllDocument` (Domain) may own `Serialize`/`TryDeserialize` | Using `System.Text.Json` in the Domain layer violates the Domain-purity architecture rule | Remove serialization from `SllDocument`; move it to an Application port + Integration adapter, preserving the wire format | Architecture lesson | Reusable scope | Addressed per the architecture-guidelines Domain-purity rule; a durable `sll-generation` requirement added |

## Learning Review

- Result: reusable lessons identified
- Evidence reviewed: QA-2 finding, `SllDocument` (Domain) and its callers, the new `ISllDocumentCodec`/`SllDocumentCodec`, relocated tests.
- Promotions completed: the codec-boundary requirement captured in the `sll-generation` spec; the general rule already lives in `architecture-guidelines`.
- Deferred promotions: none.
