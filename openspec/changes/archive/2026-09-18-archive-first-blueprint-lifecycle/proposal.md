## Why

The Blueprint overview currently exposes a permanent-looking delete action alongside archive, while archived Blueprints are hidden from normal browsing. This makes the safe lifecycle unclear and prevents users from intentionally reviewing or permanently removing records only after they have been archived.

## What Changes

- Make archive the only destructive lifecycle action available for an active Blueprint.
- Add an opt-in **Show archived Blueprints** control in the Blueprint overview; active Blueprints remain the default view.
- Display archived Blueprints distinctly and allow a selected archived Blueprint to be permanently deleted after explicit confirmation.
- Permanently delete the archived Blueprint and its owned catalog/compatibility records atomically, while blocking deletion when external Item, listing, design, or other protected references remain.
- Remove the misleading active-state permanent-delete action and align confirmation text with the actual operation.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `blueprint-offering-list`: Define active-by-default Blueprint visibility, opt-in archived Blueprint discovery, and archived-record actions.
- `product-supplier-setup`: Define archive-first Blueprint lifecycle and safe permanent deletion of archived Blueprints.

## Impact

- Application catalog and product setup contracts/services for archived Blueprint visibility and permanent deletion.
- Store Management view-model state, selection, commands, and confirmation handling.
- Store Editor Avalonia markup for the archived filter, lifecycle labels, and archived-only delete action.
- Snapshot mutation and compatibility projection cleanup; no schema migration is expected.
- Focused application tests and Avalonia headless coverage, followed by strict OpenSpec validation and the full solution test baseline.
