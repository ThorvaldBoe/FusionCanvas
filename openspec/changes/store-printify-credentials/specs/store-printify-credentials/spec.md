## ADDED Requirements

### Requirement: Printify credential status belongs to the selected Store
For a saved active Store with Shopify + Printify selected, FusionCanvas SHALL display credential presence for that Store only. Presence SHALL remain distinct from successful verification.

#### Scenario: Store has no saved key
- **WHEN** native storage reports no key for the selected Store
- **THEN** the editor shows `Printify api key is required` in the semantic error color and an `Add` button
- **AND** Verify is hidden

#### Scenario: Store has a saved key
- **WHEN** the selected Store's key is available
- **THEN** the editor shows `Printify api key is provided`, `Manage`, and `Verify`
- **AND** it does not imply the key has been verified

#### Scenario: Credential storage cannot be read
- **WHEN** native storage is locked, denied, unavailable, or contains a malformed value
- **THEN** the editor reports a safe storage error with a retry action
- **AND** does not misreport the key as missing or verified or enable overwriting an unknown value

#### Scenario: User configures a new Store draft
- **WHEN** Shopify + Printify is selected for an unsaved Store
- **THEN** the editor explains that the Store must be saved before adding its key
- **AND** does not read or write a credential for a temporary identity

#### Scenario: Archived Store is selected
- **WHEN** an archived Store is selected
- **THEN** its configuration stays read-only and Add, Manage, and Verify are unavailable
- **AND** archiving does not remove its saved key

### Requirement: Printify keys are stored securely per Store
FusionCanvas SHALL persist each Printify key exclusively through native OS credential storage under its workspace and Store identity. It SHALL exclude keys from SQLite, ordinary settings, workspace files, exports, logs, diagnostics, and error messages, and SHALL NOT fall back to plaintext storage.

#### Scenario: User saves keys for two Stores
- **WHEN** different keys are saved for two Stores, including similarly named Stores in different workspaces
- **THEN** each Store reads and verifies only its own key across application restart
- **AND** renaming a Store does not change its credential association

#### Scenario: Workspace is exported or copied to another machine
- **WHEN** workspace data is exported or copied to another machine
- **THEN** no Printify key is included
- **AND** credentials must be configured separately on that machine

#### Scenario: Native credential write fails
- **WHEN** the native backend cannot save a key
- **THEN** the dialog remains open with a safe failure message and a retryable draft
- **AND** no plaintext fallback is written and successful save is not reported

#### Scenario: Store is deleted and another is created
- **WHEN** a Store or workspace is deleted and another Store is created
- **THEN** the new Store cannot access the deleted Store's retained native entry
- **AND** no application-wide or name-based fallback selects that entry

### Requirement: A focused dialog adds or replaces a Printify key
Add and Manage SHALL open the same owned, store-named dialog with a labeled masked key field, Save, and Cancel. The field SHALL start empty and SHALL never reveal or prefill a saved key. Credential Save SHALL be independent of Store Save and SHALL not require network verification.

#### Scenario: User adds or replaces a key
- **WHEN** the user enters a nonempty valid token value and saves successfully
- **THEN** native storage saves it for the dialog's captured Store identity, the dialog closes, and its draft is cleared
- **AND** presence is refreshed and prior verification is cleared

#### Scenario: User enters an unusable value
- **WHEN** entry is empty, whitespace-only, or contains embedded control characters
- **THEN** Save cannot persist it and the existing key stays unchanged

#### Scenario: User cancels or dismisses a draft
- **WHEN** the user chooses Cancel, presses Escape, or closes the dialog
- **THEN** a nonempty unsaved draft prompts for discard
- **AND** declining retains the dialog and draft while confirming clears the draft without changing the saved key

#### Scenario: User operates the dialog by keyboard
- **WHEN** the dialog opens or closes
- **THEN** opening focuses the masked field, all actions are keyboard-reachable, and closing returns focus to its invoking control when available

#### Scenario: Save is in progress
- **WHEN** a native credential save is in progress
- **THEN** duplicate saves, editing, and dismissal are disabled until the native operation finishes
- **AND** completion is associated with the captured Store identity

### Requirement: Verification explicitly checks the saved Printify key
Verify SHALL issue one read-only request using the selected saved Store's current key. Verification SHALL not create or modify external data, automatically retry, persist response data, remove a key, or imply Shopify connectivity or publishing readiness.

#### Scenario: Key verifies successfully
- **WHEN** the user selects Verify and Printify returns a successful valid shops response, including an empty list
- **THEN** the editor reports successful Printify key verification for that Store and exposes the returned shops as a dropdown of names and IDs
- **AND** the request uses the saved key rather than an unsaved draft

#### Scenario: User selects and persists a Printify shop
- **WHEN** the verified shops dropdown contains one or more shops and the user selects a shop
- **THEN** the selected shop ID is persisted with the Store's local context
- **AND** reopening the Store restores the selected shop without another network request

#### Scenario: Previously selected shop is absent
- **WHEN** a later verification succeeds but the previously persisted shop ID is not in the returned shop list
- **THEN** the dropdown clears the selection and reports that a new shop must be selected
- **AND** no product, order, or publishing request is made

#### Scenario: Printify strategy selection is unsaved
- **WHEN** Shopify + Printify is selected but the Store still has a different persisted strategy
- **THEN** Verify remains disabled with guidance to save the Store first
- **AND** no marketplace request is made until Shopify + Printify is persisted

#### Scenario: Printify rejects authentication or permissions
- **WHEN** Printify returns an authentication or permission rejection
- **THEN** the editor distinguishes invalid or expired key from insufficient permissions
- **AND** retains the key and offers Manage without displaying response bodies or secret material

#### Scenario: Verification cannot complete
- **WHEN** verification times out, is rate limited, fails due to network or service availability, or receives an unexpected response
- **THEN** the editor reports a safe retryable failure distinct from an invalid key
- **AND** retains the key and permits an explicit retry

#### Scenario: Context changes during verification
- **WHEN** the Store, workspace, strategy, or credential changes, or the editor closes while verification is pending
- **THEN** verification is cancelled or its obsolete result is discarded
- **AND** it cannot change the new context's status or display the old key's result as current

#### Scenario: Verification is already running
- **WHEN** verification is pending
- **THEN** a busy state is visible and duplicate Verify and Manage actions are disabled
- **AND** essential context navigation remains available and cancels the pending verification
