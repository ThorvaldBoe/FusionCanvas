# FC-0107 - Basic Search and Filtering

## Summary

Basic Search and Filtering helps creators find relevant stores, niches, groups, listings, tags, and assets without losing the structure around them.

The goal is practical workspace focus, not advanced search. A creator should be able to narrow the visible workspace by remembered text, status, tag, or location and still understand where each result lives.

## User Need

As a Print on Demand creator, I need to quickly find relevant work by text, status, tag, or location so I can act on the right listings without manually browsing every folder.

## Goals

- Make work easy to find as the workspace grows.
- Support filtering by practical workflow signals.
- Provide filtering power tools above the navigation pane.
- Preserve enough hierarchy to understand result context.
- Keep filtering understandable and reversible.
- Avoid complex search behavior in Phase 1.

## Requirements

- The user can search by text across important visible fields.
- The user can filter by store.
- The user can filter by niche.
- The user can filter by group or subtree where useful.
- The user can filter by workflow stage, such as Idea, Concept, Design, Listing, or Archive.
- The user can filter by listing lifecycle status where it differs from core workflow stage.
- The user can filter by tag.
- Search and filters should work across topics and listings where relevant.
- Filtered results should preserve enough parent context to show where matching work lives.
- The user can clear filters and return to normal browsing.
- Rejected or archive-stage work should not clutter normal results unless included intentionally.

## Searchable Content

Phase 1 search should focus on content users are likely to remember:

- store names
- niche names
- group names
- listing titles or working titles
- listing ideas
- phrases
- graphic direction
- notes
- tags
- basic asset names

## User Workflows

### Search by Remembered Text

The user searches for a word or phrase remembered from a listing title, idea, phrase, note, or asset name.

Results should help the user find the relevant work quickly.

### Filter by Stage or Status

The user filters listings to focus on a workflow stage, such as ideas, concepts, designs, listings, archived work, or rejected work.

### Filter by Tag or Location

The user narrows work by tag, niche, group, or subtree to focus on a theme, campaign, or production area.

### Filter From Navigation Tools

The user uses controls above the navigation pane to filter by free text, stage, topic/subtopic, or tag without leaving the current workspace.

## Acceptance Criteria

- A user can find a listing by remembered words from its title, phrase, idea, or notes.
- A user can filter to see listings in a specific status.
- A user can filter by core workflow stage.
- A user can filter to only the current topic or subtree.
- A user can filter by tag and still understand each result's location.
- A user can narrow work without turning navigation into a disconnected flat list.
- A user can clear filters and return to normal browsing.

## Out of Scope

- Advanced query language
- Ranking or relevance scoring
- Semantic search
- AI search
- Cross-workspace search
- Saved views
- Bulk actions from search results

## Open Questions

- Should archived and rejected work be excluded by default?
- Should multiple filters combine with AND behavior, OR behavior, or simple user-selected behavior?
- Should asset names be searched in Phase 1 or deferred until asset workflows mature?

## Related Notes

- [[Phase 1 - MVP Creative Workspace]]
- [[Roadmap]]
- [[Product]]
- [[Data Model]]
- [[FC-0008 - Workflow Stage Navigator]]
- [[FC-0005 - Navigation Tree]]
