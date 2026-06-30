using MediatR;

namespace taskmanager.api
{
    public class ListTaskCreatedSendEmailEventHandler : INotificationHandler<ListTaskCreatedEvent>
    {
        //SendEmail when taskList is created
        public Task Handle(ListTaskCreatedEvent notification, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    public class ListTaskCreatedLogEventHandler : INotificationHandler<ListTaskCreatedEvent>
    {
        //SendEmail when taskList is created
        public Task Handle(ListTaskCreatedEvent notification, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
