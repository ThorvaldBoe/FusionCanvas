# Prototype Avalonia UI Description Language Retrospective

## Outcome

The current approved outcome is a deterministic UI-description prototype tested against two complementary, user-approved Issue #185 wireframes before any commit or decision about AXAML generation.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| One existing Rejected Phrases screen was enough to prove the initial vocabulary. | The user requested a round-trip test against a couple of actual wireframes, specifically Variant Management, before committing to the approach. A single implementation-derived fixture cannot show whether the language preserves independent design intent or generalizes across layout families. | Replace the single Rejected Phrases fixture with the approved Variant Management v1 and Design Areas v2 source wireframes, preserve reference copies, add only vocabulary demonstrated by them, generate SVGs, and compare composition side by side. | Missing acceptance/design requirement | Reusable scope for UI-language evaluation | Keep change-local until the spike establishes whether two-source validation should become durable UI guidance. |

## Deferred or Change-Specific Notes

- The Issue #185 thread history was temporarily unavailable through thread inspection, so the source image paths were recovered read-only from its local Codex session record and then visually verified.
- Exact pixel matching remains outside scope; recognizable major regions, ordering, grouping, relative prominence, and list-or-editor relationships are the comparison contract.
- The visual round trip preserved both major compositions using the same vocabulary and renderer. Design Areas fit the model cleanly. Variant Management exposed a useful boundary: its source includes a `Done` action clipped below the main panel, while version 1 requires viewport containment and has no scrolling or overflow semantics. A follow-up should resolve that concept and test an interaction-heavy screen before attempting AXAML generation.
