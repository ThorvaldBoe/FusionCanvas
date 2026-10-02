## Why

The accepted Ideation specification still describes the retired environment-placeholder gate, while production composition already resolves availability from the secure OpenRouter credential and effective Ideation profile. This small reconciliation keeps the accepted contract, implementation, and regression evidence aligned without archiving the broader integration change whose external credential-smoke gate and human acceptance remain pending.

## What Changes

- Remove the accepted placeholder-access requirement.
- Add the secure OpenRouter credential and effective Ideation profile availability requirement and scenarios.
- Preserve an explicit scenario proving that `FUSIONCANVAS_AI_API_KEY` does not enable production Ideation.

## Scope

- Replace the accepted Ideation availability requirement with the secure OpenRouter/profile contract already implemented in production.
- Preserve explicit non-authority of `FUSIONCANVAS_AI_API_KEY`.
- Add a focused regression test that guards the accepted specification and production composition boundary.

## Non-goals

- No runtime behavior change.
- No changes to credential storage, provider calls, or AI settings.
- No synchronization or archival of the broader `integrate-ideation-openrouter-snowclones` change.

## Verification

- Run the focused App composition/specification regression test.
- Run strict OpenSpec validation for this change and the repository.
- Run the affected App test project and solution build.
