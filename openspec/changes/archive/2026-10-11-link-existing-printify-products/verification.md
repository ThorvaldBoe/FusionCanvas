# Verification

| Acceptance scenario | Method | Result | Evidence / limitation |
| --- | --- | --- | --- |
| Selected shop is present and exact | Mocked Printify Integration response and Application readiness tests | Pass | `PrintifyListingClientTests.Connection_and_create_use_shared_authenticated_transport_and_product_payload`; selected shop `42` found in `/shops.json`. |
| Selected shop is missing or response is invalid | Mocked Integration responses and fail-closed readiness tests | Pass | `Connection_is_unavailable_when_selected_shop_is_missing`; `Connection_is_unavailable_when_shop_list_is_invalid` covers malformed JSON, object root, and entries without IDs. |
| Existing product belongs to selected shop | Mocked product response with exact product/shop identity | Pass | `Product_read_retains_identity_validation_data` exact-match case. |
| Product is missing, malformed, mismatched, or unavailable | Mocked Integration/Application failure paths; repository remains unchanged on rejection | Pass | `Product_identity_mismatch_duplicate_and_uncertain_mapping_are_rejected_without_writes`; identity parser covers missing and mismatched product/shop IDs. |
| Valid candidate is confirmed and persisted | Application repository test and headless Listing tool workflow | Pass | `Verify_and_confirm_link_persist_only_verified_product`; `PrintifyTool_VerifiesExistingProductBeforeConfirmingAndCanCancel`. |
| Existing product link is replaced after confirmation | Application repository test verifies replacement identity and snapshot | Pass | `Verify_and_confirm_link_persist_only_verified_product`. |
| Cancel, duplicate identity, stale verification, or uncertain current operation | Application and headless no-write/action-state tests | Pass | `Product_identity_mismatch_duplicate_and_uncertain_mapping_are_rejected_without_writes`; view model test verifies cancel leaves mappings empty. |
| Current product ID is visible and copyable; read-only mode blocks changes | Avalonia headless view/binding and command-state tests | Pass | `PrintifyProductLinkControls_AreVisibleAndCurrentProductIdIsCopyable`; `PrintifyTool_ShowsLinkedProductIdButDisablesChangesWhenReadOnly`. |

## Required commands

- Focused Integration: `dotnet test .\tests\FusionCanvas.Integration.Tests\FusionCanvas.Integration.Tests.csproj -m:1 --no-restore --filter FullyQualifiedName~PrintifyListingClientTests` — passed, 19 tests.
- Focused Application: `dotnet test .\tests\FusionCanvas.Application.Tests\FusionCanvas.Application.Tests.csproj -m:1 --filter FullyQualifiedName~ListingLifecycleServiceTests` — passed, 12 tests.
- Focused App: `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj -m:1 --filter FullyQualifiedName~StageToolViewModelsTests` — passed, 20 tests; Avalonia control check `PrintifyProductLinkControls_AreVisibleAndCurrentProductIdIsCopyable` — passed, 1 test.
- Strict OpenSpec: `openspec validate link-existing-printify-products --strict` — passed.
- Full deterministic baseline: `dotnet test .\FusionCanvas.sln -m:1` — passed, 2,390 tests across Domain (294), Application (686), Integration (364), App (1,017), and UI Description (29).

Post-sync strict validation: `openspec validate --all --strict` passed all 88 OpenSpec items. The capability delta was synced to `openspec/specs/printify-existing-product-linking/spec.md` and archived to `openspec/changes/archive/2026-10-11-link-existing-printify-products/`.

Restore/build reports existing NuGet advisories for `SixLabors.ImageSharp` 3.1.12 (NU1902/NU1903). These are unrelated to this change.
