# Backlog Técnico da API

Versao em ingles: [api-technical-backlog.en.md](api-technical-backlog.en.md)

Backlog priorizado para a próxima fase do projeto. A base atual já cobre autenticação, autorização simples, paginação, audit timestamps, migrações versionadas, observabilidade minima, OpenAPI runtime e testes de integração.

O backlog abaixo trata apenas do que ainda permanece como evolução estrutural relevante. A ideia aqui não é reabrir a arquitetura do boilerplate, mas orientar a próxima rodada de endurecimento técnico.

## Estado atual

- boilerplate funcional para APIs pequenas e medias
- execução local simples com SQLite
- base de autenticação e autorização estabilizada
- migrações versionadas e tooling local explícito
- observabilidade minima já presente

## Agora

### 1. Migrar o runtime principal para SQL Server

- Impacto: alto
- Risco: médio
- Esforço: médio
- Motivo: SQLite atende desenvolvimento, testes e demo local, mas não é a base adequada para crescimento consistente de concorrência, observabilidade e tuning.
- Entrega esperada:
	- configuração operacional padrão em SQL Server
	- validação de migrações e compatibilidade no provider principal
	- documentação local deixando SQLite como conveniência de desenvolvimento, não como runtime alvo

### 2. Remover carregamento integral de comentários no detalhe do post

- Impacto: alto
- Risco: médio
- Esforço: médio
- Motivo: o detalhe do post ainda embute payload variável sem limite conforme o volume de comentários cresce.
- Entrega esperada:
	- detalhe do post sem coleção completa de comentários
	- comentários consumidos pela rota paginada já existente
	- contrato e documentação atualizados para o novo comportamento

### 3. Mover ownership checks para operações SQL de escrita

- Impacto: médio
- Risco: médio
- Esforço: médio
- Motivo: o fluxo atual faz leitura prévia do recurso e depois update/delete, o que amplia round-trips e abre janela de concorrência.

## Em seguida

### 4. Revisar rate limiting para múltiplas instâncias

- Impacto: alto
- Risco: médio
- Esforço: médio
- Motivo: o rate limiting atual é local ao processo e insuficiente como controle global em ambiente distribuído.

### 5. Aplicar rate limiting por IP e por identidade de login

- Impacto: médio
- Risco: baixo
- Esforço: baixo
- Motivo: limitar apenas por IP é insuficiente para cenários com NAT, proxy ou ataque distribuído a usernames específicos.

### 6. Tratar forwarded headers na borda HTTP

- Impacto: médio
- Risco: médio
- Esforço: baixo
- Motivo: IP real do cliente e coerência de HTTPS dependem disso quando a API estiver atrás de proxy.

## Depois

### 7. Tornar parâmetros do Argon2id configuráveis por ambiente

- Impacto: médio
- Risco: baixo
- Esforço: baixo
- Motivo: o custo de CPU e memória do login deve ser calibrado com base no ambiente e na capacidade operacional.

### 8. Adicionar métricas de latência e status code

- Impacto: alto
- Risco: baixo
- Esforço: médio
- Motivo: crescimento de tráfego exige leitura objetiva de latência, erro e saturação.

### 9. Adicionar tracing básico de requisições e acesso a dados

- Impacto: médio
- Risco: baixo
- Esforço: médio
- Motivo: correlação entre request, autenticação e query é essencial quando o volume cresce.

## Opcional para a próxima fase

### 10. Definir estratégia de refresh token

- Impacto: médio
- Risco: médio
- Esforço: médio
- Motivo: JWT puramente stateless simplifica a base inicial, mas limita cenários de sessão mais controlada.

### 11. Definir estratégia de revogação de token

- Impacto: médio
- Risco: médio
- Esforço: médio
- Motivo: logout efetivo, bloqueio de sessão e invalidação seletiva exigem um modelo adicional ao token atual.

### 12. Avaliar cache para leituras públicas quentes

- Impacto: médio
- Risco: médio
- Esforço: médio
- Motivo: listagens públicas tendem a concentrar mais leitura do que escrita em cenários de crescimento.

## Fora do backlog imediato

- reestruturação arquitetural ornamental
- troca de Dapper por ORM completo sem pressão funcional ou operacional concreta
- modelo de permissão fina antes de haver necessidade real de domínio

## Sequência recomendada

1. SQL Server + detalhe de post sem comentários completos
2. Ownership em SQL + endurecimento de borda HTTP
3. Métricas, tracing e tuning de autenticação
4. Evolução de sessão, revogação e cache