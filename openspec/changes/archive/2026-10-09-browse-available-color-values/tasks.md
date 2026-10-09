## 1. Color grid presentation state

- [x] 1.1 Add focused view-model tests for configured order, global search, 10/25/50 paging and counts, sort directions and ties, page reset/clamp, and Offering-context reset.
- [x] 1.2 Implement `AvailableColorValuesGridViewModel` and read-only row presentation model; connect same-Offering Color values and Offering-context changes from `CatalogSetupViewModel`.

## 2. Colors card and headless behavior

- [x] 2.1 Replace only the Color values text summary with the bounded virtual grid and accessible search, sort, page-size, count, and navigation controls; retain the current Size/custom summaries and Manage values action.
- [x] 2.2 Add rendered Light/Dark headless tests for row templates and stripes, both empty states, tooltip and accessibility names, keyboard-operable controls, data-context lifecycle, and unchanged catalog/Manage values behavior.

## 3. Acceptance verification and completion

- [x] 3.1 Review each scenario in `specs/variant-management/spec.md`; update `design.md`, `tasks.md`, or specs if implementation exposes a resolved mechanical detail, and stop if a material decision is needed.
- [x] 3.2 Record scenario-by-scenario test results and evidence in `verification.md`; run `openspec validate browse-available-color-values --strict` and correct any artifact errors.
- [x] 3.3 Run `dotnet test .\FusionCanvas.sln -m:1`, review the changed-scope diff and `git diff --check`, and record the aggregate baseline result.
