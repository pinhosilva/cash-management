namespace CashManagement.Entries.Api.Logging;

/// <summary>
/// Componente de origem do log como <b>campo</b> estruturado (enum fechado, §8.2)
/// — não um prefixo na mensagem. Evita variações de capitalização poluindo o
/// índice de logs. Cada fatia que passa a logar adiciona aqui o seu componente.
/// </summary>
public enum LogComponent
{
    Controller,
}
