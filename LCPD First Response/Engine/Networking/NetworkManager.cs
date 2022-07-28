namespace LCPD_First_Response.Engine.Networking
{
    using System;
    using System.Net;
    using System.Threading;

    using GTA;

    using LCPD_First_Response.Engine.GUI;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Events;
    using LCPD_First_Response.Engine.Scripting.Native;
    using LCPD_First_Response.Engine.Timers;

    using global::LCPDFR.Networking;

    using Newtonsoft.Json.Linq;

    // TODO: Properly close connection when leaving network game

    /// <summary>
    /// Describes generic network messages.
    /// </summary>
    internal enum EGenericNetworkMessages
    {
        /// <summary>
        /// A new player connected.
        /// </summary>
        PlayerJoined,

        /// <summary>
        /// A new network entity is being used, i.e. will probably be used soon.
        /// </summary>
        NewNetworkEntityIDUsed,
    }

    /// <summary>
    /// The network manager, responsible for the core networking.
    /// </summary>
    internal class NetworkManager : BaseComponent
    {
        /// <summary>
        /// The client handler.
        /// </summary>
        private ClientHandler clientHandler;

        /// <summary>
        /// The server handler.
        /// </summary>
        private ServerHandler serverHandler;

        /// <summary>
        /// Whether the host has been found.
        /// </summary>
        private bool foundHost;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkManager"/> class.
        /// </summary>
        public NetworkManager()
        {
            this.Port = 1337;
        }

        /// <summary>
        /// Gets the active network peer, either client or server.
        /// </summary>
        public BaseNetPeer ActivePeer
        {
            get
            {
                if (this.IsHost && this.serverHandler != null)
                {
                    return this.serverHandler;
                }
                else if (!this.IsHost && this.clientHandler != null)
                {
                    return this.clientHandler;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets a value indicating whether data can be transmitted, i.e. a connection is established.
        /// </summary>
        public bool CanSendData
        {
            get
            {
                if (this.IsHost && this.serverHandler != null)
                {
                    return this.serverHandler.Running;
                }
                else if (!this.IsHost && this.clientHandler != null)
                {
                    return this.clientHandler.IsConnected;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the local player is a host.
        /// </summary>
        public bool IsHost { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the local player is in a network session.
        /// </summary>
        public bool IsNetworkSession { get; private set; }

        /// <summary>
        /// Gets the name (GFWL) of the local player.
        /// </summary>
        public string LocalPlayerName
        {
            get
            {
                return CPlayer.LocalPlayer.Name;
            }
        }

        /// <summary>
        /// Gets the server name of the local player. Only works when player is host.
        /// </summary>
        public string LocalServerName
        {
            get
            {
                return Natives.NetworkGetServerName();
            }
        }

        /// <summary>
        /// Gets or sets the port to use for the server.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Gets the client.
        /// </summary>
        public ClientHandler Client
        {
            get
            {
                return this.clientHandler;
            }
        }

        /// <summary>
        /// Gets the server.
        /// </summary>
        public ServerHandler Server
        {
            get
            {
                return this.serverHandler;
            }
        }

        /// <summary>
        /// Initializes the network manager.
        /// </summary>
        public void Initialize()
        {
            ScriptHelper.BindConsoleCommandS("startScript", this.StartScriptConsoleCallback);

            Logging.MessageReceived += this.Logging_MessageReceived;

            // Check if network connection has been established already.
            if (Main.LCPDFRServer.IsServerAvailable)
            {
                this.Setup();
            }
            else
            {
                // If server not yet available, but connection hasn't failed, listen for connected event.
                if (Main.LCPDFRServer.ConnectionState == ENetworkConnectionState.Pending)
                {
                    Log.Debug("Initialize: Waiting for connection", this);

                    EventNetworkConnectionEstablished.EventRaised += new EventNetworkConnectionEstablished.EventRaisedEventHandler(this.EventNetworkConnectionEstablished_EventRaised);
                    EventNetworkConnectionFailed.EventRaised += new EventNetworkConnectionFailed.EventRaisedEventHandler(this.EventNetworkConnectionFailed_EventRaised);
                }
                else if (Main.LCPDFRServer.ConnectionState == ENetworkConnectionState.Failed)
                {
                    this.Setup();
                }
            }
        }

        /// <summary>
        /// Sends a message across the network that only consists of the network ID of an entity.
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        /// <param name="messageID">The message ID.</param>
        /// <param name="networkID">The network ID.</param>
        public void SendMessageWithNetworkID(string identifier, Enum messageID, int networkID)
        {
            Log.Debug(identifier + " -- " + messageID, this);

            DynamicData dynamicData = new DynamicData(Main.NetworkManager.ActivePeer);
            dynamicData.Write(networkID);
            Main.NetworkManager.ActivePeer.Send(identifier, messageID, dynamicData);
        }

        /// <summary>
        /// Sends a message across the network with payload specified as <see cref="DynamicData"/>.
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        /// <param name="messageID">The message ID.</param>
        /// <param name="dynamicData">The dynamic data.</param>
        public void SendMessage(string identifier, Enum messageID, DynamicData dynamicData)
        {
            Log.Debug(identifier + " -- " + messageID, this);

            Main.NetworkManager.ActivePeer.Send(identifier, messageID, dynamicData);
        }

        /// <summary>
        /// Processes incoming network activity.
        /// </summary>
        public void Process()
        {
            // Fetch all updates and process :)
            if (this.IsNetworkSession)
            {
                if (this.IsHost && this.serverHandler != null)
                {
                    this.serverHandler.ProcessQueue();
                }
                else if (!this.IsHost && this.clientHandler != null)
                {
                    this.clientHandler.ProcessQueue();
                }
            }
        }

        /// <summary>
        /// Starts the server.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="port">
        /// The port.
        /// </param>
        /// <param name="localIP">
        /// The local IP.
        /// </param>
        /// <param name="defaultGateway">
        /// The default gateway.
        /// </param>
        public void StartServer(string name, int port, string localIP, string defaultGateway)
        {
            this.serverHandler.Start(name, port, localIP, defaultGateway);

            // Tell masterserver our port.
            if (Main.LCPDFRServer.IsServerAvailable)
            {
                Main.LCPDFRServer.SetSessionVariable("Port", port.ToString());
                Log.Debug("Sent port to masterserver", this);
            } 
        }

        /// <summary>
        /// Connects to the server.
        /// </summary>
        /// <param name="ip">The IP address.</param>
        /// <param name="port">The port.</param>
        public void ConnectToServer(IPAddress ip, int port)
        {
            if (this.clientHandler.IsConnected)
            {
                Log.Warning("ConnectToServer: Can't establish connection since we're already connected", this);   
                return;
            }

            this.clientHandler.Connect(ip, port);
            this.clientHandler.ConnectionEstablished += this.clientHandler_ConnectionEstablished;
            this.clientHandler.ConnectionLost += this.clientHandler_ConnectionLost;
        }

        private void clientHandler_ConnectionEstablished(global::LCPDFR.Networking.User.NetworkServer server)
        {
            Log.Info(string.Format("ConnectionEstablished: Connected to {0}", server.NetworkEndPoint.Address.ToString()), this);
            HelpBox.Print(string.Format("Connected to server."));
        }

        private void clientHandler_ConnectionLost(global::LCPDFR.Networking.User.NetworkServer server, string message)
        {
            if (server == null)
            {
                Log.Info(string.Format("ConnectionLost: Failed to establish connection"), this);
                HelpBox.Print(string.Format("Failed to establish connection to server. Please make sure server has forwarded ports properly."));
            }
            else
            {
                Log.Info(string.Format("ConnectionLost: Connection lost to {0} ({1}", server.SafeName, server.NetworkEndPoint.Address.ToString()), this);
                HelpBox.Print(string.Format("Connection lost to server ({0}).", server.SafeName));
            }
        }

        /// <summary>
        /// Called when the networking engine has logged a message.
        /// </summary>
        /// <param name="text">The log message.</param>
        private void Logging_MessageReceived(string text)
        {
            Log.Info(text, this);
        }

        /// <summary>
        /// Fired when a connection to the LCPDFR server has been established.
        /// </summary>
        /// <param name="event">The event.</param>
        private void EventNetworkConnectionEstablished_EventRaised(EventNetworkConnectionEstablished @event)
        {
            this.Setup();
        }

        /// <summary>
        /// Fired when no connection could be established.
        /// </summary>
        /// <param name="event">The event.</param>
        private void EventNetworkConnectionFailed_EventRaised(EventNetworkConnectionFailed @event)
        {
            this.Setup();
        }

        /// <summary>
        /// Setups all network stuff in the first place.
        /// </summary>
        private void Setup()
        {
            // Check if this user is in a mp session and is host.
            if (Game.NetworkMode != NetworkMode.Singleplayer)
            {
                this.IsNetworkSession = true;
                this.IsHost = Natives.IsThisMachineTheServer();
            }

            Log.Info(string.Format("NetworkManager: In network session: {0} Is host: {1}", this.IsNetworkSession, this.IsHost), "NetworkManager");
            ScriptHelper.BindConsoleCommandS("net_status", this.StatusConsoleCallback);

            // If connected, communicate with LCPDFR masterserver in order to get the host's ip and/or register this server.
            if (Main.LCPDFRServer.IsServerAvailable)
            {
                // Share local details with masterserver.
                string localServerName = this.LocalServerName;
                string localPlayerName = this.LocalPlayerName;
                bool isHost = this.IsHost;
                bool isNetworkSession = this.IsNetworkSession;
                IPAddress ipAddress = Main.LCPDFRServer.IPAddress;

                Log.Debug("Sending: " + localServerName + " -- " + localPlayerName, this);

                var thread = new Thread(() => SetMasterserverVariables(localServerName, localPlayerName, isHost, isNetworkSession, ipAddress));
                thread.IsBackground = true;
                thread.Start();
            }

            // If in network session, a connection to the LCPD:FR server is vital
            if (this.IsNetworkSession)
            {
                // Check for internet connection
                if (!Main.LCPDFRServer.IsConnectedToTheInternet)
                {
                    HelpBox.Print("Welcome to LCPDFR Multiplayer " + this.LocalPlayerName + ". You are not connected to the internet. Please check your firewall settings.");
                    return;
                }

                // If host, start UDP-Server.
                if (this.IsHost)
                {
                    this.SetupHost();

                    // Set up host-only console commands.
                    ScriptHelper.BindConsoleCommandS("net_attachBlipPed", this.AttachBlipPedConsoleCallback);
                    ScriptHelper.BindConsoleCommandS("net_attachBlipVeh", this.AttachBlipVehConsoleCallback);
                    ScriptHelper.BindConsoleCommandS("net_deleteBlipPed", this.DeleteBlipPedConsoleCallback);
                    ScriptHelper.BindConsoleCommandS("net_deleteBlipVeh", this.DeleteBlipVehConsoleCallback);
                }
                else
                {
                    this.SetupClient();

                    // Set up client-only console commands.
                    ScriptHelper.BindConsoleCommandS("net_connect", this.ConnectConsoleCallback);
                    ScriptHelper.BindConsoleCommandS("net_welcome", this.WelcomeConsoleCallback);

                    // Ask server for all players on this server.
                    if (Main.LCPDFRServer.IsServerAvailable)
                    {
                        Log.Debug("Setup: Retrieving player list", this);
                        NetworkSession hostSession = this.GetNetworkHostSession();
                        if (hostSession != null)
                        {
                            Log.Info("Setup: Retrieved host data. Name: " + hostSession.LocalPlayerName, this);

                            IPAddress ipAddress = null;
                            if (IPAddress.TryParse(hostSession.Ip, out ipAddress))
                            {
                                int port = hostSession.Port;

                                if (hostSession.Port == -1)
                                {
                                    Log.Warning("Setup: Port is missing, defaulting to " + this.Port, this);
                                    port = this.Port;
                                }

                                // Connect to server.
                                this.foundHost = true;
                                Log.Info(string.Format("Setup: Connecting to {0}:{1}", ipAddress, port), this);
                                this.ConnectToServer(ipAddress, port);
                            }
                            else
                            {
                                Log.Warning("Setup: Failed to parse IP address", this);
                            }
                        }
                        else
                        {
                            Log.Warning("Setup: Failed to find host", this);
                        }
                    }
                }

                // Introduce player to LCPDFR Networking.
                this.IntroducePlayer();

                // Add generic console commands.
                ScriptHelper.BindConsoleCommandS("net_showIDPed", this.ShowIDPedConsoleCallback);
                ScriptHelper.BindConsoleCommandS("net_showIDVeh", this.ShowIDVehConsoleCallback);
                ScriptHelper.BindConsoleCommandS("net_showIP", this.ShowIPConsoleCallback);

                // Fire event
                // Ensure event is fired in the right thread
                DelayedCaller.Call(delegate { new EventJoinedNetworkGame(); }, this, 1);
            }
        }

        /// <summary>
        /// Sets the masterserver variables. This is static as accessing some of this information from a thread will cause crash.
        /// </summary>
        /// <param name="serverName">The server name.</param>
        /// <param name="playerName">The player name</param>
        /// <param name="isHost">Whether the player is host.</param>
        /// <param name="isNetworkSession">Whether player is connected to a network session.</param>
        /// <param name="ipAddress">The player's IP address.</param>
        private static void SetMasterserverVariables(string serverName, string playerName, bool isHost, bool isNetworkSession, IPAddress ipAddress)
        {
            if (!string.IsNullOrEmpty(serverName))
            {
                Main.LCPDFRServer.SetSessionVariable("ServerName", serverName);
            }

            Main.LCPDFRServer.SetSessionVariable("LocalName", playerName);
            Main.LCPDFRServer.SetSessionVariable("IsHost", isHost.ToString());
            Main.LCPDFRServer.SetSessionVariable("IsNetworkSession", isNetworkSession.ToString());
            Main.LCPDFRServer.SetSessionVariable("IP", ipAddress.ToString());
        }

        private void IntroducePlayer()
        {
            // If masterserver is not available.
            if (!Main.LCPDFRServer.IsServerAvailable)
            {
                // If host.
                if (this.IsHost)
                {
                    HelpBox.Print("Welcome to LCPDFR Multiplayer " + this.LocalPlayerName + ".~n~"
                                  + "Couldn't reach the master server. You are the host, share your IP with all clients so they can connect. IP: " + 
                                  Main.LCPDFRServer.IPAddress);

                    DelayedCaller.Call(parameter => HelpBox.Print("You can type net [underscore] showIP at any time during the game to display your external IP address."), this, 12000);
                }
                else
                {
                    HelpBox.Print("Welcome to LCPDFR Multiplayer " + this.LocalPlayerName + ". You are a client. Ask the host for his IP and type net [underscore] connect IP in your console.");
                }
            }
            else
            {
                if (this.IsHost)
                {
                    HelpBox.Print("Welcome to LCPDFR Multiplayer " + this.LocalPlayerName + ". ~n~"
                        + "You are connected to the LCPDFR master server. Clients will join automatically.");

                    DelayedCaller.Call(parameter => HelpBox.Print("You can type net [underscore] showIP at any time during the game to display your external IP address."), this, 12000);
                }
                else
                {
                    if (this.foundHost)
                    {
                        HelpBox.Print(
                            "Welcome to LCPDFR Multiplayer " + this.LocalPlayerName
                            + ". You are connected to the LCPDFR master server. "
                            + "~n~ Connection to game host is being established. To connect manually type net [underscore] connect IP in your console.");
                    }
                    else
                    {
                        HelpBox.Print(
                            "Welcome to LCPDFR Multiplayer " + this.LocalPlayerName
                            + ". You are connected to the LCPDFR master server. "
                            + "~n~ Failed to find host session on the master server. To connect manually type net [underscore] connect IP in your console.");
                    }
                }
            }
        }

        /// <summary>
        /// Called if user is client.
        /// </summary>
        private void SetupClient()
        {
            this.clientHandler = new ClientHandler() { Username = this.LocalPlayerName };
            SearchArea.HandleNetworkMessages();
        }

        /// <summary>
        /// Called if user is host.
        /// </summary>
        private void SetupHost()
        {   
            this.serverHandler = new ServerHandler();
        }

        /// <summary>
        /// Tries to get the network session of the host of the network game. Returns null if failed.
        /// </summary>
        /// <returns>The network session, if found. Null otherwise.</returns>
        private NetworkSession GetNetworkHostSession()
        {
            try
            {
                foreach (Player player in Game.PlayerList)
                {
                    // Skip itself.
                    if (player == Game.LocalPlayer)
                    {
                        continue;
                    }

                    Log.Debug("Using " + player.Name, this);
                    JArray resultsArray = Main.LCPDFRServer.FindSessionsByVariableSearch("LocalName", player.Name);
                    if (resultsArray != null)
                    {
                        Log.Debug("Got results: " + resultsArray.Count, this);

                        // Get all sessions.
                        foreach (JToken session in resultsArray)
                        {
                            if (session["vars"] != null)
                            {
                                Log.Debug("Got vars", this);

                                JToken sessionVars = session["vars"];
                                if (sessionVars["LocalName"] != null)
                                {
                                    Log.Debug("LocalName is: " + (string)sessionVars["LocalName"], this);

                                    bool isHost = false;
                                    bool isNetworkSession = false;
                                    string localName = null;
                                    string ip = null;
                                    int port = -1;

                                    // Extract arguments.
                                    if (sessionVars["IsHost"] != null)
                                    {
                                        isHost = sessionVars["IsHost"].Value<bool>();
                                    }

                                    if (sessionVars["IsNetworkSession"] != null)
                                    {
                                        isNetworkSession = sessionVars["IsNetworkSession"].Value<bool>();
                                    }

                                    if (sessionVars["LocalName"] != null)
                                    {
                                        localName = sessionVars["LocalName"].Value<string>();
                                    }

                                    if (sessionVars["IP"] != null)
                                    {
                                        ip = sessionVars["IP"].Value<string>();
                                    }

                                    if (sessionVars["Port"] != null)
                                    {
                                        port = sessionVars["Port"].Value<int>();
                                    }

                                    Log.Debug(string.Format("IsHost: {0} -- IsNetworkSession: {1} -- LocalName: {2} -- IP: {3} -- Port: {4}", isHost, isNetworkSession, localName, ip, port), this);

                                    // Check if player is host.
                                    if (isHost)
                                    {
                                        return new NetworkSession(string.Empty, localName, isHost, isNetworkSession, ip, port);
                                    }
                                }
                                else
                                {
                                    Log.Warning("RetrievePlayerList: Server data corrupted", this);
                                }
                            }
                        }
                    }
                    else
                    {
                        // Either server is down/broken or host is not listed.
                        Log.Warning("RetrievePlayerList: Failed to retrieve player list from masterserver", this);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("GetNetworkHostSession: An exception occured: " + ex.Message + ex.StackTrace, this);
            }

            return null;
        }

        #region "Console callbacks"
        private void AttachBlipPedConsoleCallback(ParameterCollection parameterCollection)
        {
            if (parameterCollection.Count == 1)
            {
                string sID = parameterCollection[0];
                int id;
                if (int.TryParse(sID, out id))
                {
                    CPed ped = CPed.FromNetworkID(id);
                    if (ped != null && ped.Exists())
                    {
                        ped.AttachBlip();
                    }
                }
                else
                {
                    Log.Error("AttachBlipPedConsoleCallback: Failed to parse network id. Input: " + sID, this);
                }
            }
            else
            {
                Game.Console.Print("AttachBlipPedConsoleCallback: Invalid argument count");
            }
        }

        private void AttachBlipVehConsoleCallback(ParameterCollection parameterCollection)
        {
            if (parameterCollection.Count == 1)
            {
                string sID = parameterCollection[0];
                int id;
                if (int.TryParse(sID, out id))
                {
                    CVehicle cVehicle = CVehicle.FromNetworkID(id);
                    if (cVehicle != null && cVehicle.Exists())
                    {
                        cVehicle.AttachBlip();
                    }
                }
                else
                {
                    Log.Error("AttachBlipVehConsoleCallback: Failed to parse network id. Input: " + sID, this);
                }
            }
            else
            {
                Game.Console.Print("AttachBlipVehConsoleCallback: Invalid argument count");
            }
        }

        private void ConnectConsoleCallback(ParameterCollection parameterCollection)
        {
            if (parameterCollection.Count == 1)
            {
                string ip = parameterCollection[0];
                string port = this.Port.ToString();
                if (ip.Contains(":"))
                {
                    port = ip.Substring(ip.IndexOf(":") + 1);
                }

                Log.Debug("ConnectConsoleCallback: IP input: " + ip, this);
                
                // Try parsing
                IPAddress ipAddress;
                if (IPAddress.TryParse(ip, out ipAddress))
                {
                    int intPort;
                    if (int.TryParse(port, out intPort))
                    {
                        Log.Debug("ConnectConsoleCallback: Parsed: " + ipAddress.ToString(), this);
                        this.ConnectToServer(ipAddress, intPort);
                        Log.Debug("ConnectConsoleCallback: Connecting: " + ipAddress.ToString(), this);
                    }
                }
                else
                {
                    Log.Info("ConnectConsoleCallback: Failed to parse IP. Input: " + ip, this);
                }
            }
            else
            {
                Log.Debug("ConnectConsoleCallback: Invalid argument count", this);
            }
        }

        private void CutsceneConsoleCallback(ParameterCollection parameterCollection)
        {
            if (parameterCollection.Count == 1)
            {
                //string name = parameterCollection[0];
                //Cutscene cutscene = new Cutscene("y1_aa");
                //cutscene.Play(this.IsNetworkSession);

                //Log.Debug("CutsceneConsoleCallback: Name " + name, this);

                //DynamicData dynamicData = new DynamicData();
                //dynamicData.Write("YUSUF1");
                //dynamicData.Write((byte)0);
                //Main.NetworkManager.Send(EMessageID.StartCutsceneLoadText, dynamicData, NetDeliveryMethod.ReliableUnordered, EMessageReceiver.AllClients);

                //dynamicData.Clear();
                //dynamicData.Write("E2Y1Aud");
                //dynamicData.Write((byte)6);
                //Main.NetworkManager.Send(EMessageID.StartCutsceneLoadText, dynamicData, NetDeliveryMethod.ReliableUnordered, EMessageReceiver.AllClients);

                //dynamicData.Clear();
                //dynamicData.Write("y1_aa");
                //Main.NetworkManager.Send(EMessageID.StartCutscene, dynamicData, NetDeliveryMethod.ReliableUnordered, EMessageReceiver.AllClients);


                //// Todo: Create cutscene class (probably with cutscene name and audio names together)

                ////if (name == "ready")
                ////{
                ////    bool ready = GTA.Native.Function.Call<bool>("IS_PLAYER_READY_FOR_CUTSCENE", GTA.Game.LocalPlayer);
                ////    Game.Console.Print("Ready: " + ready.ToString());
                ////}
                ////else if (name == "play")
                ////{

                //Log.Debug("Start now", this);
                //GTA.Native.Function.Call("LOAD_ADDITIONAL_TEXT", "YUSUF1", 0);
                //GTA.Native.Function.Call("LOAD_ADDITIONAL_TEXT", "E2Y1Aud", 6);
                //GTA.Native.Function.Call("LOAD_ALL_OBJECTS_NOW");

                //// Ensure mode is luis
                //GTA.Model oldModel = GTA.Game.LocalPlayer.Model;
                //GTA.value.PedSkin oldSkin = GTA.Game.LocalPlayer.Character.Skin;
                //bool needModelSwitch = false;
                //if (GTA.Game.LocalPlayer.Model != "PLAYER")
                //{
                //    GTA.Game.LocalPlayer.Model = "PLAYER";
                //    needModelSwitch = true;
                //}

                //GTA.Native.Function.Call("START_CUTSCENE_NOW", "y1_aa");
                //while (!GTA.Native.Function.Call<bool>("HAS_CUTSCENE_LOADED"))
                //{
                //    Game.WaitInCurrentScript(0);
                //    Game.Console.Print("loading");
                //    //WAIT(0);
                //}
                //Log.Debug("Loaded", this);
                //while (!GTA.Native.Function.Call<bool>("HAS_CUTSCENE_FINISHED"))
                //{
                //    Game.Console.Print("playing");
                //    Game.WaitInCurrentScript(0);
                //    //WAIT(0);
                //}
                //if (needModelSwitch)
                //{
                //    GTA.Game.LocalPlayer.Model = oldModel;
                //    GTA.Game.LocalPlayer.Skin.Template = oldSkin;
                //}

                //Log.Debug("Finished", this);
                //dynamicData.Clear();
                //dynamicData.Write("y1_aa");
                //Main.NetworkManager.Send(EMessageID.ClearCutscene, dynamicData, NetDeliveryMethod.ReliableUnordered, EMessageReceiver.AllClients);

                //GTA.Native.Function.Call("CLEAR_NAMED_CUTSCENE", "y1_aa");
                //Game.FadeScreenIn(4000);
                //Log.Debug("Cleaned", this);
                ////}
            }
        }

        private void DeleteBlipPedConsoleCallback(ParameterCollection parameterCollection)
        {
            if (parameterCollection.Count == 1)
            {
                string sID = parameterCollection[0];
                int id;
                if (int.TryParse(sID, out id))
                {
                    CPed ped = CPed.FromNetworkID(id);
                    if (ped != null && ped.Exists())
                    {
                        ped.DeleteBlip();
                    }
                }
                else
                {
                    Log.Error("DeleteBlipPedConsoleCallback: Failed to parse network id. Input: " + sID, this);
                }
            }
            else
            {
                Game.Console.Print("DeleteBlipPedConsoleCallback: Invalid argument count");
            }
        }

        private void DeleteBlipVehConsoleCallback(ParameterCollection parameterCollection)
        {
            if (parameterCollection.Count == 1)
            {
                string sID = parameterCollection[0];
                int id;
                if (int.TryParse(sID, out id))
                {
                    CVehicle cVehicle = CVehicle.FromNetworkID(id);
                    if (cVehicle != null && cVehicle.Exists())
                    {
                        cVehicle.DeleteBlip();
                    }
                }
                else
                {
                    Log.Error("DeleteBlipVehConsoleCallback: Failed to parse network id. Input: " + sID, this);
                }
            }
            else
            {
                Game.Console.Print("DeleteBlipVehConsoleCallback: Invalid argument count");
            }
        }

        private void ShowIDPedConsoleCallback(ParameterCollection parameterCollection)
        {
            // Show network id of player ped
            Game.Console.Print(CPlayer.LocalPlayer.Ped.NetworkID.ToString());
        }

        private void ShowIDVehConsoleCallback(ParameterCollection parameterCollection)
        {
            // Show network id of player's current vehicle
            if (CPlayer.LocalPlayer.Ped.IsInVehicle)
            {
                Game.Console.Print(CPlayer.LocalPlayer.Ped.CurrentVehicle.NetworkID.ToString());
            }
            else
            {
                Game.Console.Print("ShowVehIDConsoleCallback: Player not in vehicle");
            }
        }

        private void ShowIPConsoleCallback(ParameterCollection parameterCollection)
        {
            Game.Console.Print(Main.LCPDFRServer.IPAddress.ToString());
            HelpBox.Print(Main.LCPDFRServer.IPAddress.ToString());
        }

        private void StartScriptConsoleCallback(ParameterCollection parameterCollection)
        {
            if (parameterCollection.Count > 0)
            {
                string name = parameterCollection[0];
                int counter = 0;

                if (CPlayer.LocalPlayer.Model != "PLAYER")
                {
                    CPlayer.LocalPlayer.Model = "PLAYER";
                }

                Log.Debug("Requesting script", this);
                GTA.Native.Function.Call("REQUEST_SCRIPT", name);

                while (!GTA.Native.Function.Call<bool>("HAS_SCRIPT_LOADED", name))
                {
                    GTA.Game.WaitInCurrentScript(10);
                    counter++;

                    if (counter > 1000)
                    {
                        return;
                    }
                }

                Log.Debug("Scipt loaded, starting...", this);

                GTA.Native.Function.Call("START_NEW_SCRIPT", name, 4096);
                GTA.Native.Function.Call("MarkScriptAsNoLongerNeeded");
            }
        }

        private void StatusConsoleCallback(ParameterCollection parameterCollection)
        {
            Log.Debug(string.Format("NetworkManager: In network session: {0} Is host: {1}", this.IsNetworkSession, this.IsHost), this);
        }

        private void WelcomeConsoleCallback(ParameterCollection parameterCollection)
        {
            // Send name to server
            string name = Main.NetworkManager.LocalServerName;
        }

        #endregion

        public override string ComponentName
        {
            get { return "NetworkManager"; }
        }
    }

    enum EMessageReceiver
    {
        /// <summary>
        /// All clients, can only be used when host
        /// </summary>
        AllClients,
        /// <summary>
        /// Requires additional information about the target client
        /// </summary>
        Client,
        /// <summary>
        /// Message from client
        /// </summary>
        Host,     
    }
}
