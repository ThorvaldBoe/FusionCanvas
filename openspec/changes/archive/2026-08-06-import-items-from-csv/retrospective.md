# Item CSV Import Retrospective

## Outcome

Adds an `Import…` action on niche and group context-menu rows that opens a dialog (file pick, raw source, preview, syntax check) and creates Design items at the targeted niche/group from a seven-column semi-colon CSV using standard quoting, with correct stage selection, inherited-context metadata, and tag creation/linking.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| Import should use a `;;`-escape format and treat empty middle fields as unrepresentable | A real exported file with an empty Notes field mis-parsed (column shift) and the two codecs were incompatible | Reconcile import to standard CSV quoting matching export; consecutive separators are empty fields, so exported rows round-trip | Implementation defect | Reusable scope | Captured in `item-csv-import` spec (standard quoting, empty fields in any column) |
| Error reporting could be a bare `Error on line N` | Users need to know which field and why | Detail the error as `Line N: <reason>` naming the column and the problem | Missing UX | Reusable scope | Captured in the delta spec (detailed error message) |
| Import codec was constructed in the App view model | Couples App to Integration | Inject `IItemCsvCodec` at the composition root | Architecture lesson | Reusable scope | Addressed in the app-layer architecture fix |

## Learning Review

- Result: reusable lessons identified
- Evidence reviewed: QA review of the 6 features, `item-csv-import` delta + verification, `ItemCsvImportService`, import `ItemCsvCodec`, and import tests; the reconciliation with item export and the improved error reporting.
- Promotions completed: standard-CSV round-trip and detailed error requirements captured in the accepted spec.
- Deferred promotions: none.
