# Separate Development Data Environment Retrospective

## Outcome

Repository Development launches now resolve local workspace and settings data beneath `%LOCALAPPDATA%\FusionCanvas-Development`, while the installed application's default remains `%LOCALAPPDATA%\FusionCanvas`. No migration or copy is performed, and provider credentials continue using the existing OS-protected credential service. The full solution baseline passed.

## Feedback-Driven Adjustments

| Initial assumption | Evidence | Correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| Development and installed launches could safely share the default user data root. | The installed application was already using the same setup as the repository development environment, and the user identified that this could affect installed-user data. | Give the repository launch profile its own empty data root and preserve the installed root unchanged. | Missing requirement | Future repository development launches and local application-data composition. | Promoted as the `application-data-environments` capability spec after user confirmation. |
| Credentials might need a separate development profile along with local application data. | User confirmed OS-protected provider credentials should remain shared. | Keep the existing credential-store service and avoid credential copying or migration. | One-off preference / explicit product boundary | This capability only. | Captured in the capability spec; no broader rule needed. |

## Learning Review

- Result: no reusable lessons were found beyond the capability behavior being added.
- Evidence reviewed: approved proposal and design decisions, delta spec, implementation tasks, criterion-level verification, focused path/persistence tests, full solution baseline, and recent Git history.
- Promotions completed: capability requirements are ready to sync into the main `application-data-environments` spec.
- Deferred promotions: none.
