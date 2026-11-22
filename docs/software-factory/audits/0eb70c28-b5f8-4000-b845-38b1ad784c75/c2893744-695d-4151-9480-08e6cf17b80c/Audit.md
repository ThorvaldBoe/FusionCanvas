# UX Audit Run

- Audit ID/run ID: `0eb70c28-b5f8-4000-b845-38b1ad784c75` / `c2893744-695d-4151-9480-08e6cf17b80c`
- Status: `COMPLETE` (scoped assessment; one finding; human review pending)
- Standard / definition: UX 1.1 Active / 1.2 Active
- Profile: `DEEP_CHANGE`; source revision `09d3089f734e99025056cfdfeeb36101de702d9d`; clean at audit start
- Extract: [source-extract.json](source-extract.json), revision 1, 4 changed workflows
- Jev: manual fallback bypass `f467cb9e-629e-4aeb-85f9-4492ce3b52e2`, log [jev/f467cb9e-629e-4aeb-85f9-4492ce3b52e2.jsonl](jev/f467cb9e-629e-4aeb-85f9-4492ce3b52e2.jsonl), source ID `AXIS-INVENTORY@1` for run-wide manual routing. Jev was unavailable; no classification was inferred.

## Assessment

Reviewed artwork variant generation, selection of Listing Colors, Printify catalog setup, and listing publication/import as separate user workflows. The design/listing separation follows the user's approved direction: artwork contrast variants are color-independent; provider colors and mockups are handled at listing time. No concern found in the stage separation itself.

| Assessment | Workflow | Result | Evidence |
|---|---|---|---|
| A-UX-001 | Create/edit artwork variants | PASS | Design workflow offers light/dark contrast intent and alternate artwork variants without assigning shirt colors. |
| A-UX-002 | Select favorite and additional Listing Colors | FAIL | More Options dialog uses the filtered nonfavorite collection, while the acceptance scenario calls for every configured Color including favorites. See F-UX-001. |
| A-UX-003 | Configure provider catalog | PASS | Color swatches/stock availability are set up in store configuration and later consumed by listing workflow. |
| A-UX-004 | Create, update, or import a listing | PASS | Listing details and mockup choices remain downstream of artwork creation; tests and specs cover the main transitions. |

## Findings

### F-UX-001 — Medium — More Options excludes favorite Colors

- Evidence: `src/FusionCanvas.App/StageTools/ListingColorOptionsWindow.axaml:21` binds the list to filtered “other” colors; `PrintifyListingStageToolViewModel.cs:173,192,382-395` builds that collection separately from `ListingFavoriteColors`.
- Expected: More Options should present the complete active configured Color collection so a user can inspect a favorite or nonfavorite and select several together.
- Observed: favorite Colors are available in the main listing surface but absent from the More Options dialog.
- Impact: Users cannot compare all candidates in one place or select favorite and additional colors in a single dialog operation.
- State: Open; not fixed as part of this review.
- GitHub issue: [#985](https://github.com/ThorvaldBoe/FusionCanvas/issues/985), label `audit-finding`.

## Challenge

`CH-UX-001` — The delta requirement says “other active configured Color Values,” while its acceptance scenario says “every active configured Color Value,” including favorites. Earlier user discussion described More Options as showing all colors from the provider. Disposition: evaluate the implementation against the explicit full-collection scenario and record F-UX-001; a future OpenSpec clarification should reconcile the wording. The audit did not edit accepted specs.
