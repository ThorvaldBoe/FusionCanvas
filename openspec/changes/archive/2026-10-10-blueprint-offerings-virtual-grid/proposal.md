## Why

The Blueprint Offering list currently uses stacked cards, so fulfillment, lifecycle, setup counts, and readiness compete for vertical space and become harder to scan as the collection grows. A virtual grid will align these summaries while preserving the focused Store Editor workflow.

## What Changes

- Present Blueprint Offerings in a read-only virtual grid with aligned identity, fulfillment, status, setup, and readiness columns.
- Keep a clear one-click Open action for every Offering and preserve archived visibility, creation, and current Blueprint/Store context.
- Keep complete readiness guidance available without expanding every row; use concise cell text with full details exposed through accessible help text/tooltip.
- Do not add sorting or searching in this module.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `blueprint-offering-list`: Define the Offering collection's virtual-grid presentation and preserve discoverable open, add, archive, setup, and readiness behavior.

## Impact

- App: `StoreEditorWindow` markup and code-behind provider lifecycle; Offering row presentation values.
- App tests: deterministic Avalonia headless coverage for rendered columns, one-click open action, provider refresh, and full readiness details.
- No domain, application, persistence, database, or external API changes. No new real-desktop Appium journey is warranted: this is a focused editor collection whose binding, routed input, and layout risks are covered by headless view tests.

## Scope and Verification

This is one reviewable presentation module on the Blueprint detail surface. It depends on the existing Offering cards and selection command. Empty and archived states, the Add route, Store read-only safeguards, and the existing Offering navigation remain authoritative. Verification will combine scenario-level headless checks, the full solution test baseline, strict OpenSpec validation, and a changed-scope review.

## UX Decisions

- Primary user: a Store maintainer reviewing fulfillment choices for one Blueprint; opening an Offering is the frequent next action, while creation and archived review are occasional actions.
- Surface: keep the collection in the focused Store Editor's Blueprint detail panel.
- Open: retain a visible per-row Open button so a grid selection does not add a click to the existing navigation flow.
- Progressive disclosure: show a short readiness summary in the grid and expose the full readiness summary and guidance on the same row via accessible help text/tooltip.
- Keyboard: the Open button remains in normal tab order and invokes the same existing selection command; Add and archived visibility remain in their existing order.
- Empty/loading/error/draft/unsaved/destructive states: retain current behavior; the grid introduces no new persistence or draft state.
