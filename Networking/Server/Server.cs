namespace LCPDFR.Networking.Server
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Sockets;
    using System.Threading;

    using LCPDFR.Networking.User;

    using Lidgren.Network;

    /// <summary>
    /// The delegate for messages received by clients.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="message">The message.</param>
    public delegate void ClientUserMessageHandlerFunction(NetworkClient sender, ReceivedUserMessage message);

    /// <summary>
    /// The delegate for when a connection to a client is established or lost.
    /// </summary>
    /// <param name="client">The client. Can be null.</param>
    public delegate void ClientConnectionEventHandler(NetworkClient client);

    /// <summary>
    /// The delegate for when the server started or stopped.
    /// </summary>
    public delegate void ServerEventHandler();

    /// <summary>
    /// The server the host runs.
    /// Does not process messages automatically, but only when <see cref="Server.ProcessQueue"/> is called (to allow synchronous processing).
    /// </summary>
    public class Server : BaseNetPeer, ILoggable
    {
        /// <summary>
        /// The network identifier.
        /// </summary>
        private const string NetworkIdentifier = "LCPDFR Networking";

        /// <summary>
        /// Whether or not the server thread should be aborted.
        /// </summary>
        private bool abort;

        /// <summary>
        /// The name of the server.
        /// </summary>
        private string name;

        /// <summary>
        /// The port to listen on.
        /// </summary>
        private int port;

        /// <summary>
        /// The client manager.
        /// </summary>
        private ClientManager clientManager;

        /// <summary>
        /// Whether or not UPnP is used.
        /// </summary>
        private bool upnpInUse;

        /// <summary>
        /// The internal lidgren server instance.
        /// </summary>
        private NetServer server;

        /// <summary>
        /// The thread running the server loop.
        /// </summary>
        private Thread thread;

        /// <summary>
        /// The UPnP helper class.
        /// </summary>
        private UPnP upnp;

        /// <summary>
        /// Initializes a new instance of the <see cref="Server"/> class.
        /// </summary>
        public Server() : this(null, 0, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Server"/> class.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="port">
        /// The port.
        /// </param>
        public Server(string name, int port) : this(name, port, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Server"/> class.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="port">
        /// The port.
        /// </param>
        /// <param name="localIpAddress">
        /// The local IP address. Use null when not specified.
        /// </param>
        /// <param name="defaultInternetGateway">
        /// The default Internet Gateway IP address. Use null when not specified.
        /// </param>
        public Server(string name, int port, string localIpAddress, string defaultInternetGateway)
            : base(NetworkIdentifier)
        {
            this.name = name;
            this.port = port;

            this.clientManager = new ClientManager();
            this.upnp = new UPnP(localIpAddress, defaultInternetGateway);

            // Set up default handlers.
            this.MessageHandler.AddHandler(EMessageID.Welcome, this.WelcomeHandlerFunction);
            this.MessageHandler.AddHandler(EMessageID.Version, this.VersionHandlerFunction);
        }

        /// <summary>
        /// The event that is fired when either the name and the version of a client has been received.
        /// </summary>
        public event ClientConnectionEventHandler ClientDetailsReceived;

        /// <summary>
        /// The event that is fired when a connection to a client has been established.
        /// </summary>
        public event ClientConnectionEventHandler ConnectionEstablished;

        /// <summary>
        /// The event that is fired when a connection to a client has been lost.
        /// </summary>
        public event ClientConnectionEventHandler ConnectionLost;

        /// <summary>
        /// The event that is fired when the server is running.
        /// </summary>
        public event ServerEventHandler ServerRunning;

        /// <summary>
        /// The event that is fired when the server has stopped.
        /// </summary>
        public event ServerEventHandler ServerStopped;

        /// <summary>
        /// Gets the component name.
        /// </summary>
        public new string ComponentName
        {
            get { return "Server"; }
        }

        /// <summary>
        /// Gets all connected clients. This may include clients that are about to time-out.
        /// </summary>
        public NetworkClient[] ConnectedClients
        {
            get
            {
                return (from connection in this.server.Connections
                    where this.clientManager.HasClient(connection.RemoteUniqueIdentifier)
                    select this.clientManager.GetClient(connection.RemoteUniqueIdentifier)).ToArray();
            }
        }

        /// <summary>
        /// Gets the public IP address of the server. Only works when UPnP firewall has been found.
        /// </summary>
        public string PublicIPAddress
        {
            get
            {
                try
                {
                    return this.upnp.GetExternalIP().ToString();
                }
                catch (Exception)
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the server is running.
        /// </summary>
        public bool Running { get; private set; }

        /// <summary>
        /// Closes the server and all its connections and sends <paramref name="goodbyeMessage"/> to all clients.
        /// Waits one second before returning to allow proper termination.
        /// </summary>
        /// <param name="goodbyeMessage">The message that is sent to all connected clients.</param>
        public override void Close(string goodbyeMessage)
        {
            this.server.Shutdown(goodbyeMessage);
            this.StopFirewallTraversal();
            this.abort = true;
            Thread.Sleep(1000);
        }

        /// <summary>
        /// Starts the server.
        /// </summary>
        public void Start()
        {
            if (this.name == null)
            {
                throw new InvalidOperationException("No server details specified.");
            }

            string localIP = this.upnp.LocalIpAddress != null ? this.upnp.LocalIpAddress.ToString() : null;
            string localGateway = this.upnp.LocalGateway != null ? this.upnp.LocalGateway.ToString() : null;

            this.Start(this.name, this.port, localIP, localGateway);
        }

        /// <summary>
        /// Starts the server. This method is asynchronous.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="port">
        /// The port.
        /// </param>
        /// <param name="localIpAddress">
        /// The local IP Address.
        /// </param>
        /// <param name="defaultInternetGateway">
        /// The default Internet Gateway.
        /// </param>
        public void Start(string name, int port, string localIpAddress, string defaultInternetGateway)
        {
            if (this.Running)
            {
                throw new InvalidOperationException("Server is already running.");
            }

            this.name = name;
            this.port = port;
            this.upnp = new UPnP(localIpAddress, defaultInternetGateway);

            this.thread = new Thread(this.Listen) { IsBackground = true };
            this.thread.Start();
        }

        /// <summary>
        /// Adds a user data handler with the specified details for messages received from clients.
        /// </summary>
        /// <param name="identifier">
        /// The message identifier. This can be used to easily separate communication and allow different enums (with the same values).
        /// For instance, an identifier could be "Vehicle" and another could be "Ped" with their respective enums.
        /// Limited to 255 chars.
        /// </param>
        /// <param name="messageID">The message ID.</param>
        /// <param name="handlerFunction">The associated handler function.</param>
        public void AddUserDataHandler(string identifier, Enum messageID, ClientUserMessageHandlerFunction handlerFunction)
        {
            this.AddUserDataHandler(identifier, (int)(object)messageID, handlerFunction);
        }

        /// <summary>
        /// Adds a user data handler with the specified details for messages received from clients.
        /// </summary>
        /// <param name="identifier">
        /// The message identifier. This can be used to easily separate communication and allow different enums (with the same values).
        /// For instance, an identifier could be "Vehicle" and another could be "Ped" with their respective enums.
        /// Limited to 255 chars.
        /// </param>
        /// <param name="messageID">The message ID.</param>
        /// <param name="handlerFunction">The associated handler function.</param>
        public void AddUserDataHandler(string identifier, int messageID, ClientUserMessageHandlerFunction handlerFunction)
        {
            base.AddUserDataHandler(identifier, messageID, handlerFunction);
        }

        /// <summary>
        /// Processes the incoming message queue. When not called, no messages are processed and no callbacks are fired. 
        /// Processing is done on the calling thread to ensure synchronous callbacks.
        /// </summary>
        public virtual void ProcessQueue()
        {
            this.MessageHandler.ProcessQueue();
        }

        /// <summary>
        /// Sends a user message to all connected clients.
        /// </summary>
        /// <param name="userMessage">The user message.</param>
        public override void Send(UserMessage userMessage)
        {
            // Construct message.
            NetOutgoingMessage outgoingMessage = this.server.CreateMessage();
            outgoingMessage.Write((byte)EMessageID.UserPayload);
            outgoingMessage.Write(userMessage.GetBytes());

            // Send to all and flush.
            this.SendToAll(outgoingMessage, NetDeliveryMethod.ReliableOrdered);
            this.server.FlushSendQueue();
        }

        /// <summary>
        /// Sends a user message to a certain recipient only.
        /// </summary>
        /// <param name="userMessage">The message.</param>
        /// <param name="recipient">The recipient.</param>
        public void Send(UserMessage userMessage, NetworkClient recipient)
        {
            // Construct message.
            NetOutgoingMessage outgoingMessage = this.server.CreateMessage();
            outgoingMessage.Write((byte)EMessageID.UserPayload);
            outgoingMessage.Write(userMessage.GetBytes());

            // Send to all and flush.
            this.Send(outgoingMessage, recipient.NetConnection, NetDeliveryMethod.ReliableOrdered);
            this.server.FlushSendQueue();
        }

        /// <summary>
        /// Sends a user message to all connected clients, but the client specified in <paramref name="except"/>.
        /// </summary>
        /// <param name="userMessage">The user message.</param>
        /// <param name="except">The client that will not receive the message.</param>
        public void SendToExcept(UserMessage userMessage, NetworkClient except)
        {
            this.SendToExcept(userMessage, new[] { except });
        }

        /// <summary>
        /// Sends a user message to all connected clients, but those specified in <paramref name="except"/>.
        /// </summary>
        /// <param name="userMessage">The user message.</param>
        /// <param name="except">The clients that will not receive the message.</param>
        public void SendToExcept(UserMessage userMessage, NetworkClient[] except)
        {
            // Construct message.
            NetOutgoingMessage outgoingMessage = this.server.CreateMessage();
            outgoingMessage.Write((byte)EMessageID.UserPayload);
            outgoingMessage.Write(userMessage.GetBytes());

            // Send to all and flush.
            List<NetConnection> exceptions = except.Select(client => client.NetConnection).ToList();
            this.SendToAll(outgoingMessage, NetDeliveryMethod.ReliableOrdered, exceptions.ToArray());
            this.server.FlushSendQueue();
        }

        /// <summary>
        /// Gets the internal <see cref="NetPeer"/> instance.
        /// </summary>
        /// <returns>The instance.</returns>
        internal override NetPeer GetInternalNetPeer()
        {
            return this.server;
        }

        /// <summary>
        /// Invokes the handler function registered for the user payload and uses more user friendly arguments. 
        /// Function is already resolved in <see cref="BaseNetPeer"/>.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <param name="message">The message.</param>
        /// <param name="sender">The sender.</param>
        internal override void InvokeUserPayloadHandlerFunction(object function, NetIncomingMessage message, NetConnection sender)
        {
            // Get client that sent the message.
            NetworkClient networkClient = this.clientManager.GetClient(sender.RemoteUniqueIdentifier);

            // Invoke function.
            ClientUserMessageHandlerFunction clientFunc = (ClientUserMessageHandlerFunction)function;
            clientFunc.Invoke(networkClient, new ReceivedUserMessage(message, clientFunc, networkClient));
        }

        internal void Send(NetOutgoingMessage outgoingMessage, NetConnection remoteHost, NetDeliveryMethod deliveryMethod)
        {
            //Logging.Debug("Send: Sending message...", this);
            outgoingMessage = this.EncryptMessage(outgoingMessage);
            NetSendResult result = this.server.SendMessage(outgoingMessage, remoteHost, deliveryMethod);
            this.server.FlushSendQueue();
            Logging.Debug("Send: Result: " + result.ToString(), this);
        }

        internal void SendToAll(NetOutgoingMessage outgoingMessage, NetDeliveryMethod deliveryMethod)
        {
            Logging.Debug("SendToAll: Sending message...", this);
            outgoingMessage = this.EncryptMessage(outgoingMessage);
            this.server.SendToAll(outgoingMessage, deliveryMethod);
        }

        internal void SendToAll(NetOutgoingMessage outgoingMessage, NetDeliveryMethod deliveryMethod, NetConnection except)
        {
            this.SendToAll(outgoingMessage, deliveryMethod, new[] { except });
        }

        internal void SendToAll(NetOutgoingMessage outgoingMessage, NetDeliveryMethod deliveryMethod, NetConnection[] except)
        {
            Logging.Debug("SendToAllExcept: Sending message...", this);
            List<NetConnection> all = this.server.Connections;
            List<NetConnection> finalList = new List<NetConnection>(all);

            foreach (NetConnection netConnection in all)
            {
                foreach (NetConnection connection in except)
                {
                    if (connection == netConnection)
                    {
                        finalList.Remove(connection);
                    }
                }
            }

            if (finalList.Count <= 0)
            {
                return;
            }

            outgoingMessage = this.EncryptMessage(outgoingMessage);
            this.server.SendMessage(outgoingMessage, finalList, deliveryMethod, 0);
        }

        private void FirewallTraversal()
        {
            try
            {
                Logging.Debug("FirewallTraversal: Searching for UPnP firewall...", this);
                if (this.upnp.LocalGateway != null)
                {
                    if (!this.upnp.SetServiceUrlUsingLocalInternetGateway())
                    {
                        Logging.Warning(string.Format("Firewall at {0} didn't respond", this.upnp.LocalGateway), this);   
                    }
                }
                else
                {
                    this.upnp.Discover();
                }

                if (this.upnp.FoundFirewall)
                {
                    Logging.Info(string.Format("Default gateway: {0}. Override via settings if not correct", this.upnp.LocalGateway), this);
                    Logging.Debug("FirewallTraversal: UPnP service: " + this.upnp.ServiceUrl, this);

                    // If no local IP is set yet, try to resolve.
                    if (this.upnp.LocalIpAddress == null)
                    {
                        // Try to resolve the local IP. This is tricky because things like Hamachi might be listed first, so we use our local gateway 
                        // we just discovered to ensure we got the right one.
                        if (string.IsNullOrEmpty(this.upnp.GetLocalIPAddressForDiscoveredGateway()))
                        {
                            // We failed, so use the "bad" attempt by simply using the first valid local address.
                            this.upnp.GetLocalIPAddress();
                            Logging.Warning("Failed to properly resolve local IP, falling back to first valid address", this);
                        }
                    }

                    Logging.Info(string.Format("Local IP: {0}. Override via settings if not correct", this.upnp.LocalIpAddress), this);
                    Logging.Debug("FirewallTraversal: UPnP-capable firewall detected, adding firewall rule...", this);

                    this.upnp.ForwardPort(this.port, ProtocolType.Udp, "LCPDFR Server (UDP)");
                    Logging.Debug(string.Format("FirewallTraversal: Port {0} forwarded", this.port), this);
                    Logging.Info(string.Format("Public IP: {0}", this.upnp.GetExternalIP()), this);

                    this.upnpInUse = true;
                }
                else
                {
                    Logging.Debug("FirewallTraversal: No UPnP firewall on the network.", this);
                }
            }
            catch (Exception ex)
            {
                Logging.Error("FirewallTraversal: " + ex.ToString(), this);
            }
        }

        private void StopFirewallTraversal()
        {
            try
            {
                if (this.upnpInUse)
                {
                    this.upnp.DeleteForwardingRule(this.port, ProtocolType.Udp);
                    Logging.Debug("StopFirewallTraversal: Removed firewall rule from UPnP firewall.", this);
                }
            }
            catch (Exception ex)
            {
                Logging.Error("StopFirewallTraversall: " + ex.ToString(), this);
            }
        }

        /// <summary>
        /// The core server logic, creates the actual server and listen on the port specified.
        /// </summary>
        private void Listen()
        {
            try
            {
                var configuration = new NetPeerConfiguration(this.AppIdentifier);
                configuration.MaximumConnections = 32;
                configuration.Port = this.port;
                configuration.AutoFlushSendQueue = true;

                this.server = new NetServer(configuration);
                this.server.Start();
                this.Running = true;

                Logging.Debug("Listen: Server running on port: " + this.port, this);
                Logging.Debug("Listen: Attempting firewall traversal", this);
                this.FirewallTraversal();

                // Fire event.
                ServerEventHandler handler = this.ServerRunning;
                if (handler != null)
                {
                    handler();
                }

                while (true)
                {
                    try
                    {
                        if (this.abort)
                        {
                            break;
                        }

                        bool allowRecycle = true;
                        NetIncomingMessage msg;
                        while ((msg = this.server.ReadMessage()) != null)
                        {
                            switch (msg.MessageType)
                            {
                                case NetIncomingMessageType.DebugMessage:
                                case NetIncomingMessageType.ErrorMessage:
                                case NetIncomingMessageType.WarningMessage:
                                case NetIncomingMessageType.VerboseDebugMessage:
                                    string text = msg.ReadString();
                                    Logging.Debug(text, this);
                                    break;

                                case NetIncomingMessageType.StatusChanged:
                                    NetConnectionStatus status = (NetConnectionStatus)msg.ReadByte();
                                    string reason = msg.ReadString();
                                    Logging.Debug(NetUtility.ToHexString(msg.SenderConnection.RemoteUniqueIdentifier) + " " + status + ": " + reason, this);

                                    if (status == NetConnectionStatus.Connected)
                                    {
                                        this.OnNewConnection(msg);
                                    }
                                    else if (status == NetConnectionStatus.Disconnected)
                                    {
                                        this.OnConnectionLost(msg);
                                    }

                                    break;

                                case NetIncomingMessageType.Data:
                                    //Logging.Debug("Data received", this);

                                    // Handle received data.
                                    msg = this.DecryptMessage(msg);
                                    if (msg != null)
                                    {
                                        this.MessageHandler.Handle(msg, msg.SenderConnection);
                                    }

                                    // The message is processed by another thread and so we don't allow to recycle it now.
                                    allowRecycle = false;
                                    break;

                                default:
                                    Logging.Debug("Unhandled type: " + msg.MessageType + " " + msg.LengthBytes + " bytes " + msg.DeliveryMethod + "|" + msg.SequenceChannel, this);
                                    break;
                            }

                            if (allowRecycle)
                            {
                                this.server.Recycle(msg);
                            }
                        }
                        // TODO: USE!
                        //Main.NetworkManager.MessageHandler.GetMessagesToRecycle();
                        Thread.Sleep(1);
                    }
                    catch (Exception exception)
                    {
                        Logging.Error("An exception occured while reading messages: " + exception.ToString(), this);
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Listen: " + ex.ToString(), this);
            }

            this.Running = false;

            // Fire event.
            ServerEventHandler handlerStopped = this.ServerStopped;
            if (handlerStopped != null)
            {
                handlerStopped();
            }

            Logging.Debug("Server closed", this);
        }

        /// <summary>
        /// Called when a new client connected.
        /// </summary>
        /// <param name="message">The message.</param>
        private void OnNewConnection(NetIncomingMessage message)
        {
            long ident = message.SenderConnection.RemoteUniqueIdentifier;

            if (this.clientManager.HasClient(ident))
            {
                Logging.Warning(string.Format("OnNewConnection: Connection {0} already in list", ident), this);
                return;
            }

            // Add client to our local manager.
            this.clientManager.AddClient(ident, message.SenderConnection);

            // Fire event.
            ClientConnectionEventHandler handler = this.ConnectionEstablished;
            if (handler != null)
            {
                NetworkClient client = this.clientManager.GetClient(ident);
                handler(client);

                // If handler disconnected client, don't proceed.
                if (client != null && client.ForcedDisconnected)
                {
                    return;
                }
            }

            // Say hello and tell version to client.
            NetOutgoingMessage outgoingMessage = this.BuildMessage(EMessageID.Welcome);
            outgoingMessage.Write("Hello, this is the server!");
            outgoingMessage.Write(this.name);
            outgoingMessage.Write(NetHelper.GetVersion());
            this.Send(outgoingMessage, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);

            // Ask for client's version.
            outgoingMessage = this.BuildMessage(EMessageID.Version);
            this.Send(outgoingMessage, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);
        }

        /// <summary>
        /// Called when the connection to the server has been lost.
        /// </summary>
        /// <param name="message">The message.</param>
        private void OnConnectionLost(NetIncomingMessage message)
        {
            ClientConnectionEventHandler handler = this.ConnectionLost;
            if (handler != null)
            {
                handler(this.clientManager.GetClient(message.SenderConnection.RemoteUniqueIdentifier));
            }
        }

        private void WelcomeHandlerFunction(NetIncomingMessage message, NetConnection sender)
        {
            // Read name.
            string name = message.ReadString();
            string identifier = NetUtility.ToHexString(sender.RemoteUniqueIdentifier);

            if (this.clientManager.HasClient(sender.RemoteUniqueIdentifier))
            {
                this.clientManager.GetClient(sender.RemoteUniqueIdentifier).Name = name;
                Logging.Info(string.Format("Client {0} is now known as {1}.", identifier, name), this);

                // Fire event.
                ClientConnectionEventHandler handler = this.ClientDetailsReceived;
                if (handler != null)
                {
                    handler(this.clientManager.GetClient(sender.RemoteUniqueIdentifier));
                }
            }
            else
            {
                Logging.Warning(string.Format("Client {0} not yet registered in client manager.", identifier), this);
            }
        }

        private void VersionHandlerFunction(NetIncomingMessage message, NetConnection sender)
        {
            // Read version.
            string msg = message.ReadString();
            string identifier = NetUtility.ToHexString(sender.RemoteUniqueIdentifier);

            if (this.clientManager.HasClient(sender.RemoteUniqueIdentifier))
            {
                string name = this.clientManager.GetClient(sender.RemoteUniqueIdentifier).SafeName;
                this.clientManager.GetClient(sender.RemoteUniqueIdentifier).Version = msg;
                Logging.Info(string.Format("Client {0} is running version {1}.", name, msg), this);

                // Fire event.
                ClientConnectionEventHandler handler = this.ClientDetailsReceived;
                if (handler != null)
                {
                    handler(this.clientManager.GetClient(sender.RemoteUniqueIdentifier));
                }
            }
            else
            {
                Logging.Warning(string.Format("Client {0} not yet registered in client manager.", identifier), this);
            }
        }
    }
}