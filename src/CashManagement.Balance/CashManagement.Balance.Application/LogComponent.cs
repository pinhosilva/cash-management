namespace CashManagement.Balance.Application;

/// <summary>
/// Componente de origem do log como <b>campo</b> estruturado (enum fechado, §8.2)
/// — não um prefixo na mensagem. Evita variações de capitalização poluindo o índice
/// de logs. Espelha o padrão do Entries.
/// </summary>
public enum LogComponent
{
    /// <summary>Consumer Kafka (loop de consumo, roteamento por tipo de evento).</summary>
    KafkaConsumer,

    /// <summary>Projeção do read model (aplicação do evento ao saldo).</summary>
    Projection,
}
