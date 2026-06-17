# FC-0805 - Event-Driven Automation

## Summary

Event-Driven Automation triggers actions when listings, assets, mockups, prompts, or marketplace products change.

## Requirements

- Users can define automation triggered by supported events.
- Triggers can respond to changes such as asset import, status change, or product publish.
- Automation should be visible and controllable.
- Users can enable, disable, or review automations.
- Actions should not surprise users or publish externally without approval.

## Acceptance Criteria

- A user can define an automation based on a supported event.
- Automation runs when the event occurs.
- Users can review or disable automation behavior.

## Out of Scope

- Full scripting platform
- External webhooks as a primary workflow
- Autonomous marketplace publishing

## Related Notes

- [[Roadmap]]
- [[FC-0506 - Plugin Event Hooks]]
- [[FC-0804 - Automation Recipes]]
