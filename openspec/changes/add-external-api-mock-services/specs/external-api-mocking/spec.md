## ADDED Requirements

### Requirement: Current external API services have contract-parity mock twins
FusionCanvas SHALL maintain an endpoint-free mock implementation for every currently implemented service that calls an external API, using the same application-facing interface(s) as the live implementation.

#### Scenario: OpenRouter service inventory is checked
- **WHEN** the external API service inventory is reviewed
- **THEN** OpenRouter SHALL have one mock twin covering credential validation, model catalogs, image endpoint discovery, text generation, and image generation
- **AND** the mock SHALL implement the same six application-facing provider contracts as the live OpenRouter adapter

#### Scenario: Printify service inventory is checked
- **WHEN** the external API service inventory is reviewed
- **THEN** the Printify catalog service and credential-verification service SHALL each have a mock twin
- **AND** each mock SHALL implement the same application-facing interface as its live counterpart

#### Scenario: A future external API is added
- **WHEN** a new FusionCanvas service begins calling an external API
- **THEN** its application-facing contract and live adapter SHALL be accompanied by a mock twin before consumer unit tests depend on it
- **AND** the service inventory SHALL identify the new live/mock pair

### Requirement: Mock services never perform external I/O
Every external API mock SHALL execute entirely in memory and SHALL NOT construct or invoke an HTTP client, socket, file, database, credential-store, or other endpoint-capable dependency.

#### Scenario: A mock method is invoked
- **WHEN** a consumer calls any method on an external API mock
- **THEN** the result SHALL be produced from synthetic in-memory data or configured in-memory behavior
- **AND** the call SHALL succeed or fail without network access

#### Scenario: A test runs without network availability
- **WHEN** external API consumer tests run with network access unavailable
- **THEN** tests using mock services SHALL remain deterministic and independent of external service availability

### Requirement: Mock data and outcomes are deterministic and safe
External API mocks SHALL provide non-sensitive synthetic defaults and SHALL allow tests to select deterministic success, failure, empty, and cancellation outcomes without exposing or retaining supplied API secrets.

#### Scenario: A mock is created with defaults
- **WHEN** a test constructs an external API mock without a custom scenario
- **THEN** it SHALL return stable synthetic identifiers, names, text, image bytes, catalog records, and shop records
- **AND** the defaults SHALL not contain real customer, store, product, credential, or provider-response data

#### Scenario: A test configures a failure
- **WHEN** a test configures a mock outcome such as invalid credentials, rate limiting, unavailable service, malformed-equivalent failure, or an application-level generation failure
- **THEN** the mock SHALL return the configured contract-level result or exception deterministically
- **AND** it SHALL not call a live endpoint to obtain that outcome

#### Scenario: A cancellation token is already cancelled
- **WHEN** a mock method is called with an already-cancelled token
- **THEN** it SHALL observe cancellation before producing a result
- **AND** it SHALL not record or perform an external operation

#### Scenario: A secret is passed to a mock
- **WHEN** a consumer passes an API key or Printify key to a mock method
- **THEN** the mock MAY validate presence but SHALL NOT retain the secret in request history, sample data, logs, exceptions, or returned data

### Requirement: Mock calls are observable without exposing secrets
Mock services SHALL expose enough in-memory observations for tests to assert the consumer-facing request shape and call count, while excluding credential values and unrelated transport details.

#### Scenario: A text generation request is submitted
- **WHEN** a consumer calls the OpenRouter text-generation mock
- **THEN** the mock SHALL make the submitted model, messages, profile, and privacy setting observable to the test
- **AND** the API key SHALL not be observable through the recorded request

#### Scenario: A catalog request is submitted
- **WHEN** a consumer calls a Printify catalog mock
- **THEN** the mock SHALL make the requested shop or catalog identifiers and selected product identifiers observable where those values are part of the contract
- **AND** the API key SHALL not be observable through the recorded request

### Requirement: Tests separate mock-consumer coverage from live-adapter coverage
FusionCanvas SHALL use mock services for application and business-logic tests that consume external API contracts, while transport-level adapter tests SHALL remain isolated tests of live request/response translation using local HTTP fakes and SHALL never call a real endpoint.

#### Scenario: An application service consumes an external API
- **WHEN** a unit test exercises application orchestration that depends on OpenRouter or Printify
- **THEN** it SHALL inject the reusable mock service twin or an equivalent contract-focused test double
- **AND** the test SHALL not construct a live adapter or real HTTP client

#### Scenario: A live adapter mapping is tested
- **WHEN** a test verifies serialization, headers, endpoint paths, response parsing, retry mapping, or payload limits of a live adapter
- **THEN** it MAY use a local fake HTTP handler
- **AND** it SHALL not use a live endpoint or API credential
