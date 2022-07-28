namespace LCPDFR.Networking
{
    using System;
    using System.Net;
    using System.Threading;

    using LCPDFR.Networking.User;

    using Lidgren.Network;

    /// <summary>
    /// The delegate for messages received by the server.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="message">The message.</param>
    public delegate void ServerUserMessageHandlerFunction(NetworkServer sender, ReceivedUserMessage message);

    /// <summary>
    /// The delegate for when the connection to the server is established or data received.
    /// </summary>
    /// <param name="server">The server. Can be null.</param>
    public delegate void ServerConnectionEventHandler(NetworkServer server);

    /// <summary>
    /// The delegate for when the connection to the server is lost.
    /// </summary>
    /// <param name="server">The server. Can be null.</param>
    /// <param name="message">The message from the server, if any. Can be null.</param>
    public delegate void ServerConnectionMessageEventHandler(NetworkServer server, string message);

    /// <summary>
    /// Represents a network client that can connect to the given IPAddress as well as managing sending and receiving messages.
    /// Does not process messages automatically, but only when <see cref="Client.ProcessQueue"/> is called (to allow synchronous processing).
    /// </summary>
    public class Client : BaseNetPeer, ILoggable
    {
        /// <summary>
        /// The network identifier.
        /// </summary>
        private const string NetworkIdentifier = "LCPDFR Networking";

        /// <summary>
        /// The port to connect to.
        /// </summary>
        private int port;

        /// <summary>
        /// The server we are connected to.
        /// </summary>
        private NetworkServer networkServer;

        /// <summary>
        /// Whether or not the server thread should be aborted.
        /// </summary>
        private bool abort;

        /// <summary>
        /// The internal lidgren client instance.
        /// </summary>
        private NetClient client;

        /// <summary>
        /// The thread running the client loop.
        /// </summary>
        private Thread thread;

        /// <summary>
        /// Initializes a new instance of the <see cref="Client"/> class.
        /// </summary>
        public Client() : this(null, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Client"/> class.
        /// </summary>
        /// <param name="ipAddress">The IP address to connect to.</param>
        /// <param name="port">The port to use.</param>
        public Client(IPAddress ipAddress, int port) : base(NetworkIdentifier)
        {
            this.port = port;
            this.HostIPAddress = ipAddress;
            this.Username = NetHelper.GetUserName();

            // Set up default handlers.
            this.MessageHandler.AddHandler(EMessageID.Welcome, this.WelcomeHandlerFunction);
            this.MessageHandler.AddHandler(EMessageID.Version, this.VersionHandlerFunction);
        }

        /// <summary>
        /// The event that is fired when a connection to the server has been established.
        /// </summary>
        public event ServerConnectionEventHandler ConnectionEstablished;

        /// <summary>
        /// The event that is fired when the connection to the server has been lost.
        /// </summary>
        public event ServerConnectionMessageEventHandler ConnectionLost;

        /// <summary>
        /// The event that is fired when the name and the version of a client have been received.
        /// </summary>
        public event ServerConnectionEventHandler ServerDetailsReceived;

        /// <summary>
        /// Gets the component name.
        /// </summary>
        public new string ComponentName
        {
            get { return "Client"; }
        }

        /// <summary>
        /// Gets the host ip address.
        /// </summary>
        public IPAddress HostIPAddress { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the client is (still) connected to the server.
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// Gets or sets the username. By default, <see cref="NetHelper.GetUserName"/> is used.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Closes the client and all its connections and sends <paramref name="goodbyeMessage"/> to the server.
        /// Waits one second before returning to allow proper termination.
        /// </summary>
        /// <param name="goodbyeMessage">The message that is sent to the server.</param>
        public override void Close(string goodbyeMessage)
        {
            this.client.Shutdown(goodbyeMessage);
            this.abort = true;
            Thread.Sleep(1000);
        }

        /// <summary>
        /// Connects to the server. This method is asynchronous.
        /// </summary>
        public void Connect()
        {
            if (this.HostIPAddress == null)
            {
                throw new InvalidOperationException("No client details specified.");
            }

            this.Connect(this.HostIPAddress, this.port);
        }

        /// <summary>
        /// Connects to the server. This method is asynchronous.
        /// </summary>
        /// <param name="ipAddress">
        /// The IP address.
        /// </param>
        /// <param name="port">
        /// The port.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Throws an exception when already connected.
        /// </exception>
        public void Connect(IPAddress ipAddress, int port)
        {
            if (this.IsConnected)
            {
                throw new InvalidOperationException("Client is already connected.");
            }

            this.port = port;
            this.HostIPAddress = ipAddress;

            Logging.Debug("Connecting to IP: " + this.HostIPAddress.ToString(), this);

            // Start thread
            this.thread = new Thread(this.ConnectInternal) { IsBackground = true };
            this.thread.Start();
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
            // Verify our server is the one that sent the message.
            if (this.networkServer != null)
            {
                if (this.networkServer.Identifier == NetUtility.ToHexString(sender.RemoteUniqueIdentifier))
                {
                    // Invoke function.
                    ServerUserMessageHandlerFunction serverFunc = (ServerUserMessageHandlerFunction)function;
                    serverFunc.Invoke(this.networkServer, new ReceivedUserMessage(message, serverFunc, this.networkServer));
                }
            }
        }

        /// <summary>
        /// The internal connect function, which also handles the message loop.
        /// </summary>
        private void ConnectInternal()
        {
            try
            {
                // Setup client.
                Logging.Debug("ConnectInternal: Thread running", this);
                var configuration = new NetPeerConfiguration(this.AppIdentifier);
                configuration.AutoFlushSendQueue = false;
                this.client = new NetClient(configuration);
                this.client.Start();
                Logging.Debug("ConnectInternal: Client running", this);

                // Connect to server.
                var ipEndPoint = new IPEndPoint(this.HostIPAddress, this.port);
                this.client.Connect(ipEndPoint);
                Logging.Debug("ConnectInternal: Connected", this);

                // Message loop.
                while (true)
                {
                    if (this.abort)
                    {
                        break;
                    }

                    bool allowRecycle = true;
                    NetIncomingMessage msg;

                    while ((msg = this.client.ReadMessage()) != null)
                    {
                        //Logging.Debug("ConnectInternal: Message available. Type: " + msg.MessageType.ToString(), this);
                        // handle incoming message.
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

                                if (status == NetConnectionStatus.Connected)
                                {
                                    this.IsConnected = true;
                                    this.OnConnectionEstablished(msg);
                                    
                                    Logging.Debug("Connection established", this);
                                }
                                else if (status == NetConnectionStatus.Disconnected)
                                {
                                    this.IsConnected = false;
                                    this.OnConnectionLost(msg, reason);

                                    Logging.Debug("Disconnected", this);
                                }

                                Logging.Debug(status.ToString() + ": " + reason, this);
                                break;

                            case NetIncomingMessageType.Data:
                                // Handle received data.
                                msg = this.DecryptMessage(msg);
                                this.MessageHandler.Handle(msg, msg.SenderConnection);

                                // The message is processed by another thread and so we don't allow to recycle it now.
                                allowRecycle = false;
                                break;

                            default:
                                Logging.Debug("Unhandled type: " + msg.MessageType + " " + msg.LengthBytes + " bytes", this);
                                break;
                        }

                        if (allowRecycle)
                        {
                            this.client.Recycle(msg);
                        }
                    }

                    // TODO: USE!
                    //Main.NetworkManager.MessageHandler.GetMessagesToRecycle();
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                Logging.Error("ConnectInternal: " + ex.ToString(), this);
            }

            Logging.Debug("Client closed", this);
        }

        /// <summary>
        /// Gets the internal <see cref="NetPeer"/> instance.
        /// </summary>
        /// <returns>The instance.</returns>
        internal override NetPeer GetInternalNetPeer()
        {
            return this.client;
        }


        /// <summary>
        /// Called when a connection to the server has been established.
        /// </summary>
        /// <param name="message">The message.</param>
        private void OnConnectionEstablished(NetIncomingMessage message)
        {
            if (this.networkServer != null)
            {
                Logging.Warning("OnConnectionEstablished: Already connected to a server", this);
            }

            this.networkServer = new NetworkServer(NetUtility.ToHexString(message.SenderConnection.RemoteUniqueIdentifier), message.SenderConnection);

            ServerConnectionEventHandler handler = this.ConnectionEstablished;
            if (handler != null)
            {
                handler(this.networkServer);
            }
        }

        /// <summary>
        /// Called when the connection to the server has been lost.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="reason">The reason.</param>
        private void OnConnectionLost(NetIncomingMessage message, string reason)
        {
            ServerConnectionMessageEventHandler handler = this.ConnectionLost;
            if (handler != null)
            {
                handler(this.networkServer, reason);
            }
        }

        /// <summary>
        /// Adds a user data handler with the specified details for messages received from the server.
        /// </summary>
        /// <param name="identifier">
        /// The message identifier. This can be used to easily separate communication and allow different enums (with the same values).
        /// For instance, an identifier could be "Vehicle" and another could be "Ped" with their respective enums.
        /// Limited to 255 chars.
        /// </param>
        /// <param name="messageID">The message ID.</param>
        /// <param name="handlerFunction">The associated handler function.</param>
        public void AddUserDataHandler(string identifier, Enum messageID, ServerUserMessageHandlerFunction handlerFunction)
        {
            this.AddUserDataHandler(identifier, (int)(object)messageID, handlerFunction);
        }

        /// <summary>
        /// Adds a user data handler with the specified details for messages received from the server.
        /// </summary>
        /// <param name="identifier">
        /// The message identifier. This can be used to easily separate communication and allow different enums (with the same values).
        /// For instance, an identifier could be "Vehicle" and another could be "Ped" with their respective enums.
        /// Limited to 255 chars.
        /// </param>
        /// <param name="messageID">The message ID.</param>
        /// <param name="handlerFunction">The associated handler function.</param>
        public void AddUserDataHandler(string identifier, int messageID, ServerUserMessageHandlerFunction handlerFunction)
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
        /// Sends a user message.
        /// </summary>
        /// <param name="userMessage">The user message.</param>
        public override void Send(UserMessage userMessage)
        {
            // Construct message.
            NetOutgoingMessage outgoingMessage = this.client.CreateMessage();
            outgoingMessage.Write((byte)EMessageID.UserPayload);
            outgoingMessage.Write(userMessage.GetBytes());
            this.SendAndFlush(outgoingMessage, NetDeliveryMethod.ReliableOrdered);
        }

        internal void Send(NetOutgoingMessage outgoingMessage, NetDeliveryMethod deliveryMethod)
        {
            NetSendResult result = this.SendAndFlush(outgoingMessage, deliveryMethod);
        }

        private NetSendResult SendAndFlush(NetOutgoingMessage message, NetDeliveryMethod deliveryMethod)
        {
            message = this.EncryptMessage(message);
            NetSendResult result = this.client.SendMessage(message, deliveryMethod);
            Logging.Debug("SendAndFlush: Result: " + result.ToString(), this);
            this.client.FlushSendQueue();
            return result;
        }

        private void WelcomeHandlerFunction(NetIncomingMessage message, NetConnection sender)
        {
            // Read message.
            string msg = message.ReadString();
            string name = message.ReadString();
            string version = message.ReadString();

            this.networkServer.Name = name;
            this.networkServer.Version = version;
            Logging.Info(string.Format("[from {0}] Welcome message received: {1}. Server is running version {2}", name, msg, version), this);

            // Say hello to server by telling our name.
            NetOutgoingMessage outgoingMessage = this.BuildMessage(EMessageID.Welcome);
            outgoingMessage.Write(this.Username);
            this.SendAndFlush(outgoingMessage, NetDeliveryMethod.ReliableOrdered);

            // Fire event.
            ServerConnectionEventHandler handler = this.ServerDetailsReceived;
            if (handler != null)
            {
                handler(this.networkServer);
            }
        }

        private void VersionHandlerFunction(NetIncomingMessage message, NetConnection sender)
        {
            // Get version and date.
            string version = NetHelper.GetVersion();

            // Tell version.
            NetOutgoingMessage outgoingMessage = this.BuildMessage(EMessageID.Version);
            outgoingMessage.Write(version);

            this.SendAndFlush(outgoingMessage, NetDeliveryMethod.ReliableOrdered);
        }
    }
}