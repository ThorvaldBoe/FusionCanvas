## Why

Store fulfillment setup cannot currently save a Printify token or check whether it works. Creators need store-specific credentials before configuring Shopify + Printify, without placing secrets in their workspace database or exports.

## Origin

Primary issue: [#320 — Printify api token storage support](https://github.com/ThorvaldBoe/FusionCanvas/issues/320).

Discovery correction: the selectable strategies are Manual, Shopify + Manual, and Shopify + Printify. Only Shopify + Printify requires a Printify key. The Printify token is account-wide at the provider, but FusionCanvas continues storing a copy per Store as an intentional best-practice workflow; the same account token may be entered for multiple Stores.

## What Changes

- Enable all three existing strategies with friendly display names while preserving their persisted numeric values.
- Under Shopify + Printify, show the requested missing/provided label and Add or Manage button; show Verify when a saved key is available.
- Add a focused, store-named key dialog with masked replacement entry, Save, and Cancel. Never reveal or prefill the stored secret.
- Store keys in the native OS credential store under workspace/store identity, separate from OpenRouter credentials, SQLite, settings, and workspace transfer files.
- Verify the saved key through an explicit, read-only Printify request, with safe success, invalid-key, permission, timeout, cancellation, and service-error states.
- After verification, retrieve the account's shops and show their IDs and names in a Store-scoped dropdown; persist the selected shop ID with the Store.
- Preserve local catalogs and keys across strategy changes, and warn before a saved strategy transition disables Printify configuration.

## Capabilities

### New Capabilities

- `store-printify-credentials`: Store-scoped secure key management and explicit verification.

### Modified Capabilities

- `store-fulfillment-strategy`: Enable the planned strategy set, define credential applicability, and make transition behavior concrete.

## Impact

Domain strategy policy; Application credential contracts and use cases; Integration native credential and HTTP adapters; App Store Editor, focused dialog, composition, and tests. Reuse the existing credential library after inspecting its supported-platform behavior. No database schema or enum-value migration is needed. No global AI credential refactor is included.

## Module Scope and UX

One outcome: configure and verify the selected store's Printify access. The selector, credential dialog, secure storage, and verification share the same store identity and acceptance fixtures, making this one coherent module.

This is occasional administration in the existing Store Editor fulfillment section; it adds no persistent main-workspace controls. New stores must be saved before a key can be added. Store strategy can be saved without a key or successful online verification; missing credentials are a configuration warning, not a barrier to local work. Token Save is independent of Store Save and works offline.

Non-goals: Shopify credentials or communication, catalog synchronization, publishing, orders, scheduled verification, key reveal/copy, and a general credential-management UI. Key removal and automatic cleanup on store/workspace deletion are deferred; retained native entries cannot be selected by another store and may be removed using OS credential management. Archiving or changing strategy retains the key and selected shop ID.

## Dependencies, Risks, and Verification

Depends on stable workspace/store identifiers, existing store save/draft behavior, the native credential library already used by OpenRouter, and Printify's read-only shops endpoint. Risks are cross-store key access, accidental secret exposure, stale asynchronous results, native storage failures, and changed strategy acceptance. Native storage failure must never fall back to plaintext persistence.

Use focused Domain/Application tests, isolated native-adapter and fake-HTTP tests, persisted strategy regression tests, and deterministic Avalonia headless tests for binding, masking, focus, and command states. Map each scenario in verification.md; completion requires the solution test baseline, build, strict OpenSpec validation, and scoped QA. Live accounts and interactive desktop checks are optional supplemental evidence.

## Review Status

Approved by the user on 2026-09-07: “Please go ahead and implement it.” Implementation follows this delivery package.
