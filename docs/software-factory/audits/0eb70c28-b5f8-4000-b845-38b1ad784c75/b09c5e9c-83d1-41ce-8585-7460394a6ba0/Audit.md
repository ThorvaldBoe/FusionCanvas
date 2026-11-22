# Architecture & Code Structure Audit Run

- Audit ID/run ID: `0eb70c28-b5f8-4000-b845-38b1ad784c75` / `b09c5e9c-83d1-41ce-8585-7460394a6ba0`
- Status: `COMPLETE` (scoped assessment; human review pending)
- Standard / definition: FC-ARCH 2.0.0 Active / 2.0 Active
- Profile: `DEEP_CHANGE`; source revision `09d3089f734e99025056cfdfeeb36101de702d9d`; clean at audit start
- Extract: [source-extract.json](source-extract.json), revision 1, 45 changed source type entries
- Scope: PR #984 architecture-relevant changed types and direct callers, contracts, registrations, persistence, and focused tests.
- Jev: invocation `fd27a52d-8353-433f-abe2-db912ef9bfd5` failed with `CLI_ERROR`; no Jev classification accepted. Manual fallback bypass `5ff8a52b-b8bc-46c8-8dfb-f36e65803c6a`, log [jev/5ff8a52b-b8bc-46c8-8dfb-f36e65803c6a.jsonl](jev/5ff8a52b-b8bc-46c8-8dfb-f36e65803c6a.jsonl), source ID `AXIS-INVENTORY@1` for run-wide manual routing.

## Assessment

Reviewed dependency direction, responsibility placement, provider boundaries, persistence and migration, integration contracts, registration/composition, and test placement. Most changed responsibilities remain in their intended layers. One Major boundary finding remains.

| Assessment | Criterion | Result | Evidence |
|---|---|---|---|
| A-ARCH-001 | ARCH-008 provider-specific representation at application boundary | FAIL | `PrintifyCatalogImportService` serializes Printify color data into `OfferingOptionValue.MetadataJson`; `ListingColorSelectionService.ReadHex` parses that provider-shaped JSON in Application. |
| A-ARCH-002 | Domain independence and dependency direction | PASS | Domain artwork/listing concepts have no Printify or UI dependency. |
| A-ARCH-003 | Persistence, migration, and composition | PASS | SQLite migration is transactional and covered by migration persistence tests; registrations compose through existing Application contracts. |
| A-ARCH-004 | Cohesion and responsibility placement across changed workflows | PASS | Workflow coordination stays in Application, transport/persistence in Integration, and presentation in App, subject to A-ARCH-001. |

## Findings

### F-ARCH-001 — Major — provider-specific color JSON crosses the Application boundary

- Evidence: `src/FusionCanvas.Application/Stores/Printify/PrintifyCatalogImportService.cs:440-468` writes serialized Printify swatch metadata; `src/FusionCanvas.Application/Listings/ListingColorSelectionService.cs:222-235` reads the hex value from that JSON.
- Impact: Application behavior now depends on Printify's metadata shape and untyped JSON convention, complicating reuse by another provider and allowing schema drift to surface as missing preview data.
- Suggested direction: represent the swatch value in a typed application contract/domain value and keep Printify JSON translation in the provider integration boundary.
- State: Open; not fixed as part of this review.
- GitHub issue: [#986](https://github.com/ThorvaldBoe/FusionCanvas/issues/986), label `audit-finding`.

No other architecture finding was established. Assessment is limited to this PR and its direct dependency neighborhood, not a full-repository architecture certification.
