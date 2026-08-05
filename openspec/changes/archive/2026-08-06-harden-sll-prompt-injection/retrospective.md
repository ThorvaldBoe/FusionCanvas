# Harden SLL Prompt-Injection Retrospective

## Outcome

Adds an explicit instruction/data boundary to the SLL generation system prompt: all supplied workspace and user content is untrusted creative material that must not be interpreted as instructions, and the output rules always take precedence. Pinned by a system-message test.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| The SLL AI service should follow the same prompt-injection hygiene as Title/ConceptRefinement | QA flagged the SLL system prompt lacked the untrusted-content guard and had no test pinning it | Add a system-rule (untrusted content, output rules win) and a `GenerateAsync_SystemMessageBindsUntrustedContent` test | Security / missing requirement | Reusable scope | Captured in the `sll-generation` delta spec |

## Learning Review

- Result: reusable lessons identified
- Evidence reviewed: QA-1/QA-4 findings, `SllGenerationService` system prompt, sibling service guards, and the new test.
- Promotions completed: the untrusted-content boundary captured in the `sll-generation` spec.
- Deferred promotions: none.
