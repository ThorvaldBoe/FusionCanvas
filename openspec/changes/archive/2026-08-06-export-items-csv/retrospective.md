# Items CSV Export Retrospective

## Outcome

Adds an `Export to CSV...` action on niche and group rows that projects active, non-empty items in the row's subtree to a UTF-8, semi-colon-delimited CSV with seven columns (Title;Base Idea;Concept Idea;Phrase;Graphic;Notes;Tags) using standard field quoting, via a file picker and an Application codec.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| Export could reuse a `;;` escaping convention | Import was later reconciled to standard double-quote quoting so the two codecs round-trip | Standard CSV quoting (quote fields containing `;`/`"`/CR/LF, double embedded quotes) is the single shared format | Missing requirement / consistency | Reusable scope | Captured in `items-csv-export` spec; import spec reconciled to match |
| A codec is needed by the App export path | App view model constructed the Integration codec directly, coupling App to Integration | Inject `IItemCsvCodec` at the composition root; App view model uses an App-local null-object default | Architecture lesson | Reusable scope | Addressed in the app-layer architecture guidance / fix routing |

## Learning Review

- Result: reusable lessons identified
- Evidence reviewed: QA review of the 6 features, `items-csv-export` + `group-management` deltas + verification, `ItemCsvExportService`, `ItemCsvCodec`, and export tests; the CSV format reconciliation with item import.
- Promotions completed: shared standard-CSV quoting captured in the accepted spec.
- Deferred promotions: none.
