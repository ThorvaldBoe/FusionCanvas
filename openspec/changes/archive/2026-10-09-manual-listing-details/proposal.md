## Why

[Issue 165](https://github.com/ThorvaldBoe/FusionCanvas/issues/165) identifies the need for a listing tool beyond Printify. Creators using a non-Printify fulfillment strategy still need a durable place to prepare the public listing and record what each sellable Variant costs and earns. The Listing stage currently focuses on mockups, while Printify owns its own synchronized listing data; a local manual tool fills this gap without requiring marketplace or provider integrations.

## What Changes

- Add a local Listing details tool for non-Printify Stores, available from the Item's Listing stage alongside existing mockup preparation.
- Store customer-facing title and description separately from the Item's working title and description, using the Item values as initial drafts; reuse existing Item tags.
- Record selling price and expected fulfillment cost per configured Variant, with a shared currency and derived per-Variant margin.
- Show the Item's single selected fulfillment setup and let the user record its manual shipping terms. Changing an Item's selected Offering is an explicit migration: retain the prior setup as read-only history, copy prices/costs only for exact Variant matches, reset setup-specific shipping terms for review, and reset Offering-bound Design variant selections as the current Design behavior does.
- Gate the manual Listing details tool by the Store fulfillment strategy. Printify strategies continue to use the Printify listing workflow; the manual tool performs no external communication.
- Preserve existing Item and catalog relationships and migrate existing workspaces without fabricating listing details or prices.

## Capabilities

### New Capabilities
- `manual-listing-details`: local, Item-bound listing copy, per-Variant pricing and fulfillment/shipping details for non-Printify strategies.

### Modified Capabilities
- `store-fulfillment-strategy`: define which fulfillment strategies expose the manual Listing details tool.
- `design-area-target-selection`: route an Item's selected Offering change through a confirmed migration when manual Listing details exist.

## Impact

- **App:** Listing-stage tool selection and Item-bound editing states for title, description, per-Variant values, and the selected fulfillment setup.
- **Application and Domain:** validation and orchestration for local manual listing details, variant pricing, and strategy-based availability.
- **Integration and persistence:** workspace snapshot/database schema and migration for durable Item listing details and per-Variant prices; no external API calls.
- **Verification:** focused domain/application/persistence coverage and an Avalonia headless view test for strategy visibility and meaningful editor bindings. Real-desktop automation is not warranted for this focused local editor.

The module is limited to one active fulfillment setup per Item and local listing preparation. It does not add marketplace publishing or synchronization, Printify replacement behavior, marketplace-specific tags, order management, shipping-label purchasing, live carrier rates, inventory, or multiple concurrent fulfillment alternatives. The manual shipping record is one setup-level shipping profile with a customer-facing charge, the creator's expected shipping cost, and a delivery estimate; per-destination rate tables remain out of scope. Migration keeps the previous profile as read-only history, carries only exact semantic Variant matches, asks the user to review the new setup, and clears setup-specific shipping terms so stale rates are never reused silently.
