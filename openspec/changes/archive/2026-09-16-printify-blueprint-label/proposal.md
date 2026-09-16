## Why

The Printify import picker currently uses the shop product title as its primary label, which describes the creator's listing but not the physical product it is built on. Creators need to recognize the fulfillment blank quickly, such as “Gildan 64000,” while still being able to distinguish products that share that Blueprint.

## What Changes

- Enrich imported shop-product summaries with the Blueprint brand and model when Printify provides them.
- Display the Blueprint name (brand plus model) as the primary label in the Printify import picker.
- Keep the shop product title available as secondary context and retain a deterministic fallback when brand/model data is incomplete.
- Preserve opaque product selection IDs, import mapping, idempotency, and existing loading/error behavior.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `printify-catalog-import`: The product-selection surface identifies each shop product primarily by the Blueprint it is built on, with the product title as supporting context.

## Impact

The Application Printify summary contract, Integration shop-product parser, App import item view model/XAML, and focused Application, Integration, and Avalonia headless tests are affected. No persistence schema or imported-record identity changes are required. The existing shop-product endpoint remains the source; when its payload does not include brand/model, the client may use the existing Blueprint detail lookup boundary or a safe fallback according to the implementation design.
