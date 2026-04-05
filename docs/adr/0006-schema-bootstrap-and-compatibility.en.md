# 0006 - Schema bootstrap and compatibility

Versao em portugues: [0006-bootstrap-de-schema-e-compatibilidade.md](0006-bootstrap-de-schema-e-compatibilidade.md)

- Status: accepted

## Context

The project needed low-friction database bootstrap. In the first phase, schema creation and compatibility lived in one place. As the baseline evolved, the goal became preserving that low-friction startup without keeping schema evolution inside a monolithic bootstrap path.

## Decision

Keep `DatabaseInitializer` as the bootstrap entry point, but make it orchestrate versioned migrations with history in `__SchemaMigrations`.

In practice, this means:

- executing known migrations at startup
- recording migration history
- keeping compatibility adjustments for SQLite and SQL Server inside explicit migrations
- avoiding dependence on external tooling for the initial bootstrap of the database

## Consequences

- fast startup on fresh environments
- minimum compatibility with earlier versions of the database
- explicit, traceable, and incremental schema evolution
- `DatabaseInitializer` no longer concentrates structural SQL directly, but still acts as the local process orchestrator
- rollback and external migration pipelines remain outside the baseline