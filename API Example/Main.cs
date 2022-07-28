namespace API_Example
{
    using System;
    using System.Windows.Forms;

    using API_Example.Callouts;
    using API_Example.World_Events;

    using GTA;

    using LCPDFR.Networking;
    using LCPDFR.Networking.User;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Input;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.API;

    using SlimDX.XInput;

    enum ENetworkTestMessages
    {
        NeedBackup,
    }

    /// <summary>
    /// Sample plugin making use of the LCPDFR API. In the attribute below you can specify the name of the plugin.
    /// </summary>
    [PluginInfo("TestPlugin", false, true)]
    public class TestPlugin : Plugin
    {
        /// <summary>
        /// A LCPDFR ped.
        /// </summary>
        private LPed lcpdfrPed;

        /// <summary>
        /// Called when the plugin has been created successfully.
        /// </summary>
        public override void Initialize()
        {
            // Bind console commands
            this.RegisterConsoleCommands();

            // Listen for on duty event
            Functions.OnOnDutyStateChanged += this.Functions_OnOnDutyStateChanged;
            Networking.JoinedNetworkGame += this.Networking_JoinedNetworkGame;

            Log.Info("Started", this);
        }

        /// <summary>
        /// Called when player changed the on duty state.
        /// </summary>
        /// <param name="onDuty">The new on duty state.</param>
        public void Functions_OnOnDutyStateChanged(bool onDuty)
        {
            if (onDuty)
            {
                // Register callouts to LCPDFR
                Functions.RegisterCallout(typeof(Shooting));
                Functions.RegisterCallout(typeof(Pursuit));

                Functions.AddWorldEvent(typeof(Brawl), "Brawl");
            }
        }

        /// <summary>
        /// Called every tick to process all plugin logic.
        /// </summary>
        public override void Process()
        {
            if (Functions.IsKeyDown(Keys.B))
            {
                //foreach (var ped in Functions.GetArrestedPeds())
                //{
                //    ped.Die();
                //}

                LHandle callout = Functions.StartCallout("Pursuit");
                if (callout != null)
                {
                    string name = Functions.GetCalloutName(callout);
                    GTA.Game.DisplayText(name);

                    LVehicle a = null;
                    a.SirenMuted = true;
                }

                if (!Networking.IsInSession || !Networking.IsConnected)
                {
                    return;
                }

                if (Networking.IsHost)
                {
                    Log.Debug("About to send message", this);

                    Vector3 position = LPlayer.LocalPlayer.Ped.Position;

                    // Tell client we need backup.
                    DynamicData dynamicData = new DynamicData(Networking.GetServerInstance());
                    dynamicData.Write(position.X);
                    dynamicData.Write(position.Y);
                    dynamicData.Write(position.Z);
                    Networking.GetServerInstance().Send("API_Example", ENetworkTestMessages.NeedBackup, dynamicData);

                    Log.Debug("Message sent", this);
                }
            }

            if (Functions.IsKeyDown(Keys.N))
            {


                //if (!Networking.IsInSession || !Networking.IsConnected)
                //{
                //    return;
                //}


                //if (!Networking.IsHost)
                //{
                //    // Register callback.
                //    Networking.GetClientInstance().AddUserDataHandler("API_Example", ENetworkTestMessages.NeedBackup, this.NeedBackupHandlerFunction);
                //    Log.Debug("Client callback registered", this);
                //}
            }

            // If on duty and Z is down
            //if (LPlayer.LocalPlayer.IsOnDuty && (Functions.IsKeyDown(Keys.Z) || (Functions.IsControllerInUse() && Functions.IsControllerKeyDown(GamepadButtonFlags.DPadRight))))
            //{
            //    DelayedCaller.Call(
            //        delegate
            //        {
            //            LPlayer.LocalPlayer.Ped.DrawTextAboveHead("Test", 500);
            //        }, 
            //        this, 
            //        500);

            //    if (this.lcpdfrPed == null || this.lcpdfrPed.Exists() || this.lcpdfrPed.IsAliveAndWell)
            //    {
            //        // Create a ped
            //        this.lcpdfrPed = new LPed(LPlayer.LocalPlayer.Ped.Position, "F_Y_HOOKER_01");
            //        this.lcpdfrPed.NoLongerNeeded();
            //        this.lcpdfrPed.AttachBlip();
            //        this.lcpdfrPed.ItemsCarried = LPed.EPedItem.Drugs;
            //        LPed.EPedItem item = this.lcpdfrPed.ItemsCarried;
            //        this.lcpdfrPed.PersonaData = new PersonaData(DateTime.Now, 0, "Sam", "T", false, 1337, true);
            //    }
            //}

            //// If our ped exists and has been arrested, kill it
            //if (this.lcpdfrPed != null && this.lcpdfrPed.Exists())
            //{
            //    if (this.lcpdfrPed.HasBeenArrested && this.lcpdfrPed.IsAliveAndWell)
            //    {
            //        this.lcpdfrPed.Die();
            //    }
            //}

            //if (Functions.IsKeyDown(Keys.B))
            //{
            //    if (Functions.IsPlayerPerformingPullover())
            //    {
            //        LHandle pullover = Functions.GetCurrentPullover();
            //        if (pullover != null)
            //        {
            //            LVehicle vehicle = Functions.GetPulloverVehicle(pullover);
            //            if (vehicle != null && vehicle.Exists())
            //            {
            //                vehicle.AttachBlip().Color = BlipColor.Cyan;
            //                if (vehicle.HasDriver)
            //                {
            //                    // Change name of driver to Sam T.
            //                    LPed driver = vehicle.GetPedOnSeat(VehicleSeat.Driver);
            //                    if (driver != null && driver.Exists())
            //                    {
            //                        // Modify name.
            //                        driver.PersonaData = new PersonaData(DateTime.Now, 0, "Sam", "T", true, 0, false);

            //                        string name = driver.PersonaData.FullName;
            //                        Functions.PrintText("--- Pulling over: " + name + " ---", 10000);
                                            
            //                        // Looking up the driver will make the vehicle explode.
            //                        Functions.PedLookedUpInPoliceComputer += delegate(PersonaData data)
            //                            {
            //                                if (data.FullName == name)
            //                                {
            //                                    DelayedCaller.Call(delegate { if (vehicle.Exists()) { vehicle.Explode(); } }, this, Common.GetRandomValue(5000, 10000));
            //                                }
            //                            };
            //                    }
            //                }
            //            }
            //        }
            //    }
            //    else
            //    {
            //        // Disable pullovers for vehicle in front.
            //        GTA.Vehicle vehicle = World.GetClosestVehicle(LPlayer.LocalPlayer.Ped.GetOffsetPosition(new Vector3(0, 10, 0)), 5f);
            //        if (vehicle != null && vehicle.Exists())
            //        {
            //            LVehicle veh = LVehicle.FromGTAVehicle(vehicle);
            //            if (veh != null)
            //            {
            //                veh.DisablePullover = true;
            //                veh.AttachBlip();
            //            }
            //        }
            //    }
            //}

            //// Kill all partners.
            //if (Functions.IsKeyDown(Keys.N))
            //{
            //    LHandle partnerManger = Functions.GetCurrentPartner();
            //    LPed[] peds = Functions.GetPartnerPeds(partnerManger);
            //    if (peds != null)
            //    {
            //        foreach (LPed partner in peds)
            //        {
            //            if (partner.Exists())
            //            {
            //                partner.Die();
            //            }
            //        }
            //    }
            //}
        }

        private void NeedBackupHandlerFunction(NetworkServer sender, ReceivedUserMessage message)
        {
            float x = 0, y = 0, z = 0;
            x = message.ReadFloat();
            y = message.ReadFloat();
            z = message.ReadFloat();

            Vector3 position = new Vector3(x, y, z);
            string area = Functions.GetAreaStringFromPosition(position);

            Functions.PrintText(string.Format("Officer {0} requests backup at {1}", sender.SafeName, area), 5000);
            Functions.CreateBlipForArea(position, 25f);
        }

        /// <summary>
        /// Called when the plugin is being disposed, e.g. because an unhandled exception occured in Process. Free all resources here!
        /// </summary>
        public override void Finally()
        {
        }

        private void Networking_JoinedNetworkGame()
        {
            GTA.Game.DisplayText("IN NETWORK GAME");
        }

        [ConsoleCommand("StartCallout", false)]
        private void StartCallout(ParameterCollection parameterCollection)
        {
            if (parameterCollection.Count > 0)
            {
                string name = parameterCollection[0];
                Functions.StartCallout(name);
            }
            else
            {
                Game.Console.Print("StartCallout: No argument given.");
            }
        }
    }
}