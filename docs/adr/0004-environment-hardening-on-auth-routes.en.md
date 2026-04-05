# 0004 - Environment hardening on authentication routes

Versao em portugues: [0004-hardening-por-ambiente-e-rotas-de-auth.md](0004-hardening-por-ambiente-e-rotas-de-auth.md)

- Status: accepted

## Context

A production-oriented baseline should not depend on default credentials, implicit bootstrap, or automatic authentication inside documentation tooling.

## Decision

Remove implicit development conveniences from the default runtime and add transversal protection to authentication routes.

In practice, this means:

- no automatic admin seed
- no auto-login in Scalar
- requirement for a real JWT secret through external configuration
- dedicated rate limiting for `login` and `register`
- ability to control this behavior through configuration

## Consequences

- clearer separation between local development and production-like environments
- hardening handled in infrastructure
- tests and automation must neutralize or configure rate limiting to avoid false negatives
- the environment requires explicit operational bootstrap for admin users and secrets