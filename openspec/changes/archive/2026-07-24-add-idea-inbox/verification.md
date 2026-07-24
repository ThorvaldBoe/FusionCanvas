# add-idea-inbox Verification

## Automated regression pass

- `dotnet test FusionCanvas.sln --nologo -v q`: 329 passed, 0 failed (Domain 28, Application 136, App 136, Integration 29).
- `openspec validate add-idea-inbox --strict`: passed.

Coverage notes:

- Audience: application-service round-trip, omission when empty (no fabricated `idea.audience` key), preservation across unrelated edits, unknown-key preservation (service tests); view-model load/edit/commit and read-only inactive state (view-model tests); SQLite persistence and complete reload through the existing schema (integration tests — no migration, metadata keys only).
- Idea-stage triage: promotion through the existing adjacent stage controls, reject through the existing `Rejected` lifecycle status, and archive through the inspector lifecycle action are covered by the existing listing-lifecycle and inspector suites; this change adds no competing controls.

## Desktop interaction pass

Not applicable: implemented under OpenCode, which has no interactive desktop session (no display). Per AGENTS.md and the testing baseline the desktop pass is optional and non-blocking under OpenCode and is recorded as not applicable rather than passed. A manual desktop spot-check is recommended before archiving: audience edit with field-exit save, idea-stage emphasis, promote/reject/archive through the existing controls, and idea review via the stage and status filters.
