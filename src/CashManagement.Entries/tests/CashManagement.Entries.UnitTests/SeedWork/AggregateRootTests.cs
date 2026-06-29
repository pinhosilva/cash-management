using CashManagement.Entries.Domain.SeedWork;
using Shouldly;
using Xunit;

namespace CashManagement.Entries.UnitTests.SeedWork;

public class AggregateRootTests
{
    [Fact]
    public void Emit_adds_event_to_uncommitted_and_applies_mutation()
    {
        var id = Guid.NewGuid();
        var sut = new FakeAggregate();

        sut.DoSomething(id, "hello");

        sut.UncommittedEvents.ShouldHaveSingleItem().ShouldBeOfType<FakeRegisteredEvent>();
        sut.AppliedData.ShouldBe("hello");   // a mutação foi aplicada
        sut.Id.ShouldBe(id);
        sut.Version.ShouldBe(1);             // versão incrementada pelo evento
    }

    [Fact]
    public void LoadFromHistory_rebuilds_state_without_populating_uncommitted()
    {
        var id = Guid.NewGuid();
        var history = new IDomainEvent[]
        {
            new FakeRegisteredEvent(id, "first"),
            new FakeRegisteredEvent(id, "second"),
        };
        var sut = new FakeAggregate();

        sut.LoadFromHistory(history);

        sut.UncommittedEvents.ShouldBeEmpty();   // replay não gera eventos "novos"
        sut.AppliedData.ShouldBe("second");
        sut.AppliedCount.ShouldBe(2);
        sut.Id.ShouldBe(id);
        sut.Version.ShouldBe(2);                 // ordenação por Version, não por relógio
    }

    [Fact]
    public void ClearUncommittedEvents_empties_the_list()
    {
        var sut = new FakeAggregate();
        sut.DoSomething(Guid.NewGuid(), "x");

        sut.ClearUncommittedEvents();

        sut.UncommittedEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Emit_without_registered_handler_throws()
    {
        var sut = new FakeAggregate();

        Should.Throw<InvalidOperationException>(() => sut.EmitUnregistered(Guid.NewGuid()));
    }
}
