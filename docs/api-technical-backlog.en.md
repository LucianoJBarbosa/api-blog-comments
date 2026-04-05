# API Technical Backlog

Versao em portugues: [backlog-tecnico-api.md](backlog-tecnico-api.md)

Prioritized backlog for the next phase of the project. The current baseline already covers authentication, simple authorization, pagination, audit timestamps, versioned migrations, minimal observability, runtime OpenAPI, and integration tests.

The backlog below only covers what still remains as relevant structural evolution. The point is not to reopen the boilerplate architecture, but to guide the next round of technical hardening.

## Current state

- functional boilerplate for small and medium APIs
- simple local execution with SQLite
- stabilized authentication and authorization baseline
- versioned migrations and explicit local tooling
- minimal observability already in place

## Now

### 1. Migrate the main runtime to SQL Server

- Impact: high
- Risk: medium
- Effort: medium
- Reason: SQLite fits development, tests, and local demos, but it is not the right base for sustained growth in concurrency, observability, and tuning.
- Expected delivery:
	- SQL Server as the default operational configuration
	- migration and compatibility validation on the main provider
	- local documentation positioning SQLite as a development convenience rather than the target runtime

### 2. Stop loading the full comment collection in post detail

- Impact: high
- Risk: medium
- Effort: medium
- Reason: post detail still embeds a variable payload with no limit as comment volume grows.
- Expected delivery:
	- post detail without the full comment collection
	- comments consumed through the existing paginated route
	- contract and documentation updated to reflect the new behavior

### 3. Move ownership checks into SQL write operations

- Impact: medium
- Risk: medium
- Effort: medium
- Reason: the current flow reads the resource first and only then updates or deletes it, which increases round-trips and leaves a concurrency window.

## Next

### 4. Rework rate limiting for multiple instances

- Impact: high
- Risk: medium
- Effort: medium
- Reason: the current rate limiting is process-local and insufficient as a global control in distributed environments.

### 5. Apply rate limiting by IP and login identity

- Impact: medium
- Risk: low
- Effort: low
- Reason: limiting only by IP is not enough for NAT, proxy, or distributed attacks against specific usernames.

### 6. Handle forwarded headers at the HTTP edge

- Impact: medium
- Risk: medium
- Effort: low
- Reason: the real client IP and HTTPS coherence depend on this when the API is behind a proxy.

## Later

### 7. Make Argon2id parameters configurable per environment

- Impact: medium
- Risk: low
- Effort: low
- Reason: login CPU and memory cost should be tuned according to environment and operational capacity.

### 8. Add latency and status-code metrics

- Impact: high
- Risk: low
- Effort: medium
- Reason: traffic growth requires objective visibility into latency, error rate, and saturation.

### 9. Add basic tracing for requests and data access

- Impact: medium
- Risk: low
- Effort: medium
- Reason: correlation across request, authentication, and query execution becomes important as volume grows.

## Optional for the next phase

### 10. Define a refresh-token strategy

- Impact: medium
- Risk: medium
- Effort: medium
- Reason: purely stateless JWT keeps the initial baseline simple, but limits more controlled session scenarios.

### 11. Define a token revocation strategy

- Impact: medium
- Risk: medium
- Effort: medium
- Reason: effective logout, session blocking, and selective invalidation require a model beyond the current token.

### 12. Evaluate caching for hot public reads

- Impact: medium
- Risk: medium
- Effort: medium
- Reason: public listings tend to concentrate more reads than writes as traffic grows.

## Outside the immediate backlog

- ornamental architectural restructuring
- replacing Dapper with a full ORM without concrete functional or operational pressure
- introducing a fine-grained permission model before the domain actually requires it

## Recommended sequence

1. SQL Server + post detail without full comments
2. Ownership in SQL + HTTP edge hardening
3. Metrics, tracing, and authentication tuning
4. Session evolution, revocation, and cache