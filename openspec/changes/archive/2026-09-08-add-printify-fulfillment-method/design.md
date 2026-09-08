## Design

Add a stable `FulfillmentStrategy.Printify` value and treat it as a Printify-connected strategy through the existing domain policy. The existing Printify credential service and editor controls remain shared; the strategy policy becomes the single decision point for whether Printify configuration is required. Shopify-only behavior remains represented by the Shopify strategy value and is not introduced for standalone Printify.

The existing store persistence path stores the enum as part of the store record. Assign the new enum value after the existing values so previously persisted values retain their meaning. No schema migration is needed.

The primary workflow is occasional store setup and configuration in the focused store editor, not the daily workspace. The selector remains the entry point; Printify controls are progressively disclosed only for the two Printify strategies. Empty, loading, verification, unavailable, and archived/read-only states continue to use the existing Printify view-model behavior. The selector and existing controls provide the relevant keyboard focus and cancellation behavior, so no new interaction surface is introduced.

## Implementation Plan

1. Update `src/FusionCanvas.Domain/Stores/FulfillmentStrategy.cs` and `FulfillmentStrategyPolicy.cs` with the stable enum value and policy membership; retain existing numeric values.
2. Update the App label converter and store editor guidance so the new option is visible and the copy distinguishes standalone Printify from Shopify + Printify.
3. Update `StorePrintifyCredentialsViewModel`, `StoreManagementViewModel`, and `StorePrintifyConfigurationService` decision points to use the policy for Printify-connected behavior and preserve strategy-change confirmation.
4. Add or adjust domain, application, and headless App tests covering option count/labels, persistence, visibility, shop selection, and leaving standalone Printify.
5. Run focused tests, the solution baseline, and strict OpenSpec validation; record criterion-level evidence in `verification.md`.

### Verification mapping

| Acceptance scenario | Verification |
| --- | --- |
| Strategy list includes standalone Printify | Domain policy test and StoreEditor headless test |
| Standalone Printify strategy persists | Store management application test |
| Standalone Printify exposes configuration | StorePrintify view-model/application and headless editor tests |
| Shopify-only strategy does not expose configuration | Existing plus updated headless editor test |
| Guidance distinguishes standalone Printify | Headless editor text assertion |
| Leaving standalone Printify requires confirmation | Headless editor workflow test |

## Decisions Not to Reopen

- The future Printify listing tool is outside this module.
- No Shopify integration or publishing behavior is added for standalone Printify.
- No credential-storage redesign or database migration is introduced.
