# 0005 - Estratégia de testes de integração

Versao em ingles: [0005-integration-test-strategy.en.md](0005-integration-test-strategy.en.md)

- Status: aceito

## Contexto

O comportamento final da API depende de middleware, autenticação, autorização, documentação runtime e persistência.

## Decisão

Adotar testes de integração HTTP com `WebApplicationFactory`, banco SQLite isolado por suíte e reset explícito de estado.

Na prática, isso significa:

- subir a aplicação em memória para os testes
- exercitar rotas HTTP reais
- validar autenticação, ownership, OpenAPI e Scalar
- controlar detalhes de infraestrutura de teste, como o rate limiting

## Consequências

- validação mais próxima do uso real da API
- cobertura de regressões em fluxos completos
- execução mais lenta do que testes unitários puros
- maior sensibilidade a configuração de ambiente e bootstrap do host de teste
