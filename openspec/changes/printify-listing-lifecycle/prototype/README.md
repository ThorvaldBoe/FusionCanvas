# Printify listing lifecycle prototypes

These static screens are an exploration aid for the `printify-listing-lifecycle` change. They use the existing Fusion Canvas dark workspace direction and focus on the most important state transitions. The composition follows the existing stage-tool pattern: one main column, a compact connection-status pill, stacked label/control rows, read-only values inherited from Item/Design/Store context, editable Printify-specific values, and state-dependent actions.

1. Ready connection with an unmapped Item.
2. Saved Printify draft with update, publish, archive, and delete actions.
3. Standalone Printify strategy without Shopify publication controls.
4. Remote Printify changes requiring a decision.
5. Published Shopify state with deletion blocked.
6. Unpublished and unlocked state with deletion available.
7. Printify connection unavailable and remote actions fail-closed.

The screens are intentionally not production markup. They are meant to expose hierarchy, state communication, action ownership, disabled-action explanations, and the boundary between Design-owned inputs and Printify-owned listing synchronization.

Generate them with:

```powershell
python .\openspec\changes\printify-listing-lifecycle\prototype\generate.py
```
