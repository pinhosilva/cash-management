# CashManagement.Entries

Serviço de **Lançamentos** (Entries) do domínio Cash Management. Responsável
por registrar débitos e créditos individuais, atuando como fonte da verdade
financeira via Event Sourcing.

Ver justificativa arquitetural completa em
[`/docs/ARCHITECTURE.md`](../../../docs/ARCHITECTURE.md).

## Estrutura (Clean Architecture + DDD)

Este serviço concentra a maior parte da lógica de negócio do domínio
(agregados, invariantes, eventos), por isso usa Clean Architecture completa,
em 4 camadas:

```
CashManagement.Entries.Domain/         # Núcleo do domínio — sem dependências externas
├── SeedWork/                          # Building blocks de ES (AggregateRoot, DomainEvent, Result/Error)
├── Aggregates/                        # Aggregate Roots (ex: Entry)
├── ValueObjects/                      # Value Objects (ex: Money, EntryType)
├── Events/                            # Domain Events (ex: CreditPostedEvent)
└── Persistence/                       # Portas de persistência (IRepository, IUnitOfWork)

CashManagement.Entries.Application/    # Casos de uso (organizados por vertical slice)
├── Abstractions/                      # Building blocks de CQRS (ICommand, ICommandHandler, ICommandDispatcher)
├── Interfaces/                        # Portas consumidas pela Application (ex: IIdGenerator, IEventPublisher)
└── Features/                          # Uma pasta por caso de uso (vertical slice)
    └── PostCredit/                    # Command + Handler + Validator (+ DTOs) do caso de uso, juntos

CashManagement.Entries.Infrastructure/ # Implementações concretas
├── Persistence/                       # EF Core: DbContext, Repository, UnitOfWork + Models/ (POCOs) e Configurations/ (mapeamentos)
├── Serialization/                     # (de)serialização de eventos ↔ JSON
└── Messaging/                         # Producer Kafka + relay da outbox

CashManagement.Entries.Api/            # Camada de entrada
├── Controllers/                       # Endpoints REST (ex: POST /entries)
└── Middleware/                        # Autenticação JWT, tratamento de erros

tests/
├── CashManagement.Entries.UnitTests/         # Testes de domínio e application (isolados)
└── CashManagement.Entries.IntegrationTests/  # Testes de API/infra (banco, Kafka)
```

> **Organização por vertical slice:** dentro da Application, cada caso de uso vive
> numa pasta em `Features/` reunindo command, handler, validator (e DTOs) — o que
> muda junto fica junto (alta coesão). Os building blocks de CQRS ficam em
> `Abstractions/` e as portas em `Interfaces/`. O **Domain** continua organizado
> por tipo de DDD (vertical slice é conceito da camada de aplicação).

### Building blocks (próprios)

O `Entry` herda de uma classe `AggregateRoot`, os eventos de `DomainEvent` e os
handlers de `ICommandHandler<T>` — abstrações **próprias** (numa pasta de *seed
work* do `Domain`), implementadas dentro do projeto, sem dependência externa. A
justificativa e o escopo estão na
[seção 5.8 do ARCHITECTURE.md](../../../docs/ARCHITECTURE.md#58-building-blocks-de-domínio-próprios).

### Regra de dependência

`Api → Application → Domain`, com `Infrastructure` implementando interfaces
definidas em `Domain`/`Application` (inversão de dependência). O `Domain` não
referencia nenhuma outra camada.

### Convenção de nomenclatura

Commands no imperativo (`PostCreditCommand`), Events no particípio passado
(`CreditPostedEvent`). Ver a tabela completa de vocabulário ubíquo e a
justificativa dessa convenção em
[`/docs/ARCHITECTURE.md`](../../../docs/ARCHITECTURE.md#13-linguagem-ubíqua-ubiquitous-language-e-convenções-de-nomenclatura).

## Modelo de domínio (sketches)

> Conceito, mapa de padrões (GoF) e SOLID na
> [seção 5.10 do ARCHITECTURE.md](../../../docs/ARCHITECTURE.md#510-modelo-de-domínio-e-padrões-de-implementação-entries).

O agregado `Entry` é **event-sourced**: o comportamento valida invariantes e
**emite** eventos; só os eventos mutam o estado, via `On<TEvent>()` explícito.
Falhas de regra de negócio usam **`Result`** (sem exceção) — o `Error` carrega
`code`, `message` e um `ErrorType` que vira o HTTP status (§4.4):

```csharp
public sealed record Error(string Code, string Message, ErrorType Type);
public enum ErrorType { Validation, Conflict, NotFound, Unauthorized, Forbidden }
// Result / Result<T>: Ok() · Ok(value) · Fail(error) · IsSuccess · Value · Error
```

```csharp
public sealed class Entry : AggregateRoot
{
    private EntryType _type;
    private Money _amount;

    private Entry() { }                          // reconstrução por replay

    public static Entry PostCredit(Guid id, Money amount, DateTime occurredAt)
    {
        var entry = new Entry();
        entry.Emit(new CreditPostedEvent(id, amount, occurredAt));
        return entry;
    }

    public static Entry PostDebit(Guid id, Money amount, DateTime occurredAt)
    {
        var entry = new Entry();
        entry.Emit(new DebitPostedEvent(id, amount, occurredAt));
        return entry;
    }

    protected override void RegisterEvents()               // só eventos mutam estado
    {
        On<CreditPostedEvent>(e => { _type = EntryType.Credit; _amount = e.Amount; });
        On<DebitPostedEvent>(e  => { _type = EntryType.Debit;  _amount = e.Amount; });
    }
}
```

> O **estorno** (`Reverse()` → `EntryReversedEvent`, marcando o lançamento como
> revertido) é **fatia futura** — será um **evento compensatório** (não muta nem
> apaga o lançamento original). Ver _Próximas fatias_ em
> [`/docs/TASKS.md`](../../../docs/TASKS.md).

O **mediator** (dispatcher próprio) captura o comando e **devolve resultado**;
numa criação, o resultado é o **id** (não o agregado — não se vaza o write
model):

```csharp
public interface ICommand<TResult> { }

public interface ICommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

public interface ICommandDispatcher
{
    Task<Result<TResult>> Send<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResult>;
}
```

O handler apenas **orquestra** (a regra está no agregado). O `Guid` nasce aqui
(camada de aplicação, via `IIdGenerator` — mockável no teste), nunca no banco:

```csharp
public sealed record PostCreditCommand(decimal Amount, DateTime OccurredAt)
    : ICommand<Guid>;

public sealed class PostCreditCommandHandler : ICommandHandler<PostCreditCommand, Guid>
{
    private readonly IRepository _repository;
    private readonly IIdGenerator _ids;
    private readonly PostCreditCommandValidator _validator;

    public PostCreditCommandHandler(IRepository repository, IIdGenerator ids, PostCreditCommandValidator validator)
        => (_repository, _ids, _validator) = (repository, ids, validator);

    public Task<Result<Guid>> HandleAsync(PostCreditCommand cmd, CancellationToken ct = default)
    {
        var validation = _validator.Validate(cmd);                    // valida antes de tocar o domínio
        if (validation.IsFailure)
            return Task.FromResult(Result.Fail<Guid>(validation.Error!));

        var id = _ids.New();                                          // id do stream / partition key
        var entry = Entry.PostCredit(id, Money.Of(cmd.Amount, "BRL"), cmd.OccurredAt);
        _repository.Add(entry);               // persiste o agregado; event store + outbox por baixo (§5.9)
        return Task.FromResult(Result.Ok(id));// o commit é do IUnitOfWork, na fronteira do caso de uso
    }
}
```

A controller fica fina — só HTTP, sem gerar id nem ter regra:

```csharp
[HttpPost("/entries")]
public async Task<IActionResult> Post(PostEntryDto dto)
{
    Result<Guid> result = await _dispatcher.Send(new PostCreditCommand(dto.Amount, dto.OccurredAt));
    return result.ToResponse(this);   // Result → envelope + status (201/4xx) — tradutor único na borda
}
```

> `ToResponse` é o **único** ponto que traduz `Result` → envelope padronizado
> (§4.4): sucesso → `201`/`200` + `{ status, result }`; falha → status do
> `ErrorType` + `{ status, error }`. Exceção não-tratada vira `500` no mesmo
> envelope (middleware global).

> `IRepository.Add` **encena** os eventos não-commitados do agregado **e** a
> linha de `outbox` (envelope §4.3); o **commit** é do `IUnitOfWork`, acionado uma
> vez na **fronteira do caso de uso** (request na API, ou um orquestrador num
> "pacotão" de comandos) — fora do dispatcher, que só despacha (SRP). A
> concorrência otimista (*expected version*) é verificada no commit. O relay publica no Kafka — ver
> Transactional Outbox na
> [§5.9](../../../docs/ARCHITECTURE.md#59-transactional-outbox-publicação-confiável).
>
> Num *retry* com a mesma `Idempotency-Key`, o dispatcher devolve o **id
> original** (a deduplicação é um *behavior* em volta do `Send`), sem recriar o
> agregado.

## Configuração

Tudo que muda por ambiente ou é knob de operação vive no `appsettings.json` (a chave
de assinatura JWT vem de **variável de ambiente / secret**, nunca commitada). Premissas
de negócio (moeda **BRL**, scope `entries:write`) ficam em código, de propósito.

| Chave | Default | O que é |
|---|---|---|
| `ConnectionStrings:Entries` | SQL local | Connection string do event store |
| `Kafka:BootstrapServers` | `localhost:9092` | Brokers do Kafka |
| `Kafka:Topic` | `cash.management.entries.events` | Tópico de publicação do relay |
| `Jwt:Issuer` / `Jwt:Audience` | `cash-management` / `cash-management-entries` | Validação do JWT |
| `ENTRIES_JWT_SIGNING_KEY` (env) | — | Chave de assinatura; **obrigatória fora de Development** |
| `Outbox:PollIntervalSeconds` | `2` | Intervalo de polling do relay |
| `Idempotency:WindowHours` | `24` | Janela de dedup de idempotência |

### Feature flags (seção `Features`)

Cada flag é **anulável**: `null` = segue o ambiente (`!Production`); `true`/`false` = força.

| Flag | `null` ⇒ | Efeito |
|---|---|---|
| `Features:Swagger` | Swagger fora de produção | Liga/desliga o Swagger UI |
| `Features:DevTokenEndpoint` | `/dev/token` fora de produção | Liga/desliga o emissor de token de dev — **sempre 404 em produção** |
| `Features:AutoCreateSchema` | `EnsureCreated` fora de produção | Cria o schema no startup (em prod use migrations) |

## Como rodar localmente

Ver instruções na raiz do repositório (`docker-compose up --build`).
