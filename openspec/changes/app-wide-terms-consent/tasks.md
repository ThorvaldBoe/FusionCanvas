## 1. Policy content and consent contract

- [ ] 1.1 Obtain human/legal approval for the initial FusionCanvas Terms of Use and Responsible Use Policy wording, effective version, and bundled document content; record the approved version in the change artifacts before release.
- [x] 1.2 Confirm the canonical Printify Terms, Printify Intellectual Property Policy, Shopify Terms, Shopify Acceptable Use Policy, and any currently applicable Shopify API reference URLs; keep provider text external to FusionCanvas.
- [x] 1.3 Add framework-free consent records, required acknowledgement versions, current-policy evaluation, and all-four-selections validation under a cohesive Application capability.

## 2. Application-settings persistence

- [x] 2.1 Extend `ApplicationSettings` with an optional versioned consent record and preserve compatibility for settings files that predate the field.
- [x] 2.2 Extend `JsonApplicationSettingsStore` serialization/deserialization and failure handling for consent state without storing unnecessary personal data.
- [x] 2.3 Add isolated integration tests for missing, current, stale, malformed, round-tripped, and save-failure consent settings.

## 3. Startup enforcement

- [x] 3.1 Add startup orchestration that evaluates consent before constructing or showing the normal workspace runtime and main window.
- [x] 3.2 Preserve the existing splash, cancellation, shutdown, UI-test, and saved-layout behavior after consent is current or successfully accepted.
- [x] 3.3 Add deterministic startup tests proving missing or declined consent prevents normal workspace composition and accepted consent continues startup.

## 4. Consent surface

- [x] 4.1 Add a focused Avalonia consent window and view model with bundled FusionCanvas policy content, four mandatory acknowledgement controls, descriptive provider links, responsibility disclaimer, `Agree and continue`, and `Quit FusionCanvas`.
- [x] 4.2 Implement keyboard focus order, accessible names, disabled/enabled primary-action state, progress state, error retention, cancellation, and close behavior according to the UX design.
- [x] 4.3 Persist consent only after all controls are selected and a settings save succeeds; keep the surface open with an actionable error when persistence fails.
- [x] 4.4 Add deterministic App tests for view-model validation, command state, policy presentation, link commands, save failure, and selection retention.
- [x] 4.5 Add Avalonia headless view tests for visual-tree construction, compiled bindings, checkbox gating, keyboard-reachable actions, focusable links, and visible error/progress states.
- [x] 4.6 Keep consent acceptance orchestration in the Application layer so the ViewModel only manages presentation state and result rendering.
- [x] 4.7 Use the shared semantic design tokens for consent-surface layout, typography, control spacing, state colors, and button styling.

## 5. Settings review surface

- [x] 5.1 Add a Settings legal/terms section or equivalent focused review surface showing accepted versions and UTC timestamp, with policy links and a route to reopen the consent surface when acknowledgement is missing or stale.
- [x] 5.2 Ensure Settings does not create a second persistence or acceptance path and does not unnecessarily block normal use when consent is current.
- [x] 5.3 Add App tests for current, missing, and stale acknowledgement summaries and review commands.

## 6. Documentation and acceptance verification

- [ ] 6.1 Add the approved FusionCanvas policy as a versioned bundled resource and document the provider-acknowledgement and non-legal-advice boundaries.
- [ ] 6.2 Verify every `terms-consent` acceptance scenario with a focused application, integration, startup, or headless UI test and record criterion-level evidence.
- [ ] 6.3 Verify the modified desktop-foundation startup and layout scenarios, including legacy/invalid layout fallback after consent.
- [ ] 6.4 Verify the modified application-settings scenarios for accepted, missing, and stale acknowledgement state.
- [x] 6.5 Run `openspec validate --strict` and correct any artifact or delta-spec issues.
- [ ] 6.6 Run `dotnet test .\\FusionCanvas.sln -m:1` and resolve regressions without expanding scope.
- [x] 6.7 Decide whether the optional real-desktop first-run/link journey adds information beyond deterministic tests; if warranted, run it only with disposable settings/workspace paths and record it as supplemental evidence.
