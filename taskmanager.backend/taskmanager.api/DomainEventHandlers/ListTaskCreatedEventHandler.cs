using MediatR;

namespace taskmanager.api
{
    //public class ListTaskCreatedSendEmailEventHandler : INotificationHandler<ListTaskCreatedEvent>
    //{
    //    //SendEmail when taskList is created
    //    public Task Handle(ListTaskCreatedEvent notification, CancellationToken cancellationToken)
    //    {
    //        return Task.CompletedTask;
    //    }
    //}

    public class ListTaskCreatedPublishIntegrationEventHandler : INotificationHandler<ListTaskCreatedEvent>
    {
        private readonly IIntegrationEventProducer _integrationEventProducer;

        public ListTaskCreatedPublishIntegrationEventHandler(IIntegrationEventProducer integrationEventProducer)
        {
            _integrationEventProducer = integrationEventProducer;
        }

        //Trigger integration event to communicate with other modules
        public async Task Handle(ListTaskCreatedEvent notification, CancellationToken cancellationToken)
        {
            //TODO: Outbox pattern pending
            await _integrationEventProducer.PublishAsync(new ListTaskCreatedIntegrationEvent(notification.listTaskId), cancellationToken);

        }
    }
}
