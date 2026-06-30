using MediatR;

namespace taskmanager.api
{
    public abstract record DomainEvent: INotification
    {
        public DateTime TimeOfEventCreation { get; set; } = DateTime.UtcNow;

    }

    public sealed record ListTaskCreatedEvent(string listTaskId) : DomainEvent
    {
        public string ListTaskId { get; } = listTaskId;
    }
}
