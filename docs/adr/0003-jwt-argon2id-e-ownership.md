# 0003 - JWT, Argon2id e ownership

Versao em ingles: [0003-jwt-argon2id-and-ownership.en.md](0003-jwt-argon2id-and-ownership.en.md)

- Status: aceito

## Contexto

Autenticação genérica e autorização binária não eram suficientes para uma API com autoria persistida de posts e comentários.

## Decisão

Adotar três pilares combinados:

- Argon2id para hashing de senha
- JWT com claims de identidade e papel
- ownership em posts e comentários com override administrativo

Na prática, isso significa:

- senha armazenada apenas como hash derivado com salt
- token contendo `userId`, `username` e `role`
- `Author` pode modificar apenas os próprios recursos
- `Admin` pode modificar qualquer recurso

## Consequências

- base de autenticação resistente a cracking
- ownership explícito no domínio e nas queries
- custo computacional maior no fluxo de login e cadastro por causa do Argon2id
- schema, DTOs e repositórios ficam um pouco mais complexos ao carregar autoria
- revogação fina de tokens continua sendo uma preocupação separada
