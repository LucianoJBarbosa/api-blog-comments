# 0008 - Dapper versus EF Core

Versao em ingles: [0008-dapper-versus-ef-core.en.md](0008-dapper-versus-ef-core.en.md)

- Status: aceito

## Contexto

O projeto precisava manter clareza sobre o comportamento do acesso a dados e sobre o SQL efetivamente executado.

## Decisão

Adotar Dapper como padrão de acesso a dados do projeto em vez de EF Core.

- Dapper favorece visibilidade explícita do SQL
- reduz a distância entre query escrita e query executada
- facilita explicar planos de execução e diferenças entre providers
- combina com o tamanho e o objetivo técnico do projeto

## Consequências

- queries mais auditáveis e fáceis de explicar
- tuning mais direto
- menor surpresa no comportamento SQL gerado
- boa aderência a SQL específico do banco
- mais SQL manual
- mais responsabilidade da aplicação sobre projeções e compatibilidade entre providers
- menos automação do que um ORM completo
