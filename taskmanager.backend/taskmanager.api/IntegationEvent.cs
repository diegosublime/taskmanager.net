using Microsoft.Extensions.Options;
using SolaceSystems.Solclient.Messaging;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using static taskmanager.api.SolaceListener;
namespace taskmanager.api
{
    public interface IIntegationEvent
    {
        string TopicName { get; } 
    }

    public interface IIntegrationEventBus
    {
        Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken) where T : IIntegationEvent;
    }
     
    public class SolaceIntegrationEventBus : IIntegrationEventBus
    {
        private readonly SolaceSystems.Solclient.Messaging.ISession _session;

        public SolaceIntegrationEventBus(SolaceSystems.Solclient.Messaging.ISession session)
        {
            _session = session;
        }

        public Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken) where T : IIntegationEvent
        {  
            var message = ContextFactory.Instance.CreateMessage();

            message.BinaryAttachment = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(integrationEvent));
            message.Destination = ContextFactory.Instance.CreateTopic(integrationEvent.TopicName);

            _session.Send(message);

            return Task.CompletedTask;
        }
    }

    public record ListTaskCreatedIntegrationEvent(string ListTaskId) : IIntegationEvent
    {
        public string ListTaskId { get; } = ListTaskId;
        public string TopicName { get => "TasksModule.ListTask.Created"; }
    }














    
    public interface ISolaceBusConnection : IDisposable 
    {
        void ConnectForListening(EventHandler<MessageEventArgs> messageEventHandler, EventHandler<SessionEventArgs>? sessionEventHandler = default);
        ReturnCode Send(IMessage message);
        void Disconnect();
    }

    public sealed class SolaceBusConnection : ISolaceBusConnection
    {
        private SolaceSystems.Solclient.Messaging.IContext? _solaceContext;
        private SolaceSystems.Solclient.Messaging.ISession? _solaceSession;
        private readonly SessionProperties _solaceSessionProperties;
        private readonly ContextProperties _contextFactoryProperties;

        private void CreateConnection(EventHandler<MessageEventArgs>? messageEventHandler = default, EventHandler<SessionEventArgs>? sessionEventHandler = default) 
        {
            _solaceContext = ContextFactory.Instance.CreateContext(_contextFactoryProperties, null);
            _solaceSession = _solaceContext.CreateSession(_solaceSessionProperties, messageEventHandler, sessionEventHandler);

            var connectionStatus = _solaceSession.Connect();

            if (connectionStatus != ReturnCode.SOLCLIENT_OK)
            {
                throw new InvalidOperationException($"Solace connection failed with code: '{connectionStatus}'");
            }
        }
        
         
        public SolaceBusConnection(ContextProperties contextFactoryProperties, SessionProperties solaceSessionProperties)
        {  
            _solaceSessionProperties = solaceSessionProperties;
            _contextFactoryProperties = contextFactoryProperties;
        }

        public void ConnectForListening(EventHandler<MessageEventArgs> messageEventHandler, EventHandler<SessionEventArgs>? sessionEventHandler = default) 
        {
            if (_solaceContext is null || _solaceSession is null) 
            {
                CreateConnection(messageEventHandler, sessionEventHandler);
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

        public SolaceListener(ISolaceBusConnection solaceConnection)
        {
            _solaceConnection = solaceConnection;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _solaceConnection.ConnectForListening(OnMessageReceived);
            return Task.CompletedTask;
        }
         

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _solaceConnection.Disconnect();
            return Task.CompletedTask;
        }


        private void OnMessageReceived(object? source, MessageEventArgs args) 
        {
        }

        
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