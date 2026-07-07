using SolaceSystems.Solclient.Messaging;
using System.Text;
using System.Text.Json;

namespace taskmanager.api
{
     

    public class SolaceMessageConsumer : IHostedService
    {
        private readonly ISolaceBusConnection _solaceConnection;
        private readonly Dictionary<string, Type> _integrationEventMessageTypes;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public SolaceMessageConsumer(ISolaceBusConnection solaceConnection, IServiceScopeFactory serviceScopeFactory)
        {
            _solaceConnection = solaceConnection;
            _serviceScopeFactory = serviceScopeFactory;
            _integrationEventMessageTypes = typeof(Program).Assembly.DefinedTypes.
               Where(type => typeof(IIntegrationEvent).IsAssignableFrom(type)).ToDictionary(type => type.Name, type => type.AsType());
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _solaceConnection.ConnectForListening(OnMessageReceived, OnFlowMessageReceived);
            return Task.CompletedTask;
        }


        public Task StopAsync(CancellationToken cancellationToken)
        {
            _solaceConnection.Disconnect();
            return Task.CompletedTask;
        }


        private void OnMessageReceived(object? source, MessageEventArgs args)
        {
            using IMessage message = args.Message;

            var stringMessage = Encoding.ASCII.GetString(message.BinaryAttachment);

            var integrationEvent = JsonSerializer.Deserialize<GenericListTaskCreatedIntegrationEvent>(stringMessage);

            if (integrationEvent is null)
            {
                return;
            }

            var integrationEventMessageType = _integrationEventMessageTypes.GetValueOrDefault(integrationEvent.IntegrationEventMessageTypeName);

            if (integrationEventMessageType is null)
            {
                return;
            }

            var integrationEventMessage = JsonSerializer.Deserialize(stringMessage, integrationEventMessageType);

            if (integrationEventMessage is null)
            {
                return;
            }

            //Mediator send might be async, so a channel implementation here would be a good idea
            //using var scope = _serviceScopeFactory.CreateScope();
            //var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            //mediator.Send(integrationEventMessage);

            _solaceConnection.SolaceFlow.Ack(message.ADMessageId);
        }

        private void OnFlowMessageReceived(object? source, FlowEventArgs args)
        {
            Console.WriteLine(args.Info);
        }


    }
}
