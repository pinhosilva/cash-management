using CashManagement.Entries.Application.Abstractions;
using CashManagement.Entries.Domain.Persistence;
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
        public Task<Result<int>> HandleAsync(FakeCommand command) =>
            Task.FromResult(Result.Ok(command.Value * 2));
    }

    private sealed class FailingHandler : ICommandHandler<FakeCommand, int>
    {
        public Task<Result<int>> HandleAsync(FakeCommand command) =>
            Task.FromResult(Result.Fail<int>(new Error("BOOM", "Failed.", ErrorType.Validation)));
    }

    private static Mock<IServiceProvider> ProviderFor(ICommandHandler<FakeCommand, int> handler)
    {
        var provider = new Mock<IServiceProvider>();
        provider
            .Setup(p => p.GetService(typeof(ICommandHandler<FakeCommand, int>)))
            .Returns(handler);
        return provider;
    }

    [Fact]
    public async Task Send_resolves_the_handler_returns_its_result_and_commits()
    {
        var provider = ProviderFor(new FakeHandler());
        var unitOfWork = new Mock<IUnitOfWork>();
        var dispatcher = new CommandDispatcher(provider.Object, unitOfWork.Object);

        Result<int> result = await dispatcher.Send<FakeCommand, int>(new FakeCommand(21));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
        unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task Send_does_not_commit_when_the_handler_fails()
    {
        var provider = ProviderFor(new FailingHandler());
        var unitOfWork = new Mock<IUnitOfWork>();
        var dispatcher = new CommandDispatcher(provider.Object, unitOfWork.Object);

        Result<int> result = await dispatcher.Send<FakeCommand, int>(new FakeCommand(1));

        result.IsFailure.ShouldBeTrue();
        unitOfWork.Verify(u => u.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task Send_throws_when_no_handler_is_registered()
    {
        var provider = new Mock<IServiceProvider>();
        provider.Setup(p => p.GetService(It.IsAny<Type>())).Returns(null!);
        var dispatcher = new CommandDispatcher(provider.Object, Mock.Of<IUnitOfWork>());

        await Should.ThrowAsync<InvalidOperationException>(
            async () => await dispatcher.Send<FakeCommand, int>(new FakeCommand(1)));
    }
}
