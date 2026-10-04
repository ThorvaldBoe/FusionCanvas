## 1. Test gate extraction

- [x] 1.1 Move deterministic solution testing into a reusable workflow.
- [x] 1.2 Preserve issue #648 failure tracking and least-privilege permissions in `ci.yml`.

## 2. Windows candidate packaging

- [x] 2.1 Add merge-to-`main` and manual package triggers.
- [x] 2.2 Gate packaging on reusable deterministic tests.
- [x] 2.3 Publish a self-contained `win-x64` application and version-aligned ZIP.
- [x] 2.4 Publish SHA-256 checksum and artifact provenance attestation.

## 3. Documentation and verification

- [x] 3.1 Document candidate artifact behavior and remaining installer/release/update scope.
- [x] 3.2 Run local build/test and strict OpenSpec validation.
- [ ] 3.3 Verify the hosted Windows workflow after merge.
