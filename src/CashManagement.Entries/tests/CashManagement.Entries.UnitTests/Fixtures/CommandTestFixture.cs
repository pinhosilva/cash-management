using CashManagement.Entries.Application.Abstractions;
using CashManagement.Entries.Application.Interfaces;
using CashManagement.Entries.Domain.Persistence;
using CashManagement.Entries.Domain.SeedWork;
using Moq;
using Xunit;

namespace CashManagement.Entries.UnitTests.Fixtures;

/// <summary>
/// Fixture para command handlers: automocka o event store e o gerador de id,
/// executa o handler sobre o comando do <c>When()</c> e expõe o
/// <see cref="Result"/> e os <see cref="PublishedEvents"/> para asserção.
/// O handler é montado pelo teste (sem reflection) via <see cref="CreateHandler"/>.
/// </summary>
public abstract class CommandTestFixture<TCommand, THandler, TAggregate> : IAsyncLifetime
    where TCommand : ICommand<Guid>
    where THandler : ICommandHandler<TCommand, Guid>
    where TAggregate : AggregateRoot
{
    protected Guid GeneratedId { get; } = Guid.NewGuid();

    protected Mock<IEventStore> EventStore { get; } = new();

    protected IReadOnlyCollection<IDomainEvent> PublishedEvents { get; private set; } = [];

    protected Result<Guid> Result { get; private set; } = default!;

    protected CommandTestFixture() =>
        EventStore
            .Setup(s => s.Append(It.IsAny<AggregateRoot>()))
            .Callback<AggregateRoot>(aggregate => PublishedEvents = aggregate.UncommittedEvents.ToList());

    public async Task InitializeAsync()
    {
        var idGenerator = new Mock<IIdGenerator>();
        idGenerator.Setup(g => g.New()).Returns(GeneratedId);

        var handler = CreateHandler(EventStore.Object, idGenerator.Object);
        Result = await handler.HandleAsync(When());
    }

    public Task DisposeAsync() => Task.CompletedTask;

    protected abstract THandler CreateHandler(IEventStore eventStore, IIdGenerator idGenerator);

    protected abstract TCommand When();
}
