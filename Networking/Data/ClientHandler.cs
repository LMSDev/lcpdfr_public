//namespace LCPD_First_Response.Engine.Networking.Data
//{
//    using System;

//    using GTA;

//    using LCPD_First_Response.Engine.Scripting.Entities;

//    using Lidgren.Network;

//    using ProtoBuf;

//    using MemoryStream = System.IO.MemoryStream;

//    class ClientHandler : BaseComponent, IRPCHandler
//    {
//        public void Register()
//        {
//            Main.NetworkManager.MessageHandler.AddHandler(EMessageID.PlayerJoined, PlayerJoined);

//            Main.NetworkManager.MessageHandler.AddHandler(EMessageID.AttachBlip, AttachBlip);
//            Main.NetworkManager.MessageHandler.AddHandler(EMessageID.ClearCutscene, ClearCutscene);
//            Main.NetworkManager.MessageHandler.AddHandler(EMessageID.SetAsRequiredForMission, SetAsRequiredForMission);
//            Main.NetworkManager.MessageHandler.AddHandler(EMessageID.StartCutscene, StartCutscene);
//            Main.NetworkManager.MessageHandler.AddHandler(EMessageID.StartCutsceneLoadText, StartCutsceneLoadText);
//        }

//        public void Unregister()
//        {  
//            throw new NotImplementedException();
//        }

//        private void PlayerJoined(NetIncomingMessage message, NetConnection sender)
//        {
//            MemoryStream memoryStream = new MemoryStream(message.ReadBytes(message.LengthBytes - 1));
//            PlayerData playerData = Serializer.Deserialize<PlayerData>(memoryStream);
//            Log.Debug("PlayerJoined: " + playerData.NetworkName, this);
//        }

//        private void AttachBlip(NetIncomingMessage message, NetConnection sender)
//        {
//            byte entityID = message.ReadByte();
//            int networkID = message.ReadInt32();

//            if (((EEntityType) entityID) == EEntityType.Ped)
//            {
//                CPed ped = CPed.FromNetworkID(networkID);
//                if (ped != null && ped.Exists())
//                {
//                    ped.AttachBlip(sync: false);
//                }
//            }

//            if (((EEntityType)entityID) == EEntityType.Vehicle)
//            {
//                CVehicle vehicle = CVehicle.FromNetworkID(networkID);
//                if (vehicle != null && vehicle.Exists())
//                {
//                    vehicle.AttachBlip(sync: false);
//                }
//            }
//        }

//        private void ClearCutscene(NetIncomingMessage message, NetConnection sender)
//        {
//            string name = message.ReadString();
//            GTA.Native.Function.Call("CLEAR_NAMED_CUTSCENE", name);
//            Game.FadeScreenIn(4000);
//        }

//        private void SetAsRequiredForMission(NetIncomingMessage message, NetConnection sender)
//        {
//            byte entityID = message.ReadByte();
//            int networkID = message.ReadInt32();
//            bool requiredForMission = message.ReadBoolean();

//            if (((EEntityType)entityID) == EEntityType.Ped)
//            {
//                CPed ped = CPed.FromNetworkID(networkID);
//                if (ped != null && ped.Exists())
//                {
//                    if (requiredForMission)
//                    {
//                        ped.BecomeMissionCharacter(sync: false);
//                    }
//                    else
//                    {
//                        ped.NoLongerNeeded(sync: false);
//                    }
//                }
//            }
//        }

//        private void StartCutscene(NetIncomingMessage message, NetConnection sender)
//        {
//            // Todo: Create cutscene class (probably with cutscene name and audio names together)

//            string name = message.ReadString();

//            // Ensure mode is luis
//            GTA.Model oldModel = GTA.Game.LocalPlayer.Model;
//            GTA.value.PedSkin oldSkin = GTA.Game.LocalPlayer.Character.Skin;
//            bool needModelSwitch = false;
//            if (GTA.Game.LocalPlayer.Model != "PLAYER")
//            {
//                GTA.Game.LocalPlayer.Model = "PLAYER";
//                needModelSwitch = true;
//            }

//            GTA.Native.Function.Call("LOAD_ALL_OBJECTS_NOW");
//            GTA.Native.Function.Call("START_CUTSCENE_NOW", name);
//            while (!GTA.Native.Function.Call<bool>("HAS_CUTSCENE_LOADED"))
//            {
//                Game.WaitInCurrentScript(0);
//                Game.Console.Print("loading");
//                //WAIT(0);
//            }
//            Log.Debug("Loaded", this);
//            while (!GTA.Native.Function.Call<bool>("HAS_CUTSCENE_FINISHED"))
//            {
//                Game.Console.Print("playing");
//                Game.WaitInCurrentScript(0);
//                //WAIT(0);
//            }
//            if (needModelSwitch)
//            {
//                GTA.Game.LocalPlayer.Model = oldModel;
//                GTA.Game.LocalPlayer.Skin.Template = oldSkin;
//            }
//            Log.Debug("Finished", this);
//            //GTA.Native.Function.Call("CLEAR_NAMED_CUTSCENE", name);
//            Game.FadeScreenIn(4000);
//            Log.Debug("Cleaned", this);
//        }

//        private void StartCutsceneLoadText(NetIncomingMessage message, NetConnection sender)
//        {
//            string name = message.ReadString();
//            int id = message.ReadByte(); // So the native gets a int rather than a byte

//            GTA.Native.Function.Call("LOAD_ADDITIONAL_TEXT", name, id);
//        }
        
//        public override string ComponentName
//        {
//            get { return "ClientHandler"; }
//        }
//    }
//}
