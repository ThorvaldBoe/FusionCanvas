# FC-0604 - Shopify Integration

## Summary

Shopify Integration syncs product information and publishing status with Shopify.

This capability may be delivered as a paid or optional plugin. The basic FusionCanvas listing tool should prepare local listing data and product images, but direct Shopify product creation belongs here.

## Requirements

- Users can configure Shopify connection details.
- Prepared listings can be used to create or update Shopify products.
- Shopify product IDs, URLs, and status can be recorded.
- Listing title, description, price, mockups, product images, tags, and marketplace notes can be mapped to Shopify product data.
- Users approve create or update actions.
- FusionCanvas remains the creative source of truth.

## Acceptance Criteria

- A user can create or update a Shopify product from listing data.
- The external Shopify record remains linked to the FusionCanvas listing.
- Users can see Shopify publishing status where available.

## Out of Scope

- Full storefront management
- Order management
- Customer data sync

## Related Notes

- [[Roadmap]]
- [[FC-0212 - Basic Listing Tool]]
- [[FC-0601 - Marketplace Product Records]]
- [[FC-0608 - Marketplace Validation]]
