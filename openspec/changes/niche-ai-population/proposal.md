## Why

Creating a niche currently requires manually drafting several supporting fields after entering the niche name. This is repetitive and makes it harder to establish a coherent, useful niche context before creative work begins. A user-invoked AI suggestion action can reduce that setup effort while preserving creator review and control.

## What Changes

- Add a `Populate` action beside the niche name field in the focused Store Management → Niches editor.
- Enable the action only for non-archived niche drafts or selected niches with a non-blank name and ready General AI configuration.
- Ask the General AI model for independent suggestions for the blank Description, Audience, Humor style, Visual style guidance, Constraints, and Notes fields.
- Leave non-blank fields untouched and do not generate or replace Risks or Research notes.
- Treat suggestions as editable in-memory draft values; never save them automatically.
- Prompt visual-style suggestions toward typical print-on-demand t-shirt graphics that are wearable, legible, scalable, and suitable for common DTG or screen-print workflows.
- Surface loading, unavailable, failure, and recovery states without changing the existing manual niche workflow.

## Capabilities

### New Capabilities

- `niche-ai-population`: User-invoked, General-model AI suggestions for blank niche context fields with editable draft-only application.

### Modified Capabilities

None. Existing niche persistence and manual editing behavior remain unchanged; the new action is specified as a separate capability.

## Impact

- Adds an Application-layer niche suggestion use case over the existing AI text-generation boundary.
- Extends Store Management presentation state and the focused niche editor UI with a command, availability state, busy state, and recoverable error guidance.
- Reuses the existing General AI settings, credential, model catalog, privacy, and provider pipeline; no new AI settings profile or persistence schema is required.
- Adds focused Application, App/view-model, and Avalonia headless coverage. Real-desktop Appium coverage is not warranted for this deterministic editor action because no native OS behavior is involved and the external AI boundary can be tested with fakes.
- No production implementation is included in this change package; it is intended to be applied later through the OpenSpec implementation workflow.
