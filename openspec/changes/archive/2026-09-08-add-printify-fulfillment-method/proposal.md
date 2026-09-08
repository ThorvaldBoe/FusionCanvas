## Why

Creators who use Printify with a storefront FusionCanvas does not integrate (for example Etsy) cannot configure Printify as their store's fulfillment method. The existing Shopify + Printify mode couples Printify configuration to Shopify, so a standalone Printify option is needed now to support the already available credential and shop-selection flow.

## What Changes

- Add a persisted `Printify` fulfillment strategy alongside Manual, Shopify + Manual, and Shopify + Printify.
- Show the existing Printify credential, verification, and shop-selection controls for both Printify strategies.
- Keep Shopify-specific behavior and wording scoped to Shopify strategies; standalone Printify must not enable Shopify integration or publishing.
- Preserve the existing confirmation behavior when changing away from a strategy that uses Printify configuration.

The module does not implement the future Printify listing tool or any Printify publishing operation.

## Capabilities

### New Capabilities

- `standalone-printify-fulfillment`: Store strategy selection and Printify configuration behavior for standalone Printify stores.

### Modified Capabilities

- None.

## Impact

- Domain strategy enum and policy.
- Store editor presentation and strategy transition handling in the App layer.
- Existing Printify application service behavior and focused domain/application/UI tests.
- No database migration is expected because the strategy is persisted through the existing enum value and store persistence path; the new value must be assigned without changing existing values.
