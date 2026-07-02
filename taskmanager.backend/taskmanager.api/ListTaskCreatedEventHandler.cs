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

    //public class ListTaskCreatedPublishIntegrationEventHandler : INotificationHandler<ListTaskCreatedEvent>
    //{
    //    private IIntegrationEventBus IntegrationEventBus { get; set; }

    //    public ListTaskCreatedPublishIntegrationEventHandler(IIntegrationEventBus integrationEventBus)
    //    {
    //        IntegrationEventBus = integrationEventBus;
    //    }
         
    //    //Trigger integration event to communicate with other modules
    //    public async Task Handle(ListTaskCreatedEvent notification, CancellationToken cancellationToken)
    //    {
    //        //TODO: Outbox pattern pending
    //        await IntegrationEventBus.PublishAsync<ListTaskCreatedIntegrationEvent>(new ListTaskCreatedIntegrationEvent(notification.listTaskId), cancellationToken);
            
    //    }
    //}   
}
