## Why

FusionCanvas needs a reliable automated testing baseline before Phase 1 features begin depending on the Phase 0 domain, application, persistence, and workflow foundations. Establishing the baseline now makes testing a normal part of development and gives contributors confidence that foundational behavior can evolve without silent regressions.

## What Changes

- Define the initial automated testing approach for FusionCanvas.
- Establish which foundational behaviors should receive focused tests first.
- Clarify expectations for unit tests, integration-style boundary tests, and lightweight application behavior tests.
- Make the test suite consistently runnable by contributors.
- Set the Phase 1 expectation that new foundational behavior generally includes relevant automated tests.
- Keep full UI automation, complete end-to-end coverage, performance testing, marketplace integration testing, visual regression testing, and manual QA process definition out of scope.

## Capabilities

### New Capabilities
- `testing-baseline`: Defines the automated testing baseline, priority coverage areas, run expectations, and contribution expectations for foundational FusionCanvas behavior.

### Modified Capabilities

## Impact

- Affected code: test project structure and initial tests under `tests/`, with focused coverage for domain, application, persistence boundary, navigation, and specification-driven acceptance behavior as those areas exist.
- Affected specs: a new `testing-baseline` capability will become accepted behavior after implementation and archive.
- Dependencies: may add or standardize test framework packages already appropriate for .NET/Avalonia projects; no external services, marketplace SDKs, AI providers, or UI automation frameworks are required.
