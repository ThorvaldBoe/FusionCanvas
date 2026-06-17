# FC-0607 - Publishing Status Sync

## Summary

Publishing Status Sync refreshes external status, URLs, and IDs while keeping FusionCanvas authoritative.

## Requirements

- Users can refresh status for linked marketplace products.
- Sync can update external ID, URL, status, and relevant timestamps.
- Sync should not overwrite creative source data without approval.
- Sync failures should be visible and recoverable.
- Users can see when status was last refreshed.

## Acceptance Criteria

- A user can refresh marketplace status for a listing.
- FusionCanvas shows current known external status.
- Sync does not replace local creative data unexpectedly.

## Out of Scope

- Real-time sync
- Order management
- Automated conflict resolution

## Related Notes

- [[Roadmap]]
- [[FC-0601 - Marketplace Product Records]]
- [[Principles]]
