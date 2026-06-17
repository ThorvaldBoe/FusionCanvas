# FC-0303 - Bulk Metadata Editing

## Summary

Bulk Metadata Editing lets users apply shared metadata, tags, notes, or marketplace fields to multiple listings.

## User Need

Creators need to update repeated information across a batch without editing every listing manually.

## Requirements

- Users can select multiple listings.
- Users can apply shared tags, notes, or selected metadata fields.
- The product should distinguish adding values from replacing values.
- Bulk edits should make the affected scope clear.
- Risky edits should require confirmation.

## Acceptance Criteria

- A user can add a tag to multiple listings.
- A user can update a shared metadata field for selected listings.
- A user can avoid accidentally overwriting unique listing data.
- The result is visible in search, filters, and inspectors.

## Out of Scope

- Full spreadsheet editor
- AI metadata generation
- Marketplace publishing
- Undo/redo

## Related Notes

- [[Roadmap]]
- [[FC-0207 - Listing Metadata Editor]]
- [[FC-0301 - Multi-Select Operations]]
