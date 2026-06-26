using CashManagement.Entries.Domain.SeedWork;

namespace CashManagement.Entries.UnitTests.SeedWork;

// Test doubles para exercitar a base de Event Sourcing (AggregateRoot/DomainEvent)
// sem depender de um agregado real do domínio (Entry vem na T04).

public sealed record FakeRegisteredEvent(Guid AggregateId, string Data) : DomainEvent(AggregateId);

public sealed record FakeUnregisteredEvent(Guid AggregateId) : DomainEvent(AggregateId);

public sealed class FakeAggregate : AggregateRoot
{
    public string? AppliedData { get; private set; }
    public int AppliedCount { get; private set; }

    // Expõe Emit (protected) para o teste disparar uma emissão.
    public void DoSomething(Guid id, string data) => Emit(new FakeRegisteredEvent(id, data));

    // Emite um evento sem handler registrado, para verificar o comportamento de rota ausente.
    public void EmitUnregistered(Guid id) => Emit(new FakeUnregisteredEvent(id));

    protected override void RegisterEvents() =>
        On<FakeRegisteredEvent>(e =>
        {
            AppliedData = e.Data;
            AppliedCount++;
        });
}
