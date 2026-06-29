using CashManagement.Entries.Domain.SeedWork;

namespace CashManagement.Entries.UnitTests.Fixtures;

/// <summary>
/// Fixture Given/When/Then para agregados event-sourced.
/// <list type="bullet">
///   <item><c>Given()</c> = histórico de eventos (cenário de replay);</item>
///   <item><c>When()</c> = ação de criação sob teste (cenário de criação);</item>
///   <item>Then = asserção sobre <see cref="PublishedEvents"/> ou o estado do
///   <see cref="AggregateRoot"/>.</item>
/// </list>
/// A reidratação usa <c>AggregateRoot.FromHistory</c> (reflection no load).
/// </summary>
public abstract class AggregateTestFixture<TAggregate>
    where TAggregate : AggregateRoot
{
    private TAggregate? _sut;

    /// <summary>SUT: reconstruído do <c>Given()</c> (replay) ou produzido pelo <c>When()</c> (criação).</summary>
    protected TAggregate AggregateRoot => _sut ??= Build();

    /// <summary>Eventos emitidos e ainda não commitados do SUT.</summary>
    protected IReadOnlyCollection<IDomainEvent> PublishedEvents => AggregateRoot.UncommittedEvents;

    /// <summary>Histórico do agregado (cenário de replay). Vazio por padrão.</summary>
    protected virtual IEnumerable<IDomainEvent> Given() => [];

    /// <summary>Ação de criação sob teste. Sobrescreva em cenários de criação.</summary>
    protected virtual TAggregate When() =>
        throw new InvalidOperationException("Override Given() (replay) ou When() (criação).");

    private TAggregate Build()
    {
        var history = Given().ToList();
        return history.Count > 0
            ? CashManagement.Entries.Domain.SeedWork.AggregateRoot.FromHistory<TAggregate>(history)
            : When();
    }
}
