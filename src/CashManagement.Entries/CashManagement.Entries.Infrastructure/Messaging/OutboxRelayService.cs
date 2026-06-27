using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CashManagement.Entries.Infrastructure.Messaging;

/// <summary>
/// Relay da outbox como <see cref="BackgroundService"/>: faz polling periódico e
/// delega ao <see cref="OutboxProcessor"/> (resolvido num escopo próprio, pois o
/// DbContext é scoped). Garante "gravou ⇒ publicado" sem depender do fluxo da request.
/// </summary>
public sealed class OutboxRelayService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    private readonly IServiceScopeFactory _scopeFactory;

    public OutboxRelayService(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<OutboxProcessor>();
                await processor.ProcessPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                // ciclo seguinte tenta de novo; observabilidade (log) entra na T08.
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }
}
