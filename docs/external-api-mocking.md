# External API Inventory and Mocking Boundary

## Current inventory

| External API | Live service | Application contract(s) | Mock twin | Implemented scope |
| --- | --- | --- | --- | --- |
| OpenRouter | `FusionCanvas.Integration.AI.OpenRouterClient` | `IAiCredentialValidator`, `IAiModelCatalogProvider`, `IAiImageModelCatalogProvider`, `IAiImageEndpointCatalogProvider`, `IAiTextProvider`, `IAiImageGenerationProvider` | `FusionCanvas.Integration.Testing.MockOpenRouterClient` | Key validation, text/image model catalogs, image endpoints, text generation, image generation |
| Printify catalog/shop | `FusionCanvas.Integration.Stores.Printify.PrintifyCatalogClient` | `IPrintifyCatalogClient` | `FusionCanvas.Integration.Testing.MockPrintifyCatalogClient` | Catalog blueprints, selected blueprints, shop products, selected shop products |
| Printify shops verification | `FusionCanvas.Integration.Stores.Printify.PrintifyCredentialVerifier` | `IPrintifyCredentialVerifier` | `FusionCanvas.Integration.Testing.MockPrintifyCredentialVerifier` | Key verification and synthetic shop list |

Shopify is planned but is not implemented in the current codebase, so it has no service or mock twin yet.

## Testing boundary

- Application and business-logic tests that consume these contracts use the reusable mock twin or an equivalent contract-focused double. They do not construct a live adapter or real `HttpClient`.
- Live-adapter tests remain under the AI and Stores integration-test folders. They use local fake HTTP handlers to verify endpoint paths, headers, serialization, response parsing, retry behavior, and limits. They never call a real endpoint.
- Mock defaults are synthetic and endpoint-free. Mock observations omit API keys and other credentials.

When a new external API service is added, update this inventory and add its mock twin in the same delivery module.
