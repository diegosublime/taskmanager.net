namespace taskmanager.api
{
    public interface IIntegationEvent
    {  
    }

    public interface IIntegrationEventBus
    {
        Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken) where T : IIntegationEvent;
    }

    public record ListTaskCreatedIntegrationEvent(string ListTaskId) : IIntegationEvent
    {
        public string ListTaskId { get; } = ListTaskId;
    }
}
