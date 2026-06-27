# Guia de Implementação — Fatia Vertical 1 (Crédito ponta-a-ponta)

Este guia traduz o [`ARCHITECTURE.md`](./ARCHITECTURE.md) em **tarefas
executáveis e ordenadas**. É escrito para o **executor** **implementar**, com o
**revisor (desenvolvedor)** avaliando e liberando cada passo.

> **Escopo desta fatia:** registrar um **crédito** ponta-a-ponta —
> `POST /entries` → event store + outbox (mesma transação) → relay → Kafka →
> consumer do Balance → projeção (Mongo) → `GET /balances/{date}`, **com testes**.
>
> **Fora de escopo agora** (NÃO implementar — vira fatias seguintes): débito,
> estorno, multi-moeda, multi-tenant, snapshot, dead-letter queue, cache Redis,
> observabilidade completa. Implemente **só o caminho do crédito**.

---

## Parte 1 — Regras de Engajamento (como trabalhar)

O **executor** **deve** seguir estas regras. Elas valem para todas as tarefas.

1. **Uma tarefa por vez, na ordem.** Não pule etapas; cada tarefa depende da anterior.
2. **TDD obrigatório (Red → Green → Refactor).** Escreva o **teste primeiro**, veja-o falhar, implemente o mínimo para passar, refatore. O teste é parte da entrega.
3. **Commits pequenos, [Conventional Commits](https://www.conventionalcommits.org/).** Um commit por mudança lógica. Prefixos: `test:`, `feat:`, `refactor:`, `chore:`, `fix:`, `docs:`. Ex.: `test: Entry rejeita crédito com valor não-positivo` seguido de `feat: invariante de valor positivo no Entry`.
4. **Respeite a regra de dependência (Clean Architecture).** `Api → Application → Domain`; `Infrastructure` implementa interfaces de `Domain`/`Application`. O `Domain` **não** referencia nenhuma outra camada. (ver §1.2)
5. **Respeite a linguagem ubíqua.** Código em **inglês**; nomes conforme a tabela da §1.3 (Entry, Credit, `PostCreditCommand`, `CreditPostedEvent`…). Docs em PT.
6. **Bata sempre com o `ARCHITECTURE.md`.** Se algo na tarefa conflitar com o doc, ou for **ambíguo**, **PARE e pergunte** — não invente design nem "melhore" por conta própria.
7. **Sem over-engineering.** Implemente **só o que a tarefa pede**. Nada de generalizar para casos fora do escopo desta fatia.
8. **Configuração e versões.** `net10.0`, **C# 14**, `Nullable` habilitado, `TreatWarningsAsErrors`. Sem segredo no repo (use variáveis de ambiente; `.env` já está no `.gitignore`).
9. **"Pronto" = critério de aceite verde.** Uma tarefa só fecha quando os testes dela passam (`dotnet test`).
10. **Nível de detalhe deste guia é híbrido:** tarefas críticas vêm com passo a passo; as triviais vêm por objetivo + aceite. Onde estiver objetivo-orientado, use o `ARCHITECTURE.md` como referência do "como".

---

## Parte 2 — Backlog de Tarefas (ordenado)

Legenda de detalhe: 🔬 **granular** (siga à risca) · 🎯 **objetivo-orientado** (você decide o "como").

### T01 — Setup da solução e qualidade 🎯

**Objetivo:** esqueleto compilável dos dois serviços + projetos de teste, com versões e qualidade travadas.

> **Cronologia de commits:** esta tarefa **cria e versiona a estrutura de `src/`**
> — ela **não** faz parte do commit inicial (que é apenas documentação).

**Passos:**
- Criar `Directory.Build.props` na raiz com: `<TargetFramework>net10.0</TargetFramework>`, `<LangVersion>14</LangVersion>`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.
- Criar `.editorconfig` (convenções C#, ordenação de usings, estilo).
- Criar as duas solutions (`CashManagement.Entries.sln`, `CashManagement.Balance.sln`) e os projetos na estrutura de `src/` (ver §1.1), com os READMEs de cada serviço. Test projects: xUnit.
- Pacotes base: xUnit, Moq, FluentAssertions (ou Shouldly).

**Critério de aceite:** `dotnet build` sem warnings/erros nas duas solutions; `dotnet test` roda (0 testes ainda) verde.

---

### T02 — Seed work do domínio (base de Event Sourcing) 🔬

**Objetivo:** as classes-base próprias de ES, em `CashManagement.Entries.Domain/SeedWork/`.

**Teste primeiro** (`Entries.UnitTests`):
- `Emit` adiciona o evento a `UncommittedEvents` **e** aplica a mutação.
- `LoadFromHistory(eventos)` reconstrói o estado **sem** popular `UncommittedEvents`.
- `On<T>` não registrado para um evento → comportamento definido (lançar ou ignorar — **decida e documente**; recomendo lançar em DEBUG).

**Implementar:**
- `DomainEvent` (abstrata): carrega `AggregateId` (Guid).
- `AggregateRoot` (abstrata): `Guid Id` (setter privado), `int Version`, `IReadOnlyCollection<IDomainEvent> UncommittedEvents`, `Emit(evento)` **`protected`**, `LoadFromHistory(...)`, `ClearUncommittedEvents()`, e o registro `On<TEvent>(Action<TEvent>)` chamado em `RegisterEvents()` (abstrato).
- `Result` / `Result<T>` + `Error` (`code`, `message`, `ErrorType`): resultado de operação **sem exceção** — base do contrato de resposta (§4.4) e do Result pattern (§5.10).

**Cuidados (armadilhas a evitar — boas práticas obrigatórias):**
- `Emit`/`RaiseEvent` é **`protected`**: só o próprio agregado emite seus eventos — invariante **não** pode ser furada de fora.
- Identidade é **só o `Guid`** (stream id), setter privado; **não** colocar id de banco (`int`) no agregado.
- **Ordenação por `Version`** (incrementada a cada evento), **nunca** por relógio/`Ticks`/`Task.Delay` (sem qualquer *delay* no apply).
- Timestamps em **UTC** (`DateTime.UtcNow` ou um `IClock` injetável), **nunca** `DateTime.Now`.
- Roteamento de eventos por **composição** (dicionário privado encapsulado), **não** herdar de `Dictionary`.
- **Sem reflection no caminho de execução (hot path)** — ex.: igualdade de Value Object a cada comparação (use `record`) e roteamento de evento (use `On<T>()`). Reflection **é ok** onde é normal e barata, fora do caminho quente: container de DI, serialização, descoberta de tipos no startup. E **sem** sync-over-async (`.GetAwaiter().GetResult()`) — inclusive nas fixtures.

**Critério de aceite:** testes acima verdes. `Domain` sem nenhuma dependência de pacote externo.

> Referência conceitual: §5.8 e §5.10.

---

### T03 — Value Objects: `Money` e `EntryType` 🔬

**Objetivo:** VOs imutáveis em `Domain/ValueObjects/`.

**Teste primeiro:**
- `Money.Of(100m, "BRL")` cria; igualdade por valor: `Money.Of(10,"BRL") == Money.Of(10,"BRL")`.
- Moeda fixa **BRL** nesta fatia (premissa de moeda única — §3).

**Implementar:** `Money` (amount + currency) e `EntryType` (Credit/Debit — só **Credit** é usado nesta fatia) em `Domain`. A regra de **valor positivo** é validada como `Result` (`ErrorType.Validation`) no validator da Application (T05), **não** como exceção; o `Money` mantém no máximo um *guard* defensivo (último recurso, não é o caminho de validação).

**Cuidados:** implemente os VOs como **`record`** (igualdade por valor nativa, imutável) — **não** igualdade por reflection sobre campos.

**Critério de aceite:** testes verdes.

---

### T04 — Agregado `Entry` (apenas crédito) 🔬

**Objetivo:** o agregado event-sourced com o comportamento de crédito.

**Teste primeiro** (use a fixture `AggregateTestFixture<Entry>` — crie-a em `Entries.UnitTests` se ainda não existir, no estilo Given/When/Then):
- *When* `Entry.PostCredit(id, Money.Of(100,"BRL"), occurredAt)` → emite **um** `CreditPostedEvent` com os dados certos.
- *Given* `CreditPostedEvent`, replay reconstrói o estado (type=Credit, amount=100).

> Validação de "valor positivo" **não** é do agregado — é `Result` no validator da Application (T05). O `Entry` assume entrada válida.

**Implementar:** `CreditPostedEvent` em `Domain/Events/`; `Entry : AggregateRoot` em `Domain/Aggregates/` com factory `PostCredit(...)` e `On<CreditPostedEvent>(...)` em `RegisterEvents()`. Ver sketch no [README do Entries](../src/CashManagement.Entries/README.md).

**Critério de aceite:** testes Given/When/Then verdes.

---

### T05 — Command, Handler e Dispatcher 🔬

**Objetivo:** o lado de aplicação que orquestra o crédito.

**Teste primeiro** (use `CommandTestFixture<PostCreditCommand, PostCreditCommandHandler, Entry>`):
- *When* `PostCreditCommand(amount, occurredAt)` válido → `PublishedEvents` contém **um** `CreditPostedEvent` e o handler retorna `Result<Guid>` de **sucesso** com o id.
- *When* valor **não-positivo** → o handler retorna `Result` de **falha** (`ErrorType.Validation`), **sem** publicar evento e **sem** lançar exceção.

**Implementar (em `Entries.Application`):**
- Abstrações: `ICommand<TResult>`, `ICommandHandler<TCommand,TResult>` (`Task<Result<TResult>> HandleAsync`), `ICommandDispatcher` (`Task<Result<TResult>> Send<TCommand, TResult>(TCommand)` — genérico sobre o comando, **reflection-free**). Dispatcher próprio, resolve handler via DI.
- `IIdGenerator` (interface) + impl simples (`Guid.NewGuid()`) na Infra/Api.
- `PostCreditCommand(decimal Amount, DateTime OccurredAt) : ICommand<Guid>`.
- **Validator** do comando (valor positivo, data válida) → devolve `Result` de falha (`ErrorType.Validation`) antes de tocar o domínio.
- `PostCreditCommandHandler` → gera id, `Entry.PostCredit(...)`, `_eventStore.Append(entry)` (sem commit — o commit é do `IUnitOfWork`, na fronteira do caso de uso), retorna `Result.Ok(id)`. (ver sketch no README do Entries)
- Portas consumidas: `IEventStore` (em `Domain/Persistence/`).

**Critério de aceite:** testes verdes; o handler não conhece SQL/Kafka (só interfaces).

---

### T06 — Event store + Outbox (SQL Server) 🔬

**Objetivo:** persistência append-only dos eventos **+** outbox na **mesma transação** (Transactional Outbox — §5.9).

**Teste primeiro** (`Entries.IntegrationTests` com **Testcontainers** SQL Server):
- `Append(entry)` + `CommitAsync()` gravam o evento na tabela de eventos **e** uma linha na `outbox`, **atomicamente** (se um falhar, nada persiste).
- `LoadAsync<Entry>(id)` reconstrói o `Entry` por replay dos eventos.
- Concorrência otimista: salvar com versão esperada divergente → conflito (mapear para 409 depois).

**Implementar (em `Entries.Infrastructure/Persistence`):**
- EF Core `DbContext` com tabelas `Events` (stream append-only) e `Outbox`.
- `EventStore : IEventStore` **encena** (sem commit) os eventos não-commitados + a linha de outbox (envelope da §4.3), de forma **genérica** (qualquer agregado), num só lugar.
- `UnitOfWork : IUnitOfWork`: `CommitAsync` faz o **commit atômico** (um `SaveChanges`); a *expected version* é garantida pelo índice único `(AggregateId, Version)`. Acionado 1× na **fronteira do caso de uso** (request na API / orquestrador num pacotão), **fora** do event store e do dispatcher.

**Critério de aceite:** testes de integração verdes; nenhuma escrita parcial possível.

> ⚠️ Atenção: o envelope gravado na outbox deve seguir o contrato da §4.3 (`event`, `aggregate`, `correlationId`, `occurredAt`, `data`).

---

### T07 — Relay da Outbox → Kafka 🔬

**Objetivo:** publicar de forma confiável os eventos da outbox no Kafka.

**Teste primeiro** (integração, Testcontainers Kafka):
- Dado uma linha não-publicada na `outbox`, o relay publica no tópico `cash.management.entries.events` (key = `aggregate.id`) e marca como publicada.
- Falha de publish → linha permanece não-publicada e é **reenviada** (at-least-once).

**Implementar:** um `BackgroundService` em `Entries.Infrastructure/Messaging` que faz polling da outbox, publica via produtor Kafka, marca publicado. Idempotência fica no consumidor (T09).

**Critério de aceite:** "gravou ⇒ publicado" comprovado em teste.

---

### T08 — API do Entries: `POST /entries` 🎯

**Objetivo:** expor o endpoint, fino, com auth e contrato de erro.

**Teste primeiro** (integração, `WebApplicationFactory`): `POST /entries` (crédito) com JWT válido → `201` + `Location` + envelope `{ status: "success", result: { id } }`; sem token → `401`; corpo inválido → `400` com envelope `{ status: "error", error }` (§4.4).

**Implementar:** controller fina mapeando `Result` → envelope via `ToResponse` (§4.4); JWT com **chave estática de dev** (§9) + endpoint utilitário de token; **filtro/tradutor único** `Result` → envelope + middleware global que transforma exceção inesperada em `500` no mesmo envelope, ecoando `correlationId`; `Idempotency-Key` (dedup 24h — §4.3); health checks `/health/live` e `/health/ready`; **expor Swagger UI (OpenAPI)** e anotar o endpoint com `[ProducesResponseType]` (tipo = o envelope) para cada status da §4.4 (201/400/401/409); **log estruturado básico** (Serilog) com `correlationId` + `component`, **sem PII/segredo** (§8.2 — observabilidade completa fica para fatia futura).

**Critério de aceite:** cenários de teste verdes; erros no formato da §4.4.

---

### T09 — Consumer + Projeção do Balance (idempotente) 🔬

**Objetivo:** consumir o evento e projetar o saldo do dia, **exactly-once de efeito**.

**Teste primeiro** (integração, Testcontainers Kafka + Mongo):
- Consumir `CreditPostedEvent` → documento `DailyBalance { date, totalCredits, totalDebits, balance }` atualizado no Mongo.
- **Idempotência:** entregar o **mesmo `event.id` duas vezes** → saldo conta **uma** vez só (upsert idempotente / dedup por `event.id`). (§4.3, §6.3)

**Implementar (em `Balance`):** consumer Kafka em `Infrastructure/Messaging`; `CreditPostedEventHandler` em `Application/Handlers` aplicando à projeção; persistência no Mongo em `Infrastructure/Persistence`. Dedup por `event.id` — o **marcador de dedup e o update do saldo no mesmo documento/transação Mongo** (atômico, sem dual-write no consumo — FAQ #5). **Log estruturado** com `correlationId` (propagado do evento) + `component=KafkaConsumer`, **sem PII/segredo** (§8.2).

**Critério de aceite:** reprocessar o mesmo evento é *no-op*; testes verdes.

---

### T10 — API do Balance: `GET /balances/{date}` 🎯

**Objetivo:** expor a consulta do saldo, lida direto da projeção.

**Teste primeiro** (integração): `GET /balances/{date}` com JWT → `200` + `{ date, totalCredits, totalDebits, balance }`; sem token → `401`.

**Implementar:** query + controller lendo do Mongo; JWT; health checks; **Swagger UI (OpenAPI)** com `[ProducesResponseType]` (200/401/404). Sem nunca chamar o Entries (§4.2).

**Critério de aceite:** testes verdes; resposta no formato do read model.

---

### T11 — Orquestração local (`docker-compose`) e validação ponta-a-ponta 🎯

**Objetivo:** subir tudo com um comando e validar a fatia de crédito de verdade.

**Implementar:** `docker-compose.yml` na raiz com SQL Server, MongoDB, Kafka e os dois serviços; observabilidade em `--profile observability` (§9). Atualizar o README da raiz com portas/exemplos de `curl`.

**Critério de aceite (a prova da fatia):** `docker-compose up --build` sobe tudo; rodando a pasta **`Smoke`** da collection (no Postman, ou via `newman --folder "Smoke" --delay-request 1000`), o fluxo **Obter token → Registrar crédito → Consultar saldo (com polling) → 401** passa, com o saldo refletindo o crédito. O **polling** no cenário de saldo é obrigatório — sem ele, a consistência eventual (a projeção é assíncrona) deixa o teste *flaky*.

---

### T12 — CI/CD: esteira com gate de teste (GitHub Actions) 🎯

**Objetivo:** automatizar a prova da fatia numa esteira que **bloqueia a subida** se algum teste falhar. Desenho completo na [§9.2 do ARCHITECTURE.md](./ARCHITECTURE.md#92-esteira-de-cicd-github-actions).

> **Cronologia de commits:** a **collection Postman** (`docs/postman/`) é
> versionada **neste passo**, junto com a esteira que a consome — não no commit
> inicial de docs.

**Implementar:** `.github/workflows/ci-cd.yml` (GitHub Actions) com:
- job `test` com os 3 gates — **unit** (`dotnet test`), **integração** (Testcontainers) e **e2e** (Newman rodando **só a pasta `Smoke`** — `--folder "Smoke" --delay-request 1000` — contra a stack do `docker compose up`; o catálogo completo da collection fica **fora** do gate);
- job `deploy` com `needs: test`, fazendo deploy **por ambiente conforme a branch** seguindo o **GitFlow** (develop→dev, `release/*`→staging, main→produção + tag) — ver §9.2;
- **versionamento automático** via `GitVersion.yml` (SemVer a partir dos commits semânticos): *bump* por `feat`/`fix`/`BREAKING`, canal por branch, tag `vX.Y.Z` criada na `main` + release notes geradas (§9.2);
- *branch protection* em `main` **e** `develop` exigindo a esteira verde e PR revisado.

**Critério de aceite:** PR com teste falhando **não** mergeia; push na `main` com tudo verde dispara o `deploy`. A esteira roda a pasta **`Smoke`** da **mesma** collection usada nos testes manuais (Newman = fonte única de verdade; o catálogo completo de endpoints fica **fora** do gate).

---

## Parte 3 — Definition of Done da fatia

A Fatia 1 está concluída quando:

- [ ] Todos os testes (unit + integração) passam (`dotnet test`).
- [ ] `docker-compose up` sobe a stack e a pasta **`Smoke`** do Postman (`newman --folder "Smoke"`, com polling no saldo) passa.
- [ ] A esteira de CI (T12) está **verde** — os 3 gates (unit, integração, e2e/Newman) passando.
- [ ] O código respeita a regra de dependência e a linguagem ubíqua.
- [ ] Histórico de commits pequenos e descritivos (conventional commits), refletindo o ciclo TDD.
- [ ] Nada fora de escopo foi implementado (sem débito/estorno/multi-tenant/etc.).
- [ ] O que divergiu do `ARCHITECTURE.md` (se algo) foi **levantado com o revisor**, não decidido sozinho.

> **Próximas fatias** (depois desta): débito → estorno → idempotência de escrita completa → segurança (scopes/ACLs Kafka) → observabilidade (OTel/OpenSearch) → testes de carga (k6). Cada uma ganha seu próprio bloco de tarefas quando chegar a vez.
