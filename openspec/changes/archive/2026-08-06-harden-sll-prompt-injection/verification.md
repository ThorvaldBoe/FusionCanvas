# Harden SLL Prompt-Injection — Verification

## Status

Complete. The SLL generation system message now states that all supplied workspace and user content is untrusted creative material, never to be obeyed as instructions, with the output rules taking precedence. Build is warning-clean, the full solution baseline passes, and strict change/repository OpenSpec validation pass.

## Acceptance evidence

| # | Acceptance scenario (specs/sll-generation/spec.md) | Passing automated evidence | Result |
|---:|---|---|---|
| 1 | System message bounds supplied content as untrusted data: states content is untrusted creative material, not instructions, and output rules take precedence | `SllGenerationServiceTests.GenerateAsync_SystemMessageBindsUntrustedContent` (asserts `untrusted`, `instructions`, and `output rules` in the SLL system message) | PASS |
| 2 | Untrusted-content boundary does not replace operational/secret exclusion | `SllGenerationServiceTests.GenerateAsync_AdversarialMetadata_ExcludesOperationalKeys` (operational/secret keys still absent) and `GenerateAsync_CapturedRequest_...NoOperationalFields` | PASS |

## Solution baseline

`dotnet test .\FusionCanvas.sln` → all green. Application `SllGenerationServiceTests`: 14 passing (including the new bounds test).

## OpenSpec validation

- `openspec validate harden-sll-prompt-injection --strict` → valid.
- `openspec validate --all --strict` → passes.

## Coordination / dependency note

Depends on `add-sll-generation`; archive after it. No payload-structure, purpose-routing, output-format, or persistence change — only the system prompt gains the instruction/data boundary rule.
