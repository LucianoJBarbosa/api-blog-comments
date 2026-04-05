# 0006 - Bootstrap de schema e compatibilidade

Versao em ingles: [0006-schema-bootstrap-and-compatibility.en.md](0006-schema-bootstrap-and-compatibility.en.md)

- Status: aceito

## Contexto

O projeto precisava iniciar banco real com baixo atrito. Na fase inicial, isso exigiu centralizar criação de schema e compatibilidade em um ponto único. Com a evolução da base, surgiu a necessidade de manter esse baixo atrito sem continuar concentrando toda a evolução de schema em um bootstrap monolítico.

## Decisão

Manter `DatabaseInitializer` como ponto de entrada do bootstrap, mas fazê-lo orquestrar migrações versionadas com histórico em `__SchemaMigrations`.

Na prática, isso significa:

- execução de migrações conhecidas no startup
- registro de histórico de migração aplicado
- manutenção de ajustes de compatibilidade para SQLite e SQL Server dentro de migrações explícitas
- ausência de dependência de ferramenta externa para o bootstrap inicial da base

## Consequências

- projeto sobe rapidamente em ambiente novo
- compatibilidade mínima com versões anteriores da base
- evolução de schema fica explícita, rastreável e incremental
- `DatabaseInitializer` deixa de concentrar diretamente o SQL estrutural, mas continua como orquestrador local do processo
- rollback e pipeline externo de migração continuam fora do escopo da base
