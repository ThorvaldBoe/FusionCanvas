## Why

The Software Factory evaluates architectural compliance well, but it does not yet provide a repeatable way to identify worthwhile opportunities to turn shallow groups of modules into deeper, more cohesive modules. This change adds that design-review capability so architectural improvement recommendations are evidence-based, testable through explicit seams, and kept separate from audit findings and certification.

The capability is timely because FusionCanvas already has rules for earned abstractions, consumer-owned interfaces, dependency visibility, and lowest-reliable-layer testing. A focused deepening review can make those existing principles easier to apply consistently without importing a second external process or creating a dependency on Matt Pocock's repository.

## What Changes

- Add a Factory-native Architecture Deepening Review procedure for selected architecture scopes, changed areas, or known hotspots.
- Add shared deep-module vocabulary covering module, interface, seam, adapter, depth, leverage, locality, deletion test, and interface-as-test-surface.
- Define a durable candidate record that captures current complexity leakage, proposed seam, expected locality and leverage, dependency/adapters, test strategy, risks, and non-goals.
- Distinguish architecture improvement candidates from compliance findings, challenges, certifications, and mandatory remediation.
- Require explicit selection and design review before a candidate becomes implementation work.
- Require OpenSpec handoff for behavior changes and preserve existing Factory evidence, provenance, and audit-scope rules.
- Keep Matt Pocock's repository as optional reference material rather than a runtime or process dependency.

## Capabilities

### New Capabilities

- `architecture-deepening-review`: Evidence-backed discovery and design review of opportunities to deepen shallow modules while preserving audit and OpenSpec boundaries.

### Modified Capabilities

- None. The capability supplements existing architecture audits and does not change product behavior, certification semantics, or the meaning of existing audit axes.

## Impact

- Software Factory architecture procedures and supporting templates.
- The Factory's Architecture & Code Structure guidance, without replacing its existing rules for earned abstractions or anti-overengineering.
- New durable review records associated with a selected scope; existing audit source extracts and assessment records remain append-only and authoritative for audits.
- OpenSpec design and verification handoff for any resulting product behavior or architecture change.
- No production code, public application API, database schema, UI surface, external service, or package dependency.
