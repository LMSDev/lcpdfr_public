namespace LCPD_First_Response.Engine.Networking
{
    using global::LCPDFR.Networking;
    using global::LCPDFR.Networking.User;

    using GTA;

    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Timers;

    using Lidgren.Network;

    class ClientHandler : Client
    {
        private MessageCache messageCache;

        private NonAutomaticTimer messageCacheTimer;

        public ClientHandler()
        {
            this.AddUserDataHandler("CPed", EPedNetworkMessages.AttachBlip, this.AttachBlip);
            this.AddUserDataHandler("CVehicle", EVehicleNetworkMessages.AttachBlip, this.AttachBlipVehicle);
            this.AddUserDataHandler("CPed", EPedNetworkMessages.RemoveBlip, this.RemoveBlip);
            this.AddUserDataHandler("CVehicle", EVehicleNetworkMessages.RemoveBlip, this.RemoveBlipVehicle);
            this.AddUserDataHandler("CPed", EPedNetworkMessages.SetBlipMode, this.SetBlipModePed);
            this.AddUserDataHandler("CVehicle", EVehicleNetworkMessages.SetBlipMode, this.SetBlipModeVehicle);
            this.AddUserDataHandler("Networking", EGenericNetworkMessages.NewNetworkEntityIDUsed, this.NewNetworkEntityIDUsed);
            
            this.messageCache = new MessageCache();
            this.messageCacheTimer = new NonAutomaticTimer(500);

            //Main.NetworkManager.MessageHandler.AddHandler(EMessageID.PlayerJoined, PlayerJoined);

            //Main.NetworkManager.MessageHandler.AddHandler(EMessageID.AttachBlip, AttachBlip);
            //Main.NetworkManager.MessageHandler.AddHandler(EMessageID.ClearCutscene, ClearCutscene);
            //Main.NetworkManager.MessageHandler.AddHandler(EMessageID.SetAsRequiredForMission, SetAsRequiredForMission);
            //Main.NetworkManager.MessageHandler.AddHandler(EMessageID.StartCutscene, StartCutscene);
            //Main.NetworkManager.MessageHandler.AddHandler(EMessageID.StartCutsceneLoadText, StartCutsceneLoadText);
        }

        /// <summary>
        /// Processes the incoming message queue. When not called, no messages are processed and no callbacks are fired. 
        /// Processing is done on the calling thread to ensure synchronous callbacks.
        /// </summary>
        public override void ProcessQueue()
        {
            base.ProcessQueue();

            if (this.messageCacheTimer.CanExecute())
            {
                this.messageCache.Process();
            }
        }

        public void CacheIfPossible(int networkID, ReceivedUserMessage message)
        {
            if (this.messageCache.HasIDBeenCreatedRecently(networkID) && !this.messageCache.IsInQueue(message))
            {
                this.messageCache.AddMessage(message);
                Log.Debug("CacheIfPossible: Network ID invalid on client as yet, adding to queue", this);
            }
        }

        public void NewNetworkEntityIDUsed(NetworkServer sender, ReceivedUserMessage message)
        {
            int networkID = message.ReadInt32();

            if (!this.messageCache.HasIDBeenCreatedRecently(networkID))
            {
                Log.Debug("NewNetworkEntityIDUsed: Remembering " + networkID, this);
                this.messageCache.AddRecentlyCreatedID(networkID);
            }
        }

        public void AttachBlip(NetworkServer sender, ReceivedUserMessage message)
        {
            int networkID = message.ReadInt32();

            Log.Debug("AttachBlip: Asked to attach blip for " + networkID, this);

            CPed ped = CPed.FromNetworkID(networkID);
            if (ped != null && ped.Exists())
            {
                ped.AttachBlip(sync: false);
            }
            else
            {
                if (this.messageCache.HasIDBeenCreatedRecently(networkID) && !this.messageCache.IsInQueue(message))
                {
                    this.messageCache.AddMessage(message);
                    Log.Debug("AttachBlip: Network ID invalid on client as yet, adding to queue", this);
                }
                else
                {
                    Log.Debug("AttachBlip: Invalid network ID", this);
                }
            }
        }

        public void RemoveBlip(NetworkServer sender, ReceivedUserMessage message)
        {
            int networkID = message.ReadInt32();

            Log.Debug("RemoveBlip: Asked to Remove blip for " + networkID, this);

            CPed ped = CPed.FromNetworkID(networkID);
            if (ped != null && ped.Exists())
            {
                ped.DeleteBlip(sync: false);
            }
            else
            {
                Log.Debug("RemoveBlip: Invalid network ID", this);
            }
        }


        public void AttachBlipVehicle(NetworkServer sender, ReceivedUserMessage message)
        {
            int networkID = message.ReadInt32();

            Log.Debug("AttachBlipVehicle: Asked to attach blip for " + networkID, this);

            CVehicle vehicle = CVehicle.FromNetworkID(networkID);
            if (vehicle != null && vehicle.Exists())
            {
                vehicle.AttachBlip(sync: false);
            }
            else
            {
                if (this.messageCache.HasIDBeenCreatedRecently(networkID) && !this.messageCache.IsInQueue(message))
                {
                    this.messageCache.AddMessage(message);
                    Log.Debug("AttachBlipVehicle: Network ID invalid on client as yet, adding to queue", this);
                }
                else
                {
                    Log.Debug("AttachBlipVehicle: Invalid network ID", this);
                }
            }
        }

        public void RemoveBlipVehicle(NetworkServer sender, ReceivedUserMessage message)
        {
            int networkID = message.ReadInt32();

            Log.Debug("RemoveBlipVehicle: Asked to Remove blip for " + networkID, this);

            CVehicle vehicle = CVehicle.FromNetworkID(networkID);
            if (vehicle != null && vehicle.Exists())
            {
                vehicle.DeleteBlip(sync: false);
            }
            else
            {
                Log.Debug("RemoveBlipVehicle: Invalid network ID", this);
            }
        }

        public void SetBlipModePed(NetworkServer sender, ReceivedUserMessage message)
        {
            int networkID = message.ReadInt32();
            int mode = message.ReadInt32();

            Log.Debug("SetBlipModePed: Asked to set mode for " + networkID, this);

            CPed ped = CPed.FromNetworkID(networkID);
            if (ped != null && ped.Exists())
            {
                if (ped.HasBlip)
                {
                    ped.Blip.Display = (BlipDisplay)mode;
                }
                else
                {
                    Log.Debug("SetBlipModePed: No blip", this);
                }
            }
            else
            {
                Log.Debug("SetBlipModePed: Invalid network ID", this);
            }
        }

        public void SetBlipModeVehicle(NetworkServer sender, ReceivedUserMessage message)
        {
            int networkID = message.ReadInt32();
            int mode = message.ReadInt32();

            Log.Debug("SetBlipModeVehicle: Asked to set mode for " + networkID, this);

            CVehicle vehicle = CVehicle.FromNetworkID(networkID);
            if (vehicle != null && vehicle.Exists())
            {
                if (vehicle.HasBlip)
                {
                    vehicle.Blip.Display = (BlipDisplay)mode;
                }
                else
                {
                    Log.Debug("SetBlipModeVehicle: No blip", this);
                }
            }
            else
            {
                Log.Debug("SetBlipModeVehicle: Invalid network ID", this);
            }
        }

        private void ClearCutscene(NetIncomingMessage message, NetConnection sender)
        {
            string name = message.ReadString();
            GTA.Native.Function.Call("CLEAR_NAMED_CUTSCENE", name);
            Game.FadeScreenIn(4000);
        }

        private void SetAsRequiredForMission(NetIncomingMessage message, NetConnection sender)
        {
            byte entityID = message.ReadByte();
            int networkID = message.ReadInt32();
            bool requiredForMission = message.ReadBoolean();

            if (((EEntityType)entityID) == EEntityType.Ped)
            {
                CPed ped = CPed.FromNetworkID(networkID);
                if (ped != null && ped.Exists())
                {
                    if (requiredForMission)
                    {
                        ped.BecomeMissionCharacter(sync: false);
                    }
                    else
                    {
                        ped.NoLongerNeeded(sync: false);
                    }
                }
            }
        }

        private void StartCutscene(NetIncomingMessage message, NetConnection sender)
        {
            // Todo: Create cutscene class (probably with cutscene name and audio names together)

            string name = message.ReadString();

            // Ensure mode is luis
            GTA.Model oldModel = GTA.Game.LocalPlayer.Model;
            GTA.value.PedSkin oldSkin = GTA.Game.LocalPlayer.Character.Skin;
            bool needModelSwitch = false;
            if (GTA.Game.LocalPlayer.Model != "PLAYER")
            {
                GTA.Game.LocalPlayer.Model = "PLAYER";
                needModelSwitch = true;
            }

            GTA.Native.Function.Call("LOAD_ALL_OBJECTS_NOW");
            GTA.Native.Function.Call("START_CUTSCENE_NOW", name);
            while (!GTA.Native.Function.Call<bool>("HAS_CUTSCENE_LOADED"))
            {
                Game.WaitInCurrentScript(0);
                Game.Console.Print("loading");
                //WAIT(0);
            }
            Log.Debug("Loaded", this);
            while (!GTA.Native.Function.Call<bool>("HAS_CUTSCENE_FINISHED"))
            {
                Game.Console.Print("playing");
                Game.WaitInCurrentScript(0);
                //WAIT(0);
            }
            if (needModelSwitch)
            {
                GTA.Game.LocalPlayer.Model = oldModel;
                GTA.Game.LocalPlayer.Skin.Template = oldSkin;
            }
            Log.Debug("Finished", this);
            //GTA.Native.Function.Call("CLEAR_NAMED_CUTSCENE", name);
            Game.FadeScreenIn(4000);
            Log.Debug("Cleaned", this);
        }

        private void StartCutsceneLoadText(NetIncomingMessage message, NetConnection sender)
        {
            string name = message.ReadString();
            int id = message.ReadByte(); // So the native gets a int rather than a byte

            GTA.Native.Function.Call("LOAD_ADDITIONAL_TEXT", name, id);
        }
    }
}
