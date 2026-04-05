# 0008 - Dapper versus EF Core

Versao em portugues: [0008-dapper-vs-ef-core.md](0008-dapper-vs-ef-core.md)

- Status: accepted

## Context

The project needed to keep data-access behavior and executed SQL explicit and easy to inspect.

## Decision

Adopt Dapper as the project's default data-access approach instead of EF Core.

- Dapper favors explicit SQL visibility
- it reduces the distance between the query that is written and the query that is executed
- it makes it easier to explain execution plans and provider differences
- it fits the size and technical goal of the project

## Consequences

- queries are easier to audit and explain
- performance tuning is more direct
- fewer surprises in generated SQL behavior
- better fit for provider-specific SQL
- more handwritten SQL
- more application responsibility for projections and provider compatibility
- less automation than a full ORM