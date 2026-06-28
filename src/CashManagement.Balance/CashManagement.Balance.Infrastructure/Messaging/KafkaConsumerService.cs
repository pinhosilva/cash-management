using CashManagement.Balance.Application;
using CashManagement.Balance.Application.Features.BalanceProjection;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CashManagement.Balance.Infrastructure.Messaging;

/// <summary>
/// Consumer Kafka do Balance como <see cref="BackgroundService"/>: faz o loop de
/// consume no tópico <c>cash.management.entries.events</c>
/// (<see cref="AutoOffsetReset.Earliest"/>), desserializa o envelope §4.3, roteia por
/// <c>event.type</c> (só <c>CreditPostedEvent</c> nesta fatia) e delega ao
/// <see cref="CreditPostedEventHandler"/>. O commit do offset é manual e só ocorre
/// <b>depois</b> da projeção — at-least-once; a idempotência (dedup por <c>event.id</c>)
/// no read model torna o reprocesso um no-op.
/// </summary>
public sealed class KafkaConsumerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaConsumerOptions _options;
    private readonly ILogger<KafkaConsumerService> _logger;

    public KafkaConsumerService(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaConsumerOptions> options,
        ILogger<KafkaConsumerService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        // Roda o loop bloqueante de consume numa thread dedicada para não segurar a
        // inicialização do host (StartAsync aguarda o ExecuteAsync até o 1º await).
        Task.Run(() => ConsumeLoopAsync(stoppingToken), stoppingToken);

    private async Task ConsumeLoopAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(_options.Topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result;
                try
                {
                    result = consumer.Consume(stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Falha ao consumir do tópico {Topic}.", _options.Topic);
                    continue;
                }

                if (result?.Message is null)
                {
                    continue;
                }

                await HandleMessageAsync(result.Message.Value, stoppingToken);

                // Commit só após a projeção (at-least-once; reprocesso é no-op por dedup).
                consumer.Commit(result);
            }
        }
        catch (OperationCanceledException)
        {
            // shutdown normal
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task HandleMessageAsync(string payload, CancellationToken cancellationToken)
    {
        var credit = CreditPostedEventDeserializer.TryParseCredit(payload);
        if (credit is null)
        {
            // Tipo fora de escopo desta fatia ou payload inválido — ignora (não loga PII).
            using (_logger.BeginScope(new Dictionary<string, object?> { ["component"] = LogComponent.KafkaConsumer }))
            {
                _logger.LogDebug("Mensagem ignorada (tipo fora de escopo ou payload inválido).");
            }

            return;
        }

        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["component"] = LogComponent.KafkaConsumer,
            ["correlationId"] = credit.CorrelationId,
        }))
        {
            _logger.LogInformation("CreditPostedEvent recebido (eventId={EventId}).", credit.EventId);
        }

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<CreditPostedEventHandler>();
        await handler.HandleAsync(credit, cancellationToken);
    }
}
