# 0002 - Dapper with configurable provider

Versao em portugues: [0002-dapper-com-provider-configuravel.md](0002-dapper-com-provider-configuravel.md)

- Status: accepted

## Context

The project needed real persistence with visible SQL and low coupling to the current provider.

## Decision

Use Dapper as the data access layer and centralize connection creation in `IDbConnectionFactory`.

In practice, this means:

- explicit SQL queries in repositories
- database provider selected through `Database:Provider`
- SQLite as the local default
- structured SQL Server support without changing controllers or services

## Consequences

- full visibility of executed queries
- low coupling to the database provider
- less mapping and tracking automation than EF Core
- schema evolution and provider differences still need manual handling