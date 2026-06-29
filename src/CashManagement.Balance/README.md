# CashManagement.Balance

Serviço de **Consolidado Diário** (Balance) do domínio Cash Management.
Responsável por consumir os eventos publicados por `CashManagement.Entries` e
manter uma projeção (read model) do saldo diário consolidado.

Ver justificativa arquitetural completa em
[`/docs/ARCHITECTURE.md`](../../../docs/ARCHITECTURE.md).

## Estrutura (Clean Architecture simplificada)

Este serviço não possui lógica de negócio rica — é essencialmente um
**projetor de eventos** (lado de consulta do CQRS). Por isso, não há camada de
`Domain` separada: não existem agregados, invariantes ou regras complexas a
proteger, apenas a construção de uma projeção a partir de eventos já
validados pelo serviço de Entries. Manter uma camada de Domain vazia aqui
seria estrutura por estrutura, sem ganho real — daí a estrutura ser
deliberadamente mais simples (3 camadas) e não um espelho do Entries.

```
CashManagement.Balance.Application/    # Projeção e consulta (organizadas por vertical slice)
└── Features/                          # Uma pasta por caso de uso
    ├── BalanceProjection/            # Handlers que projetam eventos do Kafka (ex: EntryPostedEventHandler — crédito/débito)
    └── GetDailyBalance/              # Query + handler + DTO da consulta (ex: GetDailyBalanceQuery, DailyBalanceDto)

CashManagement.Balance.Infrastructure/ # Implementações concretas
├── Persistence/                       # Acesso ao MongoDB (read model)
└── Messaging/                         # Consumer Kafka

CashManagement.Balance.Api/            # Camada de entrada
├── Controllers/                       # Endpoints REST (ex: GET /balances/{date})
├── Auth/                              # JWT: token de dev + checagem de scope
├── Correlation/                       # Middleware de correlationId
├── Http/                              # Middleware de erros → envelope padrão (§4.4)
└── Configuration/                     # Composition root (DI, health)

tests/
├── CashManagement.Balance.UnitTests/         # Testes de application (isolados)
└── CashManagement.Balance.IntegrationTests/  # Testes de API/infra (Mongo, Kafka)
```

### Regra de dependência

`Api → Application`, com `Infrastructure` implementando interfaces definidas
em `Application` (inversão de dependência).

### Convenção de nomenclatura

Este serviço apenas **consome** eventos publicados por
`CashManagement.Entries` — não cria Commands nem Events próprios. Os nomes de
evento consumidos (`CreditPostedEvent`, `DebitPostedEvent`, etc.) seguem a
mesma convenção e vocabulário ubíquo definidos em
[`/docs/ARCHITECTURE.md`](../../../docs/ARCHITECTURE.md#13-linguagem-ubíqua-ubiquitous-language-e-convenções-de-nomenclatura).

## Como rodar localmente

Ver instruções na raiz do repositório (`docker-compose up --build`).
