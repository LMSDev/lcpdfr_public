//namespace LCPD_First_Response.Engine.Networking.Data
//{
//    using System;
//    using System.IO;

//    using Lidgren.Network;

//    using ProtoBuf;

//    class ServerHandler : BaseComponent, IRPCHandler
//    {
//        public void Register()
//        {
//            Main.NetworkManager.MessageHandler.AddHandler(EMessageID.PlayerJoinedName, PlayerJoinedName);

//            Main.NetworkManager.MessageHandler.AddHandler(EMessageID.AttachBlip, AttachBlip);
//            Main.NetworkManager.MessageHandler.AddHandler(EMessageID.SetAsRequiredForMission, SetAsRequiredForMission);
//        }

//        public void Unregister()
//        {
//            throw new NotImplementedException();
//        }

//        private void PlayerJoinedName(NetIncomingMessage message, NetConnection sender)
//        {
//            MemoryStream memoryStream = new MemoryStream(Main.NetworkManager.MessageHandler.GetMessageByteData(message));
//            PlayerNameData playerNameData = Serializer.Deserialize<PlayerNameData>(memoryStream);
//            Log.Debug("PlayerJoinedName: " + playerNameData.NetworkName, this);

//            // New player, send ip and name to all clients (except the sender)
//            PlayerData playerData = new PlayerData(sender.RemoteEndpoint.Address, playerNameData.NetworkName);
//            Main.NetworkManager.Broadcast(EMessageID.PlayerJoined, playerData, NetDeliveryMethod.ReliableUnordered, message.SenderConnection);
//        }

//        private void AttachBlip(NetIncomingMessage message, NetConnection sender)
//        {
//            // Broadcast message
//            Main.NetworkManager.Broadcast(message, sender);
//        }

//        private void SetAsRequiredForMission(NetIncomingMessage message, NetConnection sender)
//        {
//            Main.NetworkManager.Broadcast(message, sender);
//        }

//        public override string ComponentName
//        {
//            get { return "ServerHandler"; }
//        }
//    }
//}
