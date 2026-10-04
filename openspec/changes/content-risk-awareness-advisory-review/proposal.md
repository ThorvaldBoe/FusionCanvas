## Why

FusionCanvas helps creators generate and assemble customer-facing print-on-demand content, but it currently provides no consistent awareness or review state for possible intellectual-property, harmful-content, or marketplace-suitability risks. Issue #318 asks for IP infringement awareness; discovery established that the useful, honest outcome is advisory review that highlights obvious signals and keeps users aware of non-obvious risks without claiming legal clearance.

## What Changes

- Add a cross-cutting Content Risk Awareness capability for customer-visible text and images produced or imported through FusionCanvas.
- Show a default, persistent awareness warning for relevant AI-assisted content and customer-facing uploaded artwork, including when no automated signal is found.
- Represent review results as advisory findings across separate IP-risk and safety/suitability categories.
- Preserve `Unreviewed` and `ReviewUnavailable` states; never present an automated result as safe, approved, legally cleared, or infringement-free.
- Review AI-generated text when it is applied to a customer-visible field, and review generated artwork before it is assigned to a customer-visible Design slot.
- Review uploaded artwork when it becomes customer-facing, while excluding private reference material unless its visibility makes it relevant.
- Support provider-neutral safety signals for text and images and low-cost obvious IP signals such as detected text or recognizable logos where an enabled provider supports them.
- Persist the content fingerprint, review method, findings, timestamp, and stale state so changes invalidate prior results.
- Keep trademark search, legal clearance, comprehensive copyright or visual-similarity search, automatic blocking, and marketplace certification out of scope.

## Capabilities

### New Capabilities

- `content-risk-awareness`: Advisory IP-risk, harmful-content, and marketplace-suitability awareness for customer-visible content, with persistent warnings, findings, provenance, and stale-result handling.

### Modified Capabilities

No existing capability requirements are replaced. The new cross-cutting capability adds review behavior at existing asset, Item, Design, and AI-application boundaries without changing their ownership or persistence contracts.

## Impact

- **Domain:** New immutable review-state, finding-category, and content-fingerprint value semantics; no legal-clearance or infringement verdict model.
- **Application:** Review orchestration at AI-application, artwork-assignment, and customer-facing asset boundaries; provider-neutral safety and signal-detection contracts; stale-result rules.
- **Integration:** Optional moderation, OCR, and logo-detection adapters with bounded payloads, safe failures, explicit provider/privacy configuration, and no secret or raw-content leakage in diagnostics.
- **App:** Compact awareness notices, inline findings, review-unavailable states, and progressive disclosure of evidence in Item, Design, and relevant asset/listing surfaces.
- **Persistence:** Backward-compatible metadata or records for review fingerprints and findings; existing assets, generated artwork, and Item content remain readable when checks are unavailable or the feature is disabled.
- **Dependencies:** Coordinate with the in-progress artwork-generation foundation and existing AI provider configuration, asset management, Item workflow, and local-first privacy boundaries.
- **Verification:** Framework-free policy and orchestration tests, isolated provider/metadata persistence tests, and Avalonia headless coverage for warning visibility, findings, stale states, and keyboard-accessible progressive disclosure.
