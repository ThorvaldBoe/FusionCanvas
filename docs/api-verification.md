# Manual live API verification

The live verification runner is a standalone, opt-in CLI. It is excluded from `FusionCanvas.sln` and the default test lane never calls a provider. The runner uses FusionCanvas' production OpenRouter and Printify adapters so findings exercise the same URL construction, headers, serializers, response parsers, and operation contracts used by the app.

## Configure credentials

Use dedicated test credentials. Do not place their values in source files, command arguments, reports, issue text, or screenshots.

```powershell
$env:FUSIONCANVAS_OPENROUTER_TEST_API_KEY = '<dedicated test key>'
$env:FUSIONCANVAS_OPENROUTER_SESSION_MAX_COST_USD = '0.25'
$env:FUSIONCANVAS_PRINTIFY_DEVTEST_API_KEY = '<FusionCanvas DevTest key>'
```

After you confirm a Printify session, the runner calls `GET /v1/shops.json` using the configured key and selects the shop whose title exactly matches `FusionCanvas DevTest`. It blocks if there is no exact match, more than one exact match, or the returned ID is invalid. You do not need to find or configure the numeric shop ID. The resolved ID is used for the run and recorded with the session-owned product journal. Product lifecycle operations remain blocked until the shop identity resolves uniquely.

## Inspect before running

```powershell
dotnet run --project .\src\FusionCanvas.ApiVerification -- list
dotnet run --project .\src\FusionCanvas.ApiVerification -- plan --provider openrouter --scenario openrouter-text-generation
dotnet run --project .\src\FusionCanvas.ApiVerification -- plan --provider printify --scenario printify-product-lifecycle
```

`list` and `plan` are local-only and make no network calls. Every live run requires an explicit `run` or `qualify` command and a typed session confirmation. Write scenarios request a second confirmation for each mutation.

## OpenRouter

```powershell
dotnet run --project .\src\FusionCanvas.ApiVerification -- run --provider openrouter --scenario openrouter-smoke
dotnet run --project .\src\FusionCanvas.ApiVerification -- run --provider openrouter --scenario openrouter-catalog --models 'provider/model-a,provider/model-b'
dotnet run --project .\src\FusionCanvas.ApiVerification -- run --provider openrouter --scenario openrouter-text-generation --models 'provider/model-a' --max-cost 0.01
dotnet run --project .\src\FusionCanvas.ApiVerification -- run --provider openrouter --scenario openrouter-image-generation --models 'provider/image-model' --max-cost 0.05
```

Text and image generation require a known upper bound from provider endpoint pricing and bounded request parameters. Unknown pricing or unsupported billing units block the call. The bound is shown before confirmation. The model qualification campaign is manual because it can send a request for every selected model and may incur cost:

```powershell
dotnet run --project .\src\FusionCanvas.ApiVerification -- qualify --provider openrouter --models all --max-cost 0.25
```

Qualification profiles use model catalog capabilities and `AiParameterRegistry`, pin to a priced endpoint, use a short text prompt and a single image, and request confirmation per generation. Matching successful fingerprints are reused unless `--force` is passed. Evidence is keyed by model, effective profile, endpoint, advertised capabilities, and adapter version. Reports and qualification evidence are stored under `%LOCALAPPDATA%\FusionCanvas\ApiVerification`.

The prompt-only image catalog omits Recraft V4 Styles and V4 Styles Pro because those models require style-reference images, which the current image-generation request contract cannot send. Other Recraft models remain available when their endpoint accepts the prompt-only request.

## Printify

```powershell
dotnet run --project .\src\FusionCanvas.ApiVerification -- run --provider printify --scenario printify-smoke
dotnet run --project .\src\FusionCanvas.ApiVerification -- run --provider printify --scenario printify-catalog
dotnet run --project .\src\FusionCanvas.ApiVerification -- run --provider printify --scenario printify-shop-products
dotnet run --project .\src\FusionCanvas.ApiVerification -- run --provider printify --scenario printify-product-detail
dotnet run --project .\src\FusionCanvas.ApiVerification -- run --provider printify --scenario printify-artwork-import
dotnet run --project .\src\FusionCanvas.ApiVerification -- run --provider printify --scenario printify-product-lifecycle
```

The lifecycle scenario uploads generated artwork, creates an unpublished product from the live catalog, reads it back, updates it, optionally publishes and unpublishes it, and offers confirmed deletion with a final absence check. Every inventory item has an explicit cost class; a missing or billable Printify classification blocks the scenario before network access. Publishing is marked as a visibility risk and requires a stronger confirmation. It may expose a sellable listing on connected channels; check those channels first because Printify charges production and shipping when an order is placed ([Printify billing help](https://help.printify.com/hc/en-us/articles/4483626302609-How-will-Printify-charge-me)). The runner never creates or modifies orders. Declining deletion leaves the product ID in the session report and recovery journal. Recover only from a report or journal that identifies a pending session-created product:

```powershell
dotnet run --project .\src\FusionCanvas.ApiVerification -- recover --provider printify --run-id '<session run id>'
```

Recovery requires the versioned session journal, which records the exact DevTest shop ID and the product ID owned by that run. If a report is present, it must agree with the journal's product ID. Recovery rechecks the DevTest identity and refuses to delete a still-published product; unpublish it through the normal workflow first. It then asks for confirmation, deletes, and verifies absence. If a write result is uncertain, inspect the DevTest shop manually before attempting recovery.

## Findings and service improvements

Each run writes JSON and Markdown reports with operation status, safe diagnostics, profile fingerprints, known or estimated cost, findings, and pending cleanup. Do not include credentials or personally identifying customer data in a test prompt or product. Findings should become focused fixes in the owning production adapter/application path and deterministic fake-handler regression tests; update the report or link the follow-up evidence after rerunning the affected live scenario.

The operation inventory is maintained in `src/FusionCanvas.ApiVerification/Inventory/ApiOperationInventory.cs`. Keep it aligned with production outbound calls and the tables in `docs/external-api-mocking.md`. Shopify is intentionally outside this initial module.

## Request and response contract checkpoints

| Operations | Request details to compare with provider evidence | Response details the production adapter must preserve |
| --- | --- | --- |
| OpenRouter key, model catalogs, ZDR, and endpoints | Bearer authorization; exact catalog URL and selected model path; `requireZeroDataRetention` selects the ZDR lookup. | Key status and account fields; model IDs/capabilities/prices; endpoint IDs, provider IDs, supported parameters and pricing. |
| OpenRouter text generation | JSON `model`, `messages`, `stream=false`, provider route/endpoint with fallbacks disabled, and only the effective model parameters from `AiParameterRegistry`; completion tokens are capped at 24. | Choice content/finish reason, requested and resolved model, usage token counts and provider-reported cost. |
| OpenRouter image generation | JSON model/prompt, one output, exact selected endpoint, dimensions and supported options. Unknown image price units or dimensions block before submission. | Image bytes/media type/resolved model and reported usage/cost; bytes are discarded after verification. |
| Printify shops and catalogs | Bearer credential; catalog/shop-specific base paths; selected product list uses `limit=50&page=N` pagination. | Shop ID is numeric and title must exactly equal `FusionCanvas DevTest`; blueprints/providers/variants preserve integer IDs, booleans, option IDs and placeholder positions/dimensions. |
| Printify product import/detail/artwork | Shop and product IDs are escaped; artwork requests use validated HTTPS hosts without forwarding the API credential. | Product/variant/print-area identity, visibility, prices and artwork URLs; downloaded artwork has a bounded size, supported content type and valid file signature. |
| Printify artwork upload | JSON `file_name` plus base64 `contents`, using a generated 1024×1024 PNG fixture. | Non-empty uploaded image `id`, retained with the content fingerprint for product creation. |
| Printify product create/update | Numeric blueprint/provider/variant IDs; price uses minor-unit integer; `is_enabled` boolean; `variant_ids` integers; image `id`, `x`, `y`, `scale`, `angle`, and print position. | Product ID is captured in the session journal immediately; production GET verifies product identity and title after create/update. |
| Printify publish/unpublish/delete | Shop/product path, no order endpoint; publish requires a separate visibility/cost-impact confirmation. | Production GET must report expected `visible` state; delete must be followed by confirmed product absence. |
