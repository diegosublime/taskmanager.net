namespace taskmanager.api 
{
    public class GenericListTaskCreatedIntegrationEvent : IIntegrationEvent
    {
        public string TopicName { get; set; }

        public string IntegrationEventMessageTypeName { get; set; }
    }

    public record ListTaskCreatedIntegrationEvent(string ListTaskId) : IIntegrationEvent
    {
        public string ListTaskId { get; } = ListTaskId;
        public string TopicName { get => "TasksModule/ListTask/Created"; }
        public string IntegrationEventMessageTypeName { get => nameof(ListTaskCreatedIntegrationEvent); }
    }
}
