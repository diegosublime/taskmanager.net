using SolaceSystems.Solclient.Messaging;

namespace taskmanager.api 
{

    // TODO: Implement automatic provisioning of queues and queue subscriptions.
    // Implement multiple flows for single session

    public interface ISolaceBusConnection : IDisposable
    {
        void ConnectForListening(EventHandler<MessageEventArgs>? messageEventHandler = default, EventHandler<FlowEventArgs>? flowEventHandler = default);
        ReturnCode Send(IMessage message);
        SolaceSystems.Solclient.Messaging.IFlow SolaceFlow { get; }
        void Disconnect();
    }
    
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
}
