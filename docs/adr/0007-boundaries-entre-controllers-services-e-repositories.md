# 0007 - Fronteiras entre controllers, services e repositories

Versao em ingles: [0007-boundaries-between-controllers-services-and-repositories.en.md](0007-boundaries-between-controllers-services-and-repositories.en.md)

- Status: aceito

## Contexto

Com autenticação persistida, ownership, roles e autorização, concentrar lógica em controllers aumentaria acoplamento entre HTTP, regra e persistência.

## Decisão

Separar explicitamente as responsabilidades em três fronteiras principais:

- controllers para HTTP e semântica de resposta
- services para regras de negócio e orquestração
- repositories para acesso a dados e SQL

Na prática, isso significa:

- `AuthController` e `PostsController` recebem requests e devolvem respostas HTTP
- `AuthService` concentra autenticação e cadastro
- `DapperUsersRepository` e `DapperPostsRepository` encapsulam queries e projeções

## Consequências

- menor acoplamento entre borda HTTP e persistência
- regras mais fáceis de localizar e evoluir
- mais classes e interfaces do que uma implementação direta
- parte da orquestração simples pode parecer verbosa em uma API pequena
