namespace CashManagement.Entries.Domain.SeedWork;

/// <summary>
/// Base de um Aggregate Root event-sourced. O estado é derivado da sequência
/// de eventos: o comportamento valida invariantes e <see cref="Emit"/> um
/// evento; só os eventos mutam o estado, via handlers registrados em
/// <see cref="RegisterEvents"/> com <see cref="On{TEvent}"/>.
/// </summary>
/// <remarks>
/// Roteamento por composição (dicionário privado), sem reflection no caminho
/// de execução. A ordenação é dada por <see cref="Version"/> (incrementada a
/// cada evento aplicado), nunca por relógio.
/// </remarks>
public abstract class AggregateRoot
{
    private readonly Dictionary<Type, Action<IDomainEvent>> _handlers = new();
    private readonly List<IDomainEvent> _uncommittedEvents = new();
    private bool _handlersRegistered;

    /// <summary>Identidade do stream (gerada na aplicação, nunca pelo banco).</summary>
    public Guid Id { get; private set; }

    /// <summary>Número de eventos já aplicados — base da concorrência otimista.</summary>
    public int Version { get; private set; }

    /// <summary>Eventos emitidos e ainda não persistidos.</summary>
    public IReadOnlyCollection<IDomainEvent> UncommittedEvents => _uncommittedEvents;

    /// <summary>
    /// Reconstrói o estado a partir do histórico (replay), sem marcar os
    /// eventos como não-commitados.
    /// </summary>
    public void LoadFromHistory(IEnumerable<IDomainEvent> history)
    {
        foreach (var @event in history)
        {
            Apply(@event);
        }
    }

    /// <summary>Limpa os eventos não-commitados após a persistência.</summary>
    public void ClearUncommittedEvents() => _uncommittedEvents.Clear();

    /// <summary>
    /// Emite um novo evento: aplica a mutação e registra como não-commitado.
    /// <c>protected</c> — só o próprio agregado emite seus eventos, para que a
    /// invariante não possa ser furada de fora.
    /// </summary>
    protected void Emit(IDomainEvent @event)
    {
        Apply(@event);
        _uncommittedEvents.Add(@event);
    }

    /// <summary>
    /// Registra os handlers de evento do agregado. Chamado uma única vez, sob
    /// demanda. A subclasse só registra os <see cref="On{TEvent}"/>.
    /// </summary>
    protected abstract void RegisterEvents();

    /// <summary>Associa um tipo de evento ao handler que muta o estado.</summary>
    protected void On<TEvent>(Action<TEvent> handler) where TEvent : IDomainEvent =>
        _handlers[typeof(TEvent)] = @event => handler((TEvent)@event);

    private void Apply(IDomainEvent @event)
    {
        EnsureHandlersRegistered();

        if (!_handlers.TryGetValue(@event.GetType(), out var handler))
        {
            throw new InvalidOperationException(
                $"No event handler registered for '{@event.GetType().Name}'. " +
                "Register it in RegisterEvents() with On<TEvent>(...).");
        }

        handler(@event);
        Id = @event.AggregateId;
        Version++;
    }

    private void EnsureHandlersRegistered()
    {
        if (_handlersRegistered)
        {
            return;
        }

        RegisterEvents();
        _handlersRegistered = true;
    }
}
