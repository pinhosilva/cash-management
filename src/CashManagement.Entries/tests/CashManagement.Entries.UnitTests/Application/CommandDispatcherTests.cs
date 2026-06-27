using CashManagement.Entries.Application.Abstractions;
using CashManagement.Entries.Domain.SeedWork;
using Moq;
using Shouldly;
using Xunit;

namespace CashManagement.Entries.UnitTests.Application;

public class CommandDispatcherTests
{
    private sealed record FakeCommand(int Value) : ICommand<int>;

    private sealed class FakeHandler : ICommandHandler<FakeCommand, int>
    {
        public Task<Result<int>> HandleAsync(FakeCommand command, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Ok(command.Value * 2));
    }

    [Fact]
    public async Task Send_resolves_the_handler_and_returns_its_result()
    {
        var provider = new Mock<IServiceProvider>();
        provider
            .Setup(p => p.GetService(typeof(ICommandHandler<FakeCommand, int>)))
            .Returns(new FakeHandler());
        var dispatcher = new CommandDispatcher(provider.Object);

        Result<int> result = await dispatcher.Send<FakeCommand, int>(new FakeCommand(21));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public async Task Send_throws_when_no_handler_is_registered()
    {
        var provider = new Mock<IServiceProvider>();
        provider.Setup(p => p.GetService(It.IsAny<Type>())).Returns(null!);
        var dispatcher = new CommandDispatcher(provider.Object);

        await Should.ThrowAsync<InvalidOperationException>(
            async () => await dispatcher.Send<FakeCommand, int>(new FakeCommand(1)));
    }
}
