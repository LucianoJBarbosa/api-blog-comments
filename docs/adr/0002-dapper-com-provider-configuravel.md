# 0002 - Dapper com provider configurável

Versao em ingles: [0002-dapper-with-configurable-provider.en.md](0002-dapper-with-configurable-provider.en.md)

- Status: aceito

## Contexto

O projeto precisava de persistência real com SQL visível e baixo acoplamento ao provedor corrente.

## Decisão

Usar Dapper como camada de acesso a dados e centralizar a criação de conexões em `IDbConnectionFactory`.

Na prática, isso significa:

- queries SQL explícitas nos repositórios
- provedor de banco configurável por `Database:Provider`
- SQLite como padrão local
- suporte estruturado para SQL Server sem mudar controllers ou services

## Consequências

- visibilidade total das queries executadas
- baixo acoplamento ao provedor de banco
- menos automação de mapeamento e tracking do que EF Core
- evolução de schema e diferenças entre providers precisam ser tratadas manualmente
