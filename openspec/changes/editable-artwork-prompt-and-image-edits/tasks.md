## 1. Prompt compilation and Item preferences

- [x] 1.1 Add per-Item contrast, generation-prompt override, and separate adjustment-instructions metadata plus a deterministic default prompt compiler that uses current Concept/Niche data and only current, layout-relevant SLL content.
- [x] 1.2 Add Application tests for deterministic prompt compilation, selected contrast directions, SLL extraction, and stale/invalid SLL omission.
- [x] 1.3 Extend Design-stage state and persistence for prompt override, contrast, reset-to-current-default, and state reload; add Application tests for save/reload/reset after source changes.

## 2. Design-stage prompt controls

- [x] 2.1 Add generation-prompt and separate adjustment-instructions editors, contrast selector, reset, and edit-current-artwork actions to the Design stage with read-only, busy, missing-art, and unsupported-endpoint states.
- [x] 2.2 Add Avalonia headless view coverage for editor bindings, contrast change, reset, persistence/re-entry, and edit-action availability.

## 3. Image-reference provider support

- [x] 3.1 Parse endpoint-advertised input-reference limits and extend image requests/OpenRouter JSON with one optional PNG reference without changing text-to-image request fields.
- [x] 3.2 Add endpoint-policy and Integration tests for supported/unsupported references, request serialization, and omission on ordinary generation.

## 4. Existing-artwork edit and persistence

- [x] 4.1 Implement image-edit dispatch with the selected slot PNG as reference and separate adjustment instructions; verify the slot/context is unchanged before saving.
- [x] 4.2 Persist successful edits atomically with the previous artwork retained as an Item-linked Related Image; clean up staged files and preserve the old slot on failure/cancellation.
- [x] 4.3 Add Application tests for request image/prompt, replacement and backup, unsupported endpoint, missing image, stale context, cancellation, and save cleanup.

## 5. Follow-up and completion verification

- [x] 5.1 Create a GitHub issue for curated visual styles, explicitly out of scope for implementation.
- [x] 5.2 Update verification.md with evidence for every acceptance scenario; correct any failed criteria and rerun affected checks.
- [x] 5.3 Run strict OpenSpec validation and `dotnet test .\FusionCanvas.sln`; record results and limitations in verification.md.
