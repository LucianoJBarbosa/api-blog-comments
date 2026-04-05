# 0001 - OpenAPI runtime com Scalar

Versao em ingles: [0001-runtime-openapi-with-scalar.en.md](0001-runtime-openapi-with-scalar.en.md)

- Status: aceito

## Contexto

A API precisava expor um contrato OpenAPI consumível e uma interface de consulta alinhada ao runtime.

## Decisão

Usar OpenAPI gerado em runtime com Scalar como interface interativa.

Na prática, isso significa:

- documento runtime em `/docs/openapi/v1.json`
- interface interativa em `/docs`
- manutenção de `OpenAPI.yaml` como artefato estático do repositório

## Consequências

- alinhamento com o ecossistema atual do .NET 10
- separação clara entre documento e interface de consulta
- menor familiaridade para equipes acostumadas com Swashbuckle
- necessidade de pequenos ajustes de compatibilidade no documento runtime
