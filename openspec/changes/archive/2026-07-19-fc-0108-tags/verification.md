## Verification

### Build and Test Baseline

- **Build**: `dotnet build .\FusionCanvas.sln` — 0 errors, 63 pre-existing xUnit analyzer warnings (unchanged from baseline).
- **Deterministic tests**: `dotnet test .\FusionCanvas.sln` — 235/235 passed.
  - Domain: 20/20
  - Application: 96/96 (added 21 tag-management service tests)
  - Integration: 23/23 (added 3 SQLite color/migration tests)
  - App: 96/96 (added 4 tree tag filter tests, 5 listing tag editor tests, 3 Tags tab tests, 2 listing row chip tests)
- **OpenSpec validation**: `openspec validate --changes --strict` — 3/3 changes pass.

### OpenSpec Coverage

Every tag-management scenario is exercised by automated tests:

- Create: `TagManagementServiceTests.CreateTagAsync_*` (3 tests)
- Rename/recolor/description: `TagManagementServiceTests.UpdateTagAsync_*` (2 tests)
- Archive/restore: `TagManagementServiceTests.ArchiveRestoreAsync_*`, `RestoreTagAsync_BlocksActiveNameConflict`
- Permanent deletion with link cleanup: `TagManagementServiceTests.DeleteTagAsync_*`
- Apply/remove with cross-store, duplicate, archived-listing rejection: `TagManagementServiceTests.ApplyTagAsync_*`, `RemoveTagAsync_*`
- Create-on-the-fly: `TagManagementServiceTests.ApplyOrCreateTagAsync_*` (3 tests)
- Vocabulary lookup: `TagManagementServiceTests.GetActiveTagVocabularyAsync_*`, `GetListingTagsAsync_*`
- Atomic persistence + rollback: `TagManagementServiceTests.Mutations_PropagateRepositoryFailuresForUiRollback`
- Tree tag filter (AND, ancestor preservation, filtered-out indicator, keyboard-reachable clear): `WorkspaceTreeViewModelTests.TagFilter_*` (4 tests)
- Listing tag editor (autocomplete, create-on-the-fly, archived-tag prompt, keyboard flow, immediate persistence, archived-parent blocking): `ListingManagementViewModelTests.TagEditor_*` (5 tests)
- Tags tab (open, active/archived separation, empty state, dirty unsaved changes, deletion confirmation + listing count, restore conflicts, color picker validation): `StoreManagementViewModelTests.TagsTab_*` (3 tests)
- Colored chips on listing rows (overflow, null color default): `WorkspaceTreeViewModelTests.ListingNodes*` (2 tests)
- SQLite migration 3→4, color round-trip, invalid color, newer-schema refusal: `SqliteWorkspaceRepositoryTests.SaveAndLoadAsync_RoundTripsTagColor`, `LoadAsync_UpgradesVersion3DatabaseTo4PreservingTags`, `SaveAsync_RejectsInvalidTagColor`, `LoadAsync_WhenDatabaseVersionIsNewer_ThrowsClearError`

### Desktop UI Verification

Per `AGENTS.md`, OpenCode cannot perform interactive desktop verification (no display). The desktop UI pass for FC-0108 is **not applicable** under OpenCode. The fast deterministic baseline above covers all tag-management scenarios through code-level tests. Interactive desktop verification (tasks 7.2, 7.3, 7.4) should be performed by an agent with display access (e.g., Codex) using a disposable workspace database.

### Tested Build/Environment

- Build: .NET 10, Avalonia, Microsoft.Data.Sqlite, xUnit v3.
- Environment: Windows + PowerShell, OpenCode (no display).
- Date: 2026-07-19.

### Scope Confirmation (Task 7.5)

FC-0108 adds no topic or asset tagging, no global tags, no tag hierarchy, no automation, no analytics, no marketplace keyword optimization, no AI tag suggestions, no required tag schemas, no saved tag views, and no bulk tag operations. Topic/asset tagging is explicitly deferred to a follow-up change that will generalize `ListingTag` into a generic `EntityTag`.
