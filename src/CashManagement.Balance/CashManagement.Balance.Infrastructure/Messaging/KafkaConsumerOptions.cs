namespace CashManagement.Balance.Infrastructure.Messaging;

/// <summary>Configuração do consumer Kafka do Balance.</summary>
public sealed class KafkaConsumerOptions
{
    public const string SectionName = "Kafka";

    public const string DefaultTopic = "cash.management.entries.events";

    public const string DefaultGroupId = "cash-management-balance";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = DefaultTopic;

    public string GroupId { get; set; } = DefaultGroupId;
}
