using MediatR;
using Microsoft.Extensions.Options;
using SolaceSystems.Solclient.Messaging;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using static System.Collections.Specialized.BitVector32;
using static taskmanager.api.SolaceListener;
namespace taskmanager.api
{
    

    public interface IIntegrationEventBus
    {
        Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken) where T : IIntegrationEvent;
    }
     
    public class SolaceIntegrationEventBus : IIntegrationEventBus
    {
        private readonly SolaceSystems.Solclient.Messaging.ISession _session;

        public SolaceIntegrationEventBus(SolaceSystems.Solclient.Messaging.ISession session)
        {
            _session = session;
        }

        public Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken) where T : IIntegrationEvent
        {  
            var message = ContextFactory.Instance.CreateMessage();

            message.BinaryAttachment = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(integrationEvent));
            message.Destination = ContextFactory.Instance.CreateTopic(integrationEvent.IntegrationEventMessageTypeName);

            _session.Send(message);

            return Task.CompletedTask;
        }
    }

    public record ListTaskCreatedIntegrationEvent(string ListTaskId) : IIntegrationEvent
    {
        public string ListTaskId { get; } = ListTaskId;
        public string TopicName { get => "TasksModule/ListTask/Created"; }
        public string IntegrationEventMessageTypeName { get => nameof(ListTaskCreatedIntegrationEvent); } 
    }














    
    public interface ISolaceBusConnection : IDisposable 
    {
        void ConnectForListening(EventHandler<MessageEventArgs>? messageEventHandler = default, EventHandler<FlowEventArgs>? flowEventHandler = default);
        ReturnCode Send(IMessage message);
        SolaceSystems.Solclient.Messaging.IFlow SolaceFlow { get; }
        void Disconnect();
    }
    // TODO: Implement automatic provisioning of queues and queue subscriptions.
    //
    // In Solace, publishers publish events to Topics.
    // Topics can fan out messages to multiple Queues through queue subscriptions.
    //
    // Each consuming module should typically own its own Queue, so that:
    // - Multiple modules can independently process the same event.
    // - Messages are isolated per module.
    // - A slow or failing module does not affect other consumers.
    //
    // A Topic represents an event type (for example: tasks/task-created).
    //
    // Each module queue should subscribe only to the Topics (integration events)
    // that the module is interested in processing.
    //
    // Example:
    //
    // Topic:
    //   tasks/task-created
    //
    // Queues:
    //   notifications-module-queue
    //   reporting-module-queue
    //
    // Queue subscriptions:
    //   notifications-module-queue -> tasks/task-created
    //   reporting-module-queue     -> tasks/task-created
    //
    // Result:
    //   Publishing a single tasks/task-created event causes Solace to place
    //   a copy of the message into both queues.
    public sealed class SolaceBusConnection : ISolaceBusConnection
    {
        private SolaceSystems.Solclient.Messaging.IContext? _solaceContext;
        private SolaceSystems.Solclient.Messaging.ISession? _solaceSession;
        private SolaceSystems.Solclient.Messaging.IQueue? _solaceQueue;  
        private readonly SessionProperties _solaceSessionProperties;
        private readonly ContextProperties _contextFactoryProperties;

        public SolaceSystems.Solclient.Messaging.IFlow? SolaceFlow { get; private set; }

        private void CreateConnection(EventHandler<MessageEventArgs>? messageEventHandler = default, EventHandler<FlowEventArgs>? flowEventHandler = default) 
        {
            _solaceContext = ContextFactory.Instance.CreateContext(_contextFactoryProperties, null);
            _solaceSession = _solaceContext.CreateSession(_solaceSessionProperties, null, null);

            var connectionStatus = _solaceSession.Connect();

            if (connectionStatus != ReturnCode.SOLCLIENT_OK)
            {
                throw new InvalidOperationException($"Solace connection failed with code: '{connectionStatus}'");
            }
             
            _solaceQueue = ContextFactory.Instance.CreateQueue("taskPOC_IntegrationEvents_Queue");

            // Set queue permissions to "consume" and access-type to "exclusive"
            EndpointProperties endpointProps = new EndpointProperties()
            {
                Permission = EndpointProperties.EndpointPermission.Consume,
                AccessType = EndpointProperties.EndpointAccessType.Exclusive
            };

            // Provision it, and do not fail if it already exists
            _solaceSession.Provision(_solaceQueue, endpointProps,
                ProvisionFlag.IgnoreErrorIfEndpointAlreadyExists | ProvisionFlag.WaitForConfirm, null);

            // Create and start flow to the newly provisioned queue
            // NOTICE HandleMessageEvent as the message event handler 
            // and HandleFlowEvent as the flow event handler
            SolaceFlow = _solaceSession.CreateFlow(new FlowProperties()
            {
                AckMode = MessageAckMode.ClientAck
            },
            _solaceQueue, null, messageEventHandler, flowEventHandler);

            var flowStatus = SolaceFlow.Start();

            if (flowStatus != ReturnCode.SOLCLIENT_OK)
            {
                throw new InvalidOperationException($"Solace flow failed with code: '{connectionStatus}'");
            }

        }
        
         
        public SolaceBusConnection(ContextProperties contextFactoryProperties, SessionProperties solaceSessionProperties)
        {  
            _solaceSessionProperties = solaceSessionProperties;
            _contextFactoryProperties = contextFactoryProperties;
        }

        public void ConnectForListening(EventHandler<MessageEventArgs>? messageEventHandler = default, EventHandler<FlowEventArgs>? flowEventHandler = default) 
        {
            if (_solaceContext is null || _solaceSession is null) 
            { 
                CreateConnection(messageEventHandler, flowEventHandler);
            } 
        }

        public ReturnCode Send(IMessage message) 
        {
            if (_solaceContext is null || _solaceSession is null)
            {
                CreateConnection();
            }
             
            var retunCode = _solaceSession!.Send(message);

            return retunCode;
        }

        public void Disconnect() 
        {
            _solaceSession?.Disconnect();
        }

        public void Dispose()
        {
            _solaceSession?.Disconnect();
            _solaceSession?.Dispose(); 
            _solaceContext?.Dispose();
        }
    }







     
    public class SolaceListener : IHostedService
    {
        private readonly ISolaceBusConnection _solaceConnection;
        private readonly Dictionary<string, Type> _integrationEventMessageTypes;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public SolaceListener(ISolaceBusConnection solaceConnection, IServiceScopeFactory serviceScopeFactory)
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

            var integrationEvent = JsonSerializer.Deserialize<GenericIntegrationEvent>(stringMessage);

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


    public interface IIntegrationEvent 
    {
        string TopicName { get; }
        string IntegrationEventMessageTypeName { get; }
    }

    public class GenericIntegrationEvent : IIntegrationEvent
    {
        public string TopicName { get; set; }

        public string IntegrationEventMessageTypeName { get; set; }
    }




    public class ListTaskCreatedIntegrationEventHandler
    {
        public async Task Handle(ListTaskCreatedIntegrationEvent listTaskCreatedIntegrationEvent)
        {
            // executes logic for different module in an application service or usecase when 
            // list task is created
        }
    }





}