# FC-0608 - Marketplace Validation

## Summary

Marketplace Validation checks listing data, image requirements, tags, and metadata before export or publishing.

## Requirements

- Users can validate a listing against selected marketplace requirements.
- Validation can check required metadata, asset presence, image constraints, and status readiness.
- Results should identify blocking issues and warnings.
- Users can navigate from validation results to the relevant listing fields or assets.
- Validation rules can vary by marketplace.

## Acceptance Criteria

- A user can validate a listing before publishing.
- Blocking issues and warnings are clearly separated.
- Validation helps users fix incomplete listings.

## Out of Scope

- Legal compliance guarantees
- Sales prediction
- Automatic correction of all issues

## Related Notes

- [[Roadmap]]
- [[FC-0207 - Listing Metadata Editor]]
- [[FC-0601 - Marketplace Product Records]]
