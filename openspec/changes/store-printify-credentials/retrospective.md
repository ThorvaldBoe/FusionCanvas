# Retrospective — store-printify-credentials

## What changed

The implementation keeps a native Printify token copy under each Store scope while treating the provider token as account-wide. Successful verification returns the account's Printify shops, the Store Editor presents them in a dropdown, and the selected shop ID is persisted with Store metadata. Verification clears a previously selected shop when it is absent from a later response.

## What worked

- OpenSpec exploration caught the account-wide token correction before implementation was finalized.
- Native credential isolation, bounded provider responses, and Store-context persistence are covered by focused tests.
- Avalonia headless tests cover strategy selection, credential actions, shop selection, and context transitions.

## Follow-up

The exact solution build remains subject to the environment's Avalonia telemetry log permission. The equivalent build and full test baseline pass with `-p:UsedAvaloniaProducts=`; archive and accepted-spec synchronization remain delivery steps after review.
