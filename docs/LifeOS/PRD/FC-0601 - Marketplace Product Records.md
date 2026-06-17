# FC-0601 - Marketplace Product Records

## Summary

Marketplace Product Records track external marketplace products linked to FusionCanvas listings.

## Requirements

- A listing can have one or more marketplace product records.
- Records can identify marketplace, external ID, URL, status, and notes.
- External records should not replace the FusionCanvas listing as source of truth.
- Users can review where a listing has been published or prepared.
- Records can support future sync and validation.

## Acceptance Criteria

- A user can associate a listing with an external marketplace product.
- A user can see marketplace status and links from the listing context.
- One listing can represent multiple marketplace products.

## Out of Scope

- Direct publishing
- Status sync
- Sales analytics

## Related Notes

- [[Roadmap]]
- [[Data Model]]
- [[Product]]
