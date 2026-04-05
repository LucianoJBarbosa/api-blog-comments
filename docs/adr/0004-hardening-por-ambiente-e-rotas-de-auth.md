# 0004 - Hardening por ambiente e rotas de autenticação

Versao em ingles: [0004-environment-hardening-on-auth-routes.en.md](0004-environment-hardening-on-auth-routes.en.md)

- Status: aceito

## Contexto

Uma base orientada a produção não deve depender de credenciais padrão, bootstrap implícito ou autenticação automática em ferramenta de documentação.

## Decisão

Remover conveniências implícitas de desenvolvimento do runtime padrão e adicionar proteção transversal às rotas de autenticação.

Na prática, isso significa:

- ausência de seed automático para usuário administrativo
- ausência de auto-login no Scalar
- exigência de segredo JWT real por configuração externa
- rate limiting dedicado para `login` e `register`
- possibilidade de controlar esse comportamento por configuração

## Consequências

- separação mais clara entre ambiente local e ambiente de produção
- endurecimento tratado na infraestrutura
- testes e automações precisam neutralizar ou configurar o rate limiting para evitar falsos negativos
- o ambiente precisa de bootstrap operacional explícito para usuários administrativos e segredos
