## Why

The Store Editor's Printify import currently presents each product created in the selected shop as a separate choice, even when several products use the same Blueprint. This makes catalog setup look like listing import and can create repeated local Blueprints while omitting provider variants that are not enabled on an individual shop product.

## What Changes

- Group the selected Printify shop's products by stable Blueprint identity and present each distinct Blueprint once for selection.
- For each selected Blueprint, import the distinct Print Providers used by that Blueprint in the selected shop.
- For each imported Blueprint/Provider offering, import the provider's complete supported sellable Variant set and corresponding Option choices, using Printify's explicit combinations rather than generating combinations from option values.
- Keep the workflow in the Store Editor's Catalog & mockups surface and preserve Store scoping, explicit confirmation, cancellation, and draft-safety behavior.
- Keep mockup-template readiness rules out of this module. Full variant coverage may increase template setup work; template validation and coverage are a separate follow-up.
- Do not automatically merge or delete existing local catalog records created by earlier product-level imports.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `printify-catalog-import`: Select unique shop-used Blueprints, import only Providers used by those Blueprints in the shop, and include each selected Provider's full supported sellable Variant set.

## Impact

The Printify catalog import integration and application mapping will change from shop-product identity to Store-scoped Blueprint and Provider identity. The Store Editor selection surface will display unique Blueprints and summarize their used Providers and supported Variant counts. Application and Integration tests will cover grouping, provider discovery, complete option/variant mapping, repeated import, and safe preservation of existing local records. No new external dependency or database migration is expected. The primary workflow remains an occasional setup task in the focused Store Editor, not a regular workspace action.
