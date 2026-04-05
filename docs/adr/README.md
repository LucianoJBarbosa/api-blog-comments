# ADRs

Versao em ingles: [README.en.md](README.en.md)

Este diretório concentra os Registros de Decisão Arquitetural do projeto.

## Índice

- [0001 - OpenAPI runtime com Scalar](0001-openapi-runtime-com-scalar.md)
- [0002 - Persistência com Dapper e provider configurável](0002-dapper-com-provider-configuravel.md)
- [0003 - JWT, Argon2id e ownership](0003-jwt-argon2id-e-ownership.md)
- [0004 - Hardening por ambiente nas rotas de autenticação](0004-hardening-por-ambiente-e-rotas-de-auth.md)
- [0005 - Estratégia de testes de integração](0005-estrategia-de-testes-de-integracao.md)
- [0006 - Bootstrap de schema e compatibilidade](0006-bootstrap-de-schema-e-compatibilidade.md)
- [0007 - Fronteiras entre controllers, services e repositories](0007-boundaries-entre-controllers-services-e-repositories.md)
- [0008 - Dapper versus EF Core](0008-dapper-vs-ef-core.md)

## Função

Os ADRs deste repositório servem para:

- preservar o racional técnico das decisões principais
- documentar alternativas rejeitadas e trade-offs aceitos
- reduzir ambiguidade em futuras evoluções da base

## Estrutura

Cada ADR segue a mesma estrutura:

- contexto
- decisão
- consequências

Os registros são curtos por definição. O objetivo é manter rastreabilidade técnica sem ampliar burocracia documental.