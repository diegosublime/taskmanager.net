using SolaceSystems.Solclient.Messaging;
using System.Text;
using System.Text.Json;

namespace taskmanager.api 
{
    public class SolaceMessageProducer
    {
    }

    public interface IIntegrationEventProducer
    {
        Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken) where T : IIntegrationEvent;
    }

    public class SolaceIntegrationEventBus : IIntegrationEventProducer
    {
        private readonly ISolaceBusConnection _solaceBusConnection;

        public SolaceIntegrationEventBus(ISolaceBusConnection solaceBusConnection)
        {
            _solaceBusConnection = solaceBusConnection;
        }

        public Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken) where T : IIntegrationEvent
        {
            using var message = ContextFactory.Instance.CreateMessage();
            message.DeliveryMode = MessageDeliveryMode.Persistent;
            message.Destination = ContextFactory.Instance.CreateTopic(integrationEvent.TopicName);
            message.BinaryAttachment = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(integrationEvent));


            var returnCode = _solaceBusConnection.Send(message);

            return Task.CompletedTask;
        }
    }
}
