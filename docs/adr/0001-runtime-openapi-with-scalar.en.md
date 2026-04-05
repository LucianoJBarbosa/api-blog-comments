# 0001 - Runtime OpenAPI with Scalar

Versao em portugues: [0001-openapi-runtime-com-scalar.md](0001-openapi-runtime-com-scalar.md)

- Status: accepted

## Context

The API needed to expose a consumable OpenAPI contract and an inspection interface aligned with runtime behavior.

## Decision

Use runtime-generated OpenAPI with Scalar as the interactive interface.

In practice, this means:

- runtime document at `/docs/openapi/v1.json`
- interactive interface at `/docs`
- keeping `OpenAPI.yaml` as a static artifact in the repository

## Consequences

- alignment with the current .NET 10 ecosystem
- clear separation between the contract and the inspection interface
- lower familiarity for teams used to Swashbuckle
- need for small compatibility adjustments in the runtime document