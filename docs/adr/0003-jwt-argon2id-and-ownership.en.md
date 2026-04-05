# 0003 - JWT, Argon2id, and ownership

Versao em portugues: [0003-jwt-argon2id-e-ownership.md](0003-jwt-argon2id-e-ownership.md)

- Status: accepted

## Context

Generic authentication and binary authorization were not enough for an API with persisted authorship on posts and comments.

## Decision

Adopt three combined pillars:

- Argon2id for password hashing
- JWT with identity and role claims
- ownership on posts and comments with admin override

In practice, this means:

- passwords stored only as derived hashes with salt
- token containing `userId`, `username`, and `role`
- `Author` can modify only their own resources
- `Admin` can modify any resource

## Consequences

- modern authentication baseline resistant to password cracking
- explicit ownership in the domain and in queries
- higher CPU cost during login and registration because of Argon2id
- schema, DTOs, and repositories become more complex when authorship is tracked
- fine-grained token revocation remains a separate concern