namespace LCPD_First_Response.Engine.Networking
{
    using global::LCPDFR.Networking;
    using global::LCPDFR.Networking.Server;
    using global::LCPDFR.Networking.User;

    using LCPD_First_Response.Engine.GUI;
    using LCPD_First_Response.Engine.Scripting.Entities;

    class ServerHandler : Server
    {
        /// <summary>
        /// The local client.
        /// </summary>
        private ClientHandler localClient;

        //public ServerHandler(string name, int port)
        //{

        //    //Main.NetworkManager.MessageHandler.AddHandler(EMessageID.PlayerJoinedName, PlayerJoinedName);

        //    //Main.NetworkManager.MessageHandler.AddHandler(EMessageID.AttachBlip, AttachBlip);
        //    //Main.NetworkManager.MessageHandler.AddHandler(EMessageID.SetAsRequiredForMission, SetAsRequiredForMission);
        //}


        //private void PlayerJoinedName(NetIncomingMessage message, NetConnection sender)
        //{
        //    MemoryStream memoryStream = new MemoryStream(Main.NetworkManager.MessageHandler.GetMessageByteData(message));
        //    PlayerNameData playerNameData = Serializer.Deserialize<PlayerNameData>(memoryStream);
        //    Log.Debug("PlayerJoinedName: " + playerNameData.NetworkName, this);

        //    // New player, send ip and name to all clients (except the sender)
        //    PlayerData playerData = new PlayerData(sender.RemoteEndpoint.Address, playerNameData.NetworkName);
        //    Main.NetworkManager.Broadcast(EMessageID.PlayerJoined, playerData, NetDeliveryMethod.ReliableUnordered, message.SenderConnection);
        //}

        //private void AttachBlip(NetIncomingMessage message, NetConnection sender)
        //{
        //    // Broadcast message
        //    Main.NetworkManager.Broadcast(message, sender);
        //}

        //private void SetAsRequiredForMission(NetIncomingMessage message, NetConnection sender)
        //{
        //    Main.NetworkManager.Broadcast(message, sender);
        //}

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerHandler"/> class.
        /// </summary>
        public ServerHandler()
        {
            this.ClientDetailsReceived += this.ServerHandler_ClientDetailsReceived;
            this.localClient = new ClientHandler();

            this.AddUserDataHandler("CPed", EPedNetworkMessages.AttachBlip, this.AttachBlip);
        }

        void ServerHandler_ClientDetailsReceived(NetworkClient client)
        {
            if (client.Name != null && client.Version != null)
            {
                HelpBox.Print(string.Format("{0} connected from {1}", client.SafeName, client.NetworkEndPoint.Address));
            }
        }

        private void AttachBlip(NetworkClient sender, ReceivedUserMessage message)
        {
            // Broadcast to all connected clients but the sender.
            this.SendToExcept(new UserMessage(message), sender);
            this.localClient.AttachBlip(null, message);
        }
    }
}