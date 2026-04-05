# 0007 - Boundaries between controllers, services, and repositories

Versao em portugues: [0007-boundaries-entre-controllers-services-e-repositories.md](0007-boundaries-entre-controllers-services-e-repositories.md)

- Status: accepted

## Context

With persisted authentication, ownership, roles, and authorization, concentrating logic in controllers would increase coupling between HTTP, rules, and persistence.

## Decision

Explicitly separate responsibilities into three main boundaries:

- controllers for HTTP and response semantics
- services for business rules and orchestration
- repositories for data access and SQL

In practice, this means:

- `AuthController` and `PostsController` receive requests and return HTTP responses
- `AuthService` concentrates authentication and registration logic
- `DapperUsersRepository` and `DapperPostsRepository` encapsulate queries and projections

## Consequences

- lower coupling between the HTTP edge and persistence
- rules become easier to locate and evolve
- more classes and interfaces than in a direct implementation
- part of the simple orchestration may feel verbose in a small API