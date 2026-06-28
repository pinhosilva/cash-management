using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CashManagement.Entries.Infrastructure.Messaging;

/// <summary>
/// Relay da outbox como <see cref="BackgroundService"/>: faz polling periódico e
/// delega ao <see cref="OutboxProcessor"/> (resolvido num escopo próprio, pois o
/// DbContext é scoped). Garante "gravou ⇒ publicado" sem depender do fluxo da request.
/// O intervalo de polling vem de <see cref="OutboxOptions"/> (config).
/// </summary>
public sealed class OutboxRelayService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _pollInterval;

    public OutboxRelayService(IServiceScopeFactory scopeFactory, IOptions<OutboxOptions> options)
    {
        _scopeFactory = scopeFactory;
        _pollInterval = TimeSpan.FromSeconds(options.Value.PollIntervalSeconds);
    }

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
                // ciclo seguinte tenta de novo (at-least-once); o log do relay entra
                // na fatia futura de observabilidade (§8.2), com ILogger + component.
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }
    }
}
