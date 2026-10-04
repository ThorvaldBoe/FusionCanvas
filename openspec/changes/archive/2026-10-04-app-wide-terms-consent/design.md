## Context

Issue #319 requests a mandatory acknowledgement that users will follow relevant terms when using FusionCanvas with Printify and Shopify. FusionCanvas currently creates application services and the main window from `App.OnFrameworkInitializationCompleted`, while application preferences are loaded from the JSON-backed `ApplicationSettings` store. Workspace data is separate SQLite-backed data and is exportable as workspace packages.

The feature is app-wide rather than workspace-specific. It must establish user responsibility before the workspace/navigation surface is available, while preserving the local-first principle and avoiding claims that FusionCanvas can verify ownership, provide legal advice, or accept third-party provider agreements for the user.

The primary workflow is infrequent: a user completes it once on first launch and again only when FusionCanvas's own policy version changes. It belongs in a focused startup surface, not in the daily workspace. Settings provides a secondary review path.

## Goals / Non-Goals

**Goals:**

- Block normal workspace access until the required app-wide acknowledgements are complete.
- Present bundled FusionCanvas Terms of Use and Responsible Use content offline.
- Link to current Printify and Shopify terms and policies without copying or claiming ownership of their text.
- Use separate, explicit acknowledgements for FusionCanvas terms, Printify obligations, Shopify obligations, and content/IP responsibility.
- Persist a minimal, versioned acknowledgement record in application settings.
- Re-prompt when the FusionCanvas terms or acknowledgement policy version changes.
- Let users review the accepted versions and reopen policy documents from Settings.
- Keep all consent decisions testable without network access or an interactive desktop.

**Non-Goals:**

- Automated copyright, trademark, or other infringement detection.
- Legal advice or a guarantee that user content is lawful or non-infringing.
- Accepting Printify or Shopify contracts on behalf of the user.
- Provider publishing, account creation, marketplace compliance validation, or provider-specific enforcement.
- Storing consent in workspace metadata or carrying consent records inside workspace packages.
- Capturing identity, IP address, or other unnecessary personal data.

## Decisions

### 1. Enforce consent before normal application composition completes

The startup path will load application settings and determine whether consent is current before constructing or showing the normal main workspace. A focused consent window can be shown while the existing splash window is active. After acceptance, startup continues into the existing service and main-window composition.

This is preferable to an overlay inside `MainWindow`: it prevents workspace services, navigation, and provider-capable runtime composition from becoming available before the gate is complete, and it gives decline/quit a clear outcome.

Alternative considered: show the main window with a modal overlay. Rejected for this module because it permits the workspace runtime to be initialized before acknowledgement and complicates focus and shutdown behavior.

### 2. Use acknowledgement language for provider policies

The FusionCanvas document may use “agree” once the application terms have been approved. The Printify and Shopify controls will use “I understand that these providers' current terms and policies apply, and I agree to comply with them.” They will link to official pages and explicitly state that the acknowledgement does not replace the user's direct acceptance or account obligations with those providers.

This avoids representing FusionCanvas as a contracting party or suggesting that a local checkbox can complete an external provider agreement.

### 3. Require four separate confirmations

The form will have four mandatory controls:

1. FusionCanvas Terms of Use and Responsible Use Policy.
2. Printify terms and applicable policies.
3. Shopify terms and applicable policies.
4. User responsibility for rights, licences, permissions, trademarks, and other IP.

The primary action remains disabled until all four are selected. The form will also state that FusionCanvas is not legal advice and does not verify rights or guarantee non-infringement.

Alternative considered: one combined “I agree” checkbox. Rejected because it hides materially different responsibilities and makes the user's acknowledgement less meaningful.

### 4. Store acknowledgement in application settings

Consent is installation/user-profile state, not workspace content. The persisted record will extend the existing application-settings serialization with a nullable consent record containing document/policy versions and an accepted UTC timestamp. Missing or outdated values mean consent is required.

Workspace databases and workspace packages remain unchanged. This prevents a legal acknowledgement from being copied between machines or mistaken for creative/workspace data.

### 5. Version FusionCanvas content and acknowledgement policy separately from provider content

FusionCanvas will version its bundled terms and the set/wording of required acknowledgements. Provider terms are external and can change independently, including while the application is offline. The UI will direct users to review the current provider pages but will not claim to know their current revision automatically.

When the FusionCanvas terms version or acknowledgement-policy version changes, the gate reappears. A later module may add a provider-policy update service; this module does not.

### 6. Bundle only FusionCanvas-owned policy text

The FusionCanvas terms document is maintained as a versioned application resource and displayed locally. Provider documents remain external links to their official pages. The initial FusionCanvas document is approved by the product owner as version 0.1; it retains explicit non-legal-advice and user-responsibility boundaries, while separate legal-counsel review remains external.

## Risks / Trade-offs

- [Legal wording is incomplete or inaccurate] → Keep the approved text concise, retain the non-legal-advice boundary, and seek separate legal counsel when legal review is required.
- [Provider terms change without the app knowing] → Link to canonical provider pages, display a clear “review the current terms” notice, and version only FusionCanvas-controlled content in this module.
- [Users experience the gate as friction] → Keep the form focused, provide concise summaries plus expandable full text, support keyboard navigation, and show it only on first launch or policy-version changes.
- [Consent blocks access to local work] → Provide a clear Quit action, keep existing data untouched, and never delete, migrate, or export workspace data as part of the consent flow.
- [Settings persistence fails] → Surface a recoverable save error and do not claim acceptance was stored when it was not; the next launch must safely require acknowledgement again.
- [External policy links are unavailable offline] → Keep the FusionCanvas document fully bundled and explain that provider links require connectivity; the user can still read the local policy and exit without data changes.
- [Consent is confused with content compliance] → Use explicit language that the user remains responsible and that FusionCanvas does not verify rights or guarantee non-infringement.

## Migration Plan

1. Add the nullable consent record to application settings with backward-compatible deserialization. Existing settings files without it load as “consent required.”
2. Add the bundled FusionCanvas policy resource and version constants.
3. Add application consent state and persistence orchestration before normal workspace composition.
4. Add the startup consent surface, then continue existing startup only after successful persistence.
5. Add Settings review/status access without creating a second acceptance mutation path.
6. Verify old settings, missing consent, current consent, stale consent, save failure, cancellation, and normal startup/layout behavior.

Rollback is safe at the data level because the new settings field is nullable and additive. A binary rollback will ignore the field and retain existing workspace data; reverting the feature does not require a workspace migration.

## Implementation Plan

### Affected layers and likely types/files

- **Application:** add a focused `TermsConsent` capability containing an immutable acknowledgement record, required-version policy, consent state, and an application-facing settings contract or use-case service. Keep version comparison and “all required acknowledgements selected” rules framework-free.
- **Integration:** extend `JsonApplicationSettingsStore` and its serializable settings document with an optional consent record. Preserve unknown/legacy settings compatibility and atomic save behavior.
- **App startup:** update `App`, `AppServicesFactory`, or a dedicated startup coordinator so settings/consent are resolved before normal `AppServices` and `MainWindow` composition. Avoid creating a second startup path for UI tests; provide deterministic injection or an explicit accepted-consent test state.
- **App UI:** add a focused consent window/view model under a cohesive `Legal` or `TermsConsent` folder. Use compiled bindings, a scrollable local policy region, explicit checkbox labels, provider links, an error/progress state, keyboard focus, `Agree and continue`, and `Quit`.
- **Settings UI:** add a read-only acknowledgement summary and policy-document links. Reopening the first-run gate or changing accepted state must have one clear owner in the consent capability.
- **Content:** maintain the product-owner-approved version 0.1 bundled FusionCanvas Terms of Use and Responsible Use Policy resource.

### State and algorithms

- Load `ApplicationSettings` first.
- Compute `ConsentRequired` when no record exists, the stored FusionCanvas terms version differs from the current version, or the stored acknowledgement-policy version differs from the current version.
- Keep checkbox selections transient until the user activates the primary action.
- Validate all four selections before saving.
- Save the record atomically through the existing application-settings store.
- Only after a successful save continue startup and create/show the main window.
- If save fails, keep the consent surface open, show an actionable error, and leave the record absent or unchanged.
- If the user quits or startup cancellation occurs, do not create workspace services and do not mutate workspace data.

### UX and accessibility

- Initial focus goes to the first acknowledgement or the primary heading according to the chosen Avalonia focus pattern; tab order follows document links, checkboxes, primary action, and quit.
- Full policy text is scrollable and readable without requiring a browser.
- Links have descriptive accessible names and do not silently submit consent.
- The primary action is visibly disabled until all acknowledgements are selected.
- The save-in-progress state disables duplicate submission and exposes progress text.
- Error state preserves the user's selections so they can retry.
- Settings review does not force acceptance again unless the stored record is stale.

### Verification plan

- **Application tests:** required-version evaluation, all-checkbox validation, current/stale/missing consent, save failure behavior, and no mutation on decline/cancel.
- **Integration tests:** old settings deserialize with consent missing, consent round-trips, malformed consent falls back safely, and atomic save failure is reported.
- **App tests:** consent view-model state, command enablement, policy-link commands, error retention, and Settings summary.
- **Avalonia headless tests:** visual-tree construction, checkbox bindings, disabled/enabled primary action, focusable controls, keyboard-reachable quit/continue actions, and policy content presentation.
- **Startup tests:** missing consent stops before normal main-window composition; accepted consent preserves existing startup and layout behavior.
- **Desktop coverage:** one optional real-desktop journey is warranted for the first-run window handoff and native external-link behavior if the headless tests cannot validate it; it should use a disposable settings/workspace root and be supplemental, not the deterministic gate.
- **Baseline:** run strict OpenSpec validation and `dotnet test .\\FusionCanvas.sln -m:1` after implementation.

## Open Questions

- The product owner should confirm the final official provider URLs at implementation time, because provider policy navigation can change independently of FusionCanvas releases.
