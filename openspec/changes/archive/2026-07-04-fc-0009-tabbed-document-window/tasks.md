## 1. Presentation State

- [x] 1.1 Add a UI-owned document context model with id, title, optional navigation location, workflow stage, and detail view key.
- [x] 1.2 Add a document window state or view model that tracks open tabs, the active tab, and the active document context.
- [x] 1.3 Implement open-context behavior that creates a new tab for new contexts and activates the existing tab for duplicate contexts.
- [x] 1.4 Implement tab selection behavior that updates the active document context.
- [x] 1.5 Implement close-tab behavior for inactive tabs, active tabs with neighbors, and the final remaining tab.

## 2. Workspace Layout

- [x] 2.1 Update the main workspace view to include a document window region with a tab strip, workflow area, and larger detail area.
- [x] 2.2 Bind tab titles and active-tab styling to document window state.
- [x] 2.3 Bind the workflow area to the active context's workflow stage.
- [x] 2.4 Bind the detail area to the active context's current stage or detail view, with an empty state when no tabs are open.

## 3. Context Coordination

- [x] 3.1 Add a navigation-open action path that opens or activates document tabs without discarding other open tabs.
- [x] 3.2 Coordinate active tab changes with navigation selection or revealed hierarchy state when the active context has a known location.
- [x] 3.3 Coordinate active tab changes with the workflow stage navigator state.
- [x] 3.4 Preserve a single detail-area host shape that can later be used by built-in stage views and Stage Tool Host-provided views.

## 4. Verification

- [x] 4.1 Add unit tests for opening multiple contexts, opening duplicate contexts, switching tabs, and closing tabs.
- [x] 4.2 Add unit tests for active-context coordination with workflow stage and navigation location state.
- [x] 4.3 Build the solution and run the relevant test projects.
- [x] 4.4 Manually launch the app and verify the document window layout shows tabs, workflow area, detail area, and empty state correctly.
