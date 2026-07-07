namespace taskmanager.api 
{
    public interface IIntegrationEvent
    {
        string TopicName { get; }
        string IntegrationEventMessageTypeName { get; } 
    }
}
