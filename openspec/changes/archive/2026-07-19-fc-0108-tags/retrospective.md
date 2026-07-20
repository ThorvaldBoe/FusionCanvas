# FC-0108 Tags Retrospective

## Outcome

FC-0108 delivered store-scoped tag management for listings: create, rename, recolor, archive, restore, and delete tags with atomic link cleanup; apply and remove tags on listings with create-on-the-fly; filter the workspace tree by tag with AND semantics; and render colored chips across the listing editor, tree filter, Tags tab, and listing rows. The `Tag` domain entity gained a nullable `Color` field with `#RRGGBB` normalization, persisted through a versioned SQLite 3→4 migration. The deterministic baseline (235/235 tests) passes and strict OpenSpec validation is green. Interactive desktop verification (tasks 7.2-7.4) is N/A under OpenCode per AGENTS.md and is deferred to an agent with display access.

Topic and asset tagging was intentionally deferred to a follow-up change that will generalize `ListingTag` into a generic `EntityTag` with its own migration. Color UI shipped in Phase 1 per user decision; the alternative (metadata-only color) was rejected.

## Feedback-Driven Adjustments

| Initial assumption | Evidence | Correction | Classification | Applicability | Promotion |
|---|---|---|---|---|---|
| The PRD's "apply tags to topics where useful" should ship in FC-0108. | Generalizing `ListingTag` into a generic `EntityTag` would require a separate migration and broaden the change beyond the user's immediate need. | User scoped FC-0108 to listings-only application; topic/asset tagging is explicitly deferred. | Missing requirement (scope decision) | Change-specific | Retained in proposal, design, and this retrospective; no broader promotion needed. |
| Color could live in `MetadataJson`. | The user chose to ship a color picker + colored chips in Phase 1, making a dedicated field cleaner for validation, query, and chip binding. | Added a nullable `Color` column on `tags` via schema 3→4 migration rather than overloading metadata. | Architecture decision | Change-specific | Retained in design and the local-sqlite-persistence delta spec. |
| Tag apply/remove could queue behind the listing description/notes Save. | The "Find Work by Tag" workflow needs the filter to reflect current tags immediately; tag edits are structural metadata, not draft text. | Tag apply/remove persists immediately and atomically, visually independent of the description/notes Save. | Reusable UX principle | Any surface mixing immediate structural edits with draft text edits | Retained in the listing-management delta spec; no broader promotion needed because the principle already exists implicitly in FC-0103/FC-0104 autosave guidance. |
| Permanent tag deletion should block while any link exists. | The PRD explicitly asks for safe everywhere-removal after confirmation, and requiring manual unlinking would make vocabulary cleanup impractical. | Deletion cascades link cleanup atomically after confirmation, with the warning naming the affected listing count. | Missing requirement | Change-specific | Retained in the tag-management spec. |
| OpenCode could perform interactive desktop verification. | AGENTS.md marks the desktop UI pass as N/A under OpenCode (no display). | Tasks 7.2-7.4 are recorded as not-applicable in `verification.md` and deferred to an agent with display access. | OpenSpec process rule | All OpenCode-driven user-facing changes | Already documented in AGENTS.md; no promotion needed. |

## Learning Review

- Result: no reusable lessons requiring promotion
- Evidence reviewed: final proposal, design, four delta specs (tag-management, listing-management, navigation-tree, local-sqlite-persistence), completed task checklist, `verification.md`, and the single commit `18005a0 Add tag management for listings`.
- Promotions completed: none. All reusable behavior is captured in the new `tag-management` spec and the modified `listing-management`, `navigation-tree`, and `local-sqlite-persistence` specs, which will sync to main specs during archive.
- Deferred promotions: the immediate-persistence-vs-draft-Save principle is already covered by existing autosave guidance in FC-0103/FC-0104; the OpenCode desktop-N/A rule is already in AGENTS.md; the scope and color-field decisions are change-specific and remain in this retrospective and the design.
