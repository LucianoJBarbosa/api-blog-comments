# 0005 - Integration test strategy

Versao em portugues: [0005-estrategia-de-testes-de-integracao.md](0005-estrategia-de-testes-de-integracao.md)

- Status: accepted

## Context

Final API behavior depends on middleware, authentication, authorization, runtime documentation, and persistence.

## Decision

Adopt HTTP integration tests with `WebApplicationFactory`, an isolated SQLite database per suite, and explicit state reset.

In practice, this means:

- starting the application in memory for tests
- exercising real HTTP routes
- validating authentication, ownership, OpenAPI, and Scalar
- controlling test infrastructure details such as rate limiting

## Consequences

- validation closer to real API usage
- better regression coverage for complete flows
- slower execution than pure unit tests
- higher sensitivity to environment configuration and test-host bootstrap