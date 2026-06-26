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
├── Aggregates/                        # Aggregate Roots (ex: Entry)
├── ValueObjects/                      # Value Objects (ex: Money, EntryType)
├── Events/                            # Domain Events (ex: CreditPostedEvent, DebitPostedEvent)
└── Repositories/                      # Interfaces de repositório (ex: IEntryRepository)

CashManagement.Entries.Application/    # Casos de uso, orquestração
├── Commands/                          # Command Handlers (ex: PostCreditCommand)
├── DTOs/                              # Objetos de entrada/saída da camada de aplicação
├── Validators/                        # Validação de comandos
└── Interfaces/                        # Contratos consumidos pela Application (ex: IEventPublisher)

CashManagement.Entries.Infrastructure/ # Implementações concretas
├── Persistence/                       # Event Store (SQL Server) — DbContext, migrations, tabela outbox
├── Messaging/                         # Producer Kafka + relay da outbox
└── Repositories/                      # Implementação concreta dos repositórios do Domain

CashManagement.Entries.Api/            # Camada de entrada
├── Controllers/                       # Endpoints REST (ex: POST /entries)
└── Middleware/                        # Autenticação JWT, tratamento de erros

tests/
├── CashManagement.Entries.UnitTests/         # Testes de domínio e application (isolados)
└── CashManagement.Entries.IntegrationTests/  # Testes de API/infra (banco, Kafka)
```

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
    private bool _isReversed;

    private Entry() { }                          // reconstrução por replay

    public static Entry PostCredit(Guid id, Money amount, DateTime occurredAt)
    {
        var entry = new Entry();
        entry.Emit(new CreditPostedEvent(id, amount, occurredAt));
        return entry;
    }

    public Result Reverse()
    {
        if (_isReversed)
            return Result.Fail(new Error("ENTRY_ALREADY_REVERSED",
                                         "Entry already reversed.", ErrorType.Conflict));
        Emit(new EntryReversedEvent(Id));
        return Result.Ok();
    }

    protected override void RegisterEvents()               // só eventos mutam estado
    {
        On<CreditPostedEvent>(e => { _type = EntryType.Credit; _amount = e.Amount; });
        On<DebitPostedEvent>(e  => { _type = EntryType.Debit;  _amount = e.Amount; });
        On<EntryReversedEvent>(_ => _isReversed = true);
    }
}
```

O **mediator** (dispatcher próprio) captura o comando e **devolve resultado**;
numa criação, o resultado é o **id** (não o agregado — não se vaza o write
model):

```csharp
public interface ICommand<TResult> { }

public interface ICommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<Result<TResult>> HandleAsync(TCommand command);
}

public interface ICommandDispatcher
{
    Task<Result<TResult>> Send<TResult>(ICommand<TResult> command);
}
```

O handler apenas **orquestra** (a regra está no agregado). O `Guid` nasce aqui
(camada de aplicação, via `IIdGenerator` — mockável no teste), nunca no banco:

```csharp
public sealed record PostCreditCommand(decimal Amount, DateTime OccurredAt)
    : ICommand<Guid>;

public sealed class PostCreditCommandHandler : ICommandHandler<PostCreditCommand, Guid>
{
    private readonly IEntryRepository _repository;
    private readonly IIdGenerator _ids;

    public PostCreditCommandHandler(IEntryRepository repository, IIdGenerator ids)
        => (_repository, _ids) = (repository, ids);

    public async Task<Result<Guid>> HandleAsync(PostCreditCommand cmd)
    {
        var id = _ids.New();                                          // id do stream / partition key
        var entry = Entry.PostCredit(id, Money.Of(cmd.Amount, "BRL"), cmd.OccurredAt);
        await _repository.SaveAsync(entry);   // eventos + outbox na mesma transação (§5.9)
        return Result.Ok(id);                 // o dispatcher devolve Result<Guid>
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

> `SaveAsync` grava os eventos não-commitados do agregado no event store **e** o
> registro de `outbox` na mesma transação, com verificação de *expected version*
> (concorrência otimista). O relay publica no Kafka — ver Transactional Outbox na
> [§5.9](../../../docs/ARCHITECTURE.md#59-transactional-outbox-publicação-confiável).
>
> Num *retry* com a mesma `Idempotency-Key`, o dispatcher devolve o **id
> original** (a deduplicação é um *behavior* em volta do `Send`), sem recriar o
> agregado.

## Como rodar localmente

Ver instruções na raiz do repositório (`docker-compose up --build`).
