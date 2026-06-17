# FC-0702 - Performance Data Model

## Summary

Performance Data Model stores measurable marketplace and advertising signals for listings.

## Requirements

- Performance data can be associated with listings and marketplace product records.
- Data can include source, date, impressions, clicks, favorites, carts, sales, revenue, and ad spend.
- Derived metrics should be calculated where practical.
- Historical performance should be preserved.
- Data should support listing, niche, group, tag, and experiment analysis later.

## Acceptance Criteria

- FusionCanvas can represent performance history for a listing.
- Data can be connected to external marketplace records.
- Derived metrics can be calculated from stored values.

## Out of Scope

- Direct import UI
- Forecasting
- Automated recommendations

## Related Notes

- [[Roadmap]]
- [[Data Model]]
- [[FC-0701 - Manual Performance Import]]
