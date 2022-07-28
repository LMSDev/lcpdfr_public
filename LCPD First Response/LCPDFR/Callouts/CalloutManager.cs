namespace LCPD_First_Response.LCPDFR.Callouts
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;

    using global::LCPDFR.Networking;
    using global::LCPDFR.Networking.User;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.GUI;
    using LCPD_First_Response.Engine.Input;
    using LCPD_First_Response.Engine.Networking;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.API;
    using LCPD_First_Response.LCPDFR.Input;
    using LCPD_First_Response.LCPDFR.Scripts;

    using Main = LCPD_First_Response.LCPDFR.Main;

    enum ECalloutNetworkMessages
    {
        CalloutAccepted,
        CalloutEnded,
        CalloutInstruction,
    }

    /// <summary>
    /// Manages the creation and execution of callouts.
    /// </summary>
    [ScriptInfo("CalloutManager", false)]
    internal class CalloutManager : BaseScript
    {
        /// <summary>
        /// The busy image texture.
        /// </summary>
        private Texture busyTexture;

        /// <summary>
        /// Current callout.
        /// </summary>
        private Callout currentCallout;

        /// <summary>
        /// Callouts registered.
        /// </summary>
        private List<Type> registeredCallouts;

        /// <summary>
        /// The time the last callout has finished.
        /// </summary>
        private DateTime timeLastCalloutFinished;

        /// <summary>
        /// Initializes a new instance of the <see cref="CalloutManager"/> class.
        /// </summary>
        public CalloutManager()
        {
            // Look for callouts in this assebmly
            this.registeredCallouts = AssemblyHelper.GetTypesInheritingFromType(typeof(Callout)).ToList();
            this.AllowRandomCallouts = Settings.CalloutsEnabled;
            this.timeLastCalloutFinished = DateTime.Now;
            Gui.PerFrameDrawing += this.Gui_PerFrameDrawing;
            byte[] data = ResourceHelper.GetResourceBytes("Phone_icon_disconnect.png", typeof(Main));
            this.busyTexture = new Texture(data);

            // Networking stuff.
            if (Engine.Main.NetworkManager.IsNetworkSession)
            {
                // Register callbacks.
                if (!Engine.Main.NetworkManager.IsHost)
                {
                    Engine.Main.NetworkManager.Client.AddUserDataHandler(
                        "Callouts", 
                        ECalloutNetworkMessages.CalloutAccepted,
                        delegate(NetworkServer sender, ReceivedUserMessage message)
                        {
                            string calloutMessage = message.ReadString();
                            Vector3 position = message.ReadVector3();
                            float radius = message.ReadFloat();

                            Main.TextWall.AddText("[" + sender.SafeName + "] " + "I could need some help: " + calloutMessage);

                            if (position != Vector3.Zero)
                            {
                                 // Show area blip for a short time.
                                Blip blip = AreaBlocker.CreateAreaBlip(position, radius);
                                if (blip != null && blip.Exists())
                                {
                                    blip.SetColorRGB(Color.WhiteSmoke);

                                    Blip blip2 = Blip.AddBlip(position);
                                    if (blip2 != null && blip2.Exists())
                                    {
                                        blip2.SetColorRGB(Color.WhiteSmoke);
                                    }

                                    DelayedCaller.Call(
                                    delegate
                                    {
                                        if (blip.Exists())
                                        {
                                            blip.Delete();
                                        }

                                        if (blip2 != null && blip2.Exists())
                                        {
                                            blip2.Delete();
                                        }
                                    }, 
                                    this, 
                                    10000);
                                }
                            }
                        });

                    Engine.Main.NetworkManager.Client.AddUserDataHandler(
                        "Callouts", 
                        ECalloutNetworkMessages.CalloutEnded,
                        delegate(NetworkServer sender, ReceivedUserMessage message)
                        {
                            Main.TextWall.AddText("[" + sender.SafeName + "] " + "We're done here!");
                        });

                    Engine.Main.NetworkManager.Client.AddUserDataHandler(
                        "Callouts", 
                        ECalloutNetworkMessages.CalloutInstruction,
                        delegate(NetworkServer sender, ReceivedUserMessage message)
                        {
                            string calloutMessage = message.ReadString();
                            int duration = message.ReadInt32();
                            Functions.PrintText(calloutMessage, duration);
                        });
                }
            }
            
            Log.Debug("Initialized", this);
        }

        /// <summary>
        /// Invoked when the user a accepted a callout.
        /// </summary>
        public event EventHandler<string> CalloutHasBeenAccepted;

        /// <summary>
        /// Gets or sets a value indicating whether random callouts are allowed.
        /// </summary>
        public bool AllowRandomCallouts { get; set; }

        /// <summary>
        /// Gets the current callout.
        /// </summary>
        public Callout CurrentCallout
        {
            get
            {
                return this.currentCallout;
            }
        }

        /// <summary>
        /// Gets a value indicating whether a callout is running.
        /// </summary>
        public bool IsCalloutRunning
        {
            get
            {
                return this.currentCallout != null;
            }
        }

        /// <summary>
        /// Called every tick to process all script logic.
        /// </summary>
        public override void Process()
        {
            if (this.AllowRandomCallouts && !this.IsCalloutRunning)
            {
                if (!CPlayer<LCPDFRPlayer>.LocalPlayer.IsInPoliceDepartment && !CPlayer<LCPDFRPlayer>.LocalPlayer.IsBusy && !LCPDFRPlayer.LocalPlayer.IsBusyForCallouts
                    && LCPDFRPlayer.LocalPlayer.AvailabilityState == EPlayerAvailabilityState.Idle)
                {
                    // Check if time the last callout has finished and the minimum time to wait before a new is before the current date, so a callout could start
                    if (this.timeLastCalloutFinished.AddSeconds(Settings.CalloutMinimumSecondsBeforeNewCallout) < DateTime.Now)
                    {
                        // Increase chance by 2 of a crime beeing committed at night
                        int nightMultiplier = 1;

                        // At night, twice the chance
                        if (Globals.IsNightTime)
                        {
                            nightMultiplier = 2;
                        }

                        // Use multiplier to get the chance
                        int divisor = Common.EnsureValueIsNotZero<int>(Settings.CalloutsMultiplier);

                        // Calculate chance
                        bool createCallout = Common.GetRandomValue(0, (1000000 / nightMultiplier) / divisor) == 1;

                        // If time has exceeded, force callout
                        if (this.timeLastCalloutFinished.AddSeconds(Settings.CalloutMaximumSecondsBeforeNewCallout) < DateTime.Now)
                        {
                            createCallout = true;
                        }

                        if (createCallout)
                        {
                            this.StartCallout();
                        }
                    }
                }
            }

            // If there is a pending callout, check keys
            if (this.currentCallout != null)
            {
                if (this.currentCallout.AcceptanceState == ECalloutAcceptanceState.Pending)
                {
                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.AcceptCallout))
                    {
                        AcceptCallout();
                        return;
                    }

                    if (KeyHandler.IsKeyDown(ELCPDFRKeys.DenyCallout))
                    {
                        this.currentCallout.OnCalloutNotAccepted();
                        this.currentCallout = null;

                        // Prevent a new callout from being created immediately, so we reset the time
                        this.timeLastCalloutFinished = DateTime.Now;
                        Main.TextWall.AddText("[" + CultureHelper.GetText("POLICE_SCANNER_OFFICER") + " " + CPlayer.LocalPlayer.GetOfficerName() + "] " + CultureHelper.GetText("CALLOUT_IGNORED"));
                        Stats.UpdateStat(Stats.EStatType.CalloutDenied, 1);
                        Main.TextWall.DontFade = false;
                        return;
                    }
                }

                // Process running callout.
                Callout callout = this.currentCallout;
                if (callout.AcceptanceState == ECalloutAcceptanceState.Running)
                {
                    try
                    {
                        callout.Process();
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error while processing callout: " + callout.ScriptInfo.Name + ": " + ex.Message + ex.StackTrace, this);
                        ExceptionHandler.ExceptionCaught(callout, ex);
                        this.EndCurrentCallout();
                    }
                }
            }
        }

        /// <summary>
        /// Registers the callout to the manager. Useful when the callout hasn't been detected by the manager automatically.
        /// </summary>
        /// <param name="type">The type of the callout.</param>
        public void RegisterCallout(Type type)
        {
            this.registeredCallouts.Add(type);
        }

        /// <summary>
        /// Starts the callout.
        /// </summary>
        /// <param name="calloutName">
        /// The callout Name.
        /// </param>
        /// <returns>
        /// The callout.
        /// </returns>
        public Callout StartCallout(string calloutName = "")
        {
            if (this.currentCallout != null)
            {
                Log.Warning("StartCallout: There's already a callout running. Aborting...", this);
                this.EndCurrentCallout();
            }

            // Look for callout. If not found, use random one
            Type registeredCallout = this.GetRegisteredCalloutByName(calloutName) ?? this.GetRandomRegisteredCallout();

            Callout callout = Activator.CreateInstance(registeredCallout) as Callout;
            this.CalloutCreated(callout);
            return callout;
        }

        /// <summary>
        /// Starts the callout using an already created callout instance.
        /// </summary>
        /// <param name="callout">The callout instance.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="callout"/> is null.</exception>
        public void StartCallout(Callout callout)
        {           
            if (callout == null)
            {
                throw new ArgumentNullException("callout");
            }

            if (this.currentCallout != null)
            {
                Log.Warning("StartCallout: There's already a callout running. Aborting...", this);
                this.EndCurrentCallout();
            }

            this.CalloutCreated(callout);
        }

        /// <summary>
        /// Stops the current callout.
        /// </summary>
        public void StopCallout()
        {
            this.EndCurrentCallout();
        }

        /// <summary>
        /// Put all resource free logic here. This is either called by the engine to shutdown the script or can be called by the script itself to execute the cleanup code.
        /// Call base when overriding.
        /// </summary>
        public override void End()
        {
            base.End();

            Gui.PerFrameDrawing -= this.Gui_PerFrameDrawing;

            this.StopCallout();
        }

        /// <summary>
        /// Accepts the currently pending callout.
        /// </summary>
        public void AcceptCallout()
        {
            this.currentCallout.OnEnd += new OnEndEventHandler(this.currentCallout_OnEnd);
            Main.TextWall.AddText("[" + CultureHelper.GetText("POLICE_SCANNER_OFFICER") + " " + CPlayer.LocalPlayer.GetOfficerName() + "] " + CultureHelper.GetText("CALLOUT_ACCEPTED"));

            if (!this.currentCallout.IsNotWithinMinimumDistance() || !this.currentCallout.OnCalloutAccepted())
            {
                this.EndCurrentCallout();

                DelayedCaller.Call(
                    delegate
                    {
                        Main.TextWall.AddText("[" + CultureHelper.GetText("POLICE_SCANNER_CONTROL") + "] " + CultureHelper.GetText("CALLOUT_DISREGARD"));
                    },
                    this,
                    1500);
            }

            EventHandler<string> handler = this.CalloutHasBeenAccepted;
            if (handler != null)
            {
                string name = this.currentCallout.ScriptInfo.Name;
                handler(this.currentCallout, name);
            }

            Stats.UpdateStat(Stats.EStatType.CalloutAccepted, 1);
            Main.TextWall.DontFade = false;

            // Let network players know.
            if (Engine.Main.NetworkManager.IsNetworkSession && Engine.Main.NetworkManager.IsHost && Engine.Main.NetworkManager.CanSendData)
            {
                DynamicData dynamicData = new DynamicData(Engine.Main.NetworkManager.Server);
                dynamicData.Write(this.currentCallout.CalloutMessage);
                dynamicData.Write(this.currentCallout.AreaBlipPosition);
                dynamicData.Write(this.currentCallout.AreaBlipRadius);
                Engine.Main.NetworkManager.ActivePeer.Send("Callouts", ECalloutNetworkMessages.CalloutAccepted, dynamicData);
            }
        }

        /// <summary>
        /// Called when the current callout has ended.
        /// </summary>
        /// <param name="sender">The callout.</param>
        private void currentCallout_OnEnd(object sender)
        {
            this.EndCurrentCallout();
        }

        /// <summary>
        /// Setups <paramref name="callout"/> and sets it as current callout.
        /// </summary>
        /// <param name="callout">The callout.</param>
        private void CalloutCreated(Callout callout)
        {
            // Set as current callout and add to script manager
            this.currentCallout = callout;
            LCPDFR.Main.ScriptManager.RegisterScriptInstance(callout);
            
            // Check if callout can be displayed
            if (!this.currentCallout.OnBeforeCalloutDisplayed())
            {
                Log.Debug("CalloutCreated: Callout should be aborted", this);
                this.EndCurrentCallout();
                return;
            }

            // Present callout to user
            Main.TextWall.AddText("[" + CultureHelper.GetText("POLICE_SCANNER_CONTROL") + "] " + this.currentCallout.CalloutMessage);
            Main.TextWall.DontFade = true;
            this.currentCallout.OnCalloutDisplayed();

            // Add timer to abort callout if user didn't respond for some time
            DelayedCaller.Call(this.CalloutTimedOut, Common.GetRandomValue(8000, 20000));
        }

        /// <summary>
        /// Called when the callout request has timed out.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        private void CalloutTimedOut(object[] parameter)
        {
            // If there's still a callout pending
            if (this.currentCallout != null && this.currentCallout.AcceptanceState == ECalloutAcceptanceState.Pending)
            {
                this.currentCallout.OnCalloutNotAccepted();
                this.currentCallout = null;

                // Prevent a new callout from being created immediately, so we reset the time
                this.timeLastCalloutFinished = DateTime.Now;
                Main.TextWall.AddText("[" + CultureHelper.GetText("POLICE_SCANNER_CONTROL") + "] " + CultureHelper.GetText("CALLOUT_TIMEOUT"));
                Main.TextWall.DontFade = false;
            }
        }

        /// <summary>
        /// Ends the currently running callout. Does nothing if no callout running.
        /// </summary>
        private void EndCurrentCallout()
        {
            if (this.currentCallout == null)
            {
                Log.Warning("EndCurrentCallout: No callout running", this);
                return;
            }

            // Deregister event.
            this.currentCallout.OnEnd -= new OnEndEventHandler(this.currentCallout_OnEnd);

            // Call Callout.End if not ended already.
            if (this.currentCallout.AcceptanceState != ECalloutAcceptanceState.Ended)
            {
                Callout callout = this.currentCallout;
                Log.Debug("EndCurrentCallout: Finishing...", this);

                try
                {
                    callout.End();
                }
                catch (Exception ex)
                {
                    Log.Warning("Failed to free callout properly: " + callout.ScriptInfo.Name + ex.Message + ex.StackTrace, this);
                }
            }

            // Reset state.
            this.timeLastCalloutFinished = DateTime.Now;
            this.currentCallout = null;

            // Let network players know.
            if (Engine.Main.NetworkManager.IsNetworkSession && Engine.Main.NetworkManager.IsHost && Engine.Main.NetworkManager.CanSendData)
            {
                DynamicData dynamicData = new DynamicData(Engine.Main.NetworkManager.Server);
                Engine.Main.NetworkManager.ActivePeer.Send("Callouts", ECalloutNetworkMessages.CalloutEnded, dynamicData);
            }
        }

        /// <summary>
        /// Gets a random callout name.
        /// </summary>
        /// <returns>Random name.</returns>
        private string GetRandomCalloutName()
        {
            int randomNumber = Common.GetRandomValue(0, this.registeredCallouts.Count);
            CalloutInfoAttribute attribute = AssemblyHelper.GetAttribute<CalloutInfoAttribute>(this.registeredCallouts[randomNumber]);
            return attribute.Name;
        }
        
        /// <summary>
        /// Returns a random registered callout.
        /// </summary>
        /// <returns>The callout.</returns>
        private Type GetRandomRegisteredCallout()
        {
            // Get random name first
            string calloutName = this.GetRandomCalloutName();

            foreach (Type registeredCallout in this.registeredCallouts)
            {
                CalloutInfoAttribute calloutInfoAttribute = AssemblyHelper.GetAttribute<CalloutInfoAttribute>(registeredCallout);

                if (calloutInfoAttribute.Name.ToLower() == calloutName.ToLower())
                {
                    // Create callout based on chance
                    switch (calloutInfoAttribute.CalloutProbability)
                    {
                        case ECalloutProbability.VeryHigh:
                            if (Common.GetRandomValue(0, 100) > 90)
                            {
                                Log.Debug("GetRandomRegisteredCallout: Probability is ECalloutProbability.VeryHigh, skipped.", this);
                                return this.GetRandomRegisteredCallout();
                            }

                            break;

                        case ECalloutProbability.High:
                            if (Common.GetRandomValue(0, 100) > 75)
                            {
                                Log.Debug("GetRandomRegisteredCallout: Probability is ECalloutProbability.High, skipped.", this);
                                return this.GetRandomRegisteredCallout();
                            }

                            break;

                        case ECalloutProbability.Medium:
                            if (Common.GetRandomValue(0, 100) > 50)
                            {
                                Log.Debug("GetRandomRegisteredCallout: Probability is ECalloutProbability.Medium, skipped.", this);
                                return this.GetRandomRegisteredCallout();
                            }

                            break;

                        case ECalloutProbability.Low:
                            if (Common.GetRandomValue(0, 100) > 25)
                            {
                                Log.Debug("GetRandomRegisteredCallout: Probability is ECalloutProbability.Low, skipped.", this);
                                return this.GetRandomRegisteredCallout();
                            }

                            break;

                        case ECalloutProbability.VeryLow:
                            if (Common.GetRandomValue(0, 100) > 10)
                            {
                                Log.Debug("GetRandomRegisteredCallout: Probability is ECalloutProbability.VeryLow, skipped.", this);
                                return this.GetRandomRegisteredCallout();
                            }

                            break;

                        case ECalloutProbability.Never:
                            Log.Debug("GetRandomRegisteredCallout: Probability is ECalloutProbability.Never, skipped.", this);
                            return this.GetRandomRegisteredCallout();

                    }

                    return registeredCallout;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns a registered callout with <paramref name="calloutName"/> as name. Returns null if not found.
        /// </summary>
        /// <param name="calloutName">The name of the callout.</param>
        /// <returns>The callout.</returns>
        private Type GetRegisteredCalloutByName(string calloutName)
        {
            // If no name is given, return ull
            if (calloutName == string.Empty)
            {
                Log.Debug("GetRegisteredCalloutByName: No name specified", this);
                return null;
            }

            foreach (Type registeredCallout in this.registeredCallouts)
            {
                CalloutInfoAttribute calloutInfoAttribute = AssemblyHelper.GetAttribute<CalloutInfoAttribute>(registeredCallout);
                if (calloutInfoAttribute.Name.ToLower() == calloutName.ToLower())
                {
                    return registeredCallout;
                }
            }

            return null;
        }

        /// <summary>
        /// Used to draw the busy icon on screen.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void Gui_PerFrameDrawing(object sender, GraphicsEventArgs e)
        {
            if (LCPDFRPlayer.LocalPlayer.AvailabilityState == EPlayerAvailabilityState.InCalloutFinished)
            {
                float height = Game.Resolution.Height;
                e.Graphics.DrawSprite(this.busyTexture, 50, height - 60, 64, 32, 0);
            }
        }


        /// <summary>
        /// Benchmarks the random callout selection with <paramref name="iterations"/> and logs it.
        /// </summary>
        /// <param name="iterations">The number of iterations.</param>
        /// <param name="onlyLogFinalResults">Whether only the final results should be logged and not every single decision.</param>
        private void BenchmarkCalloutSelection(int iterations, bool onlyLogFinalResults)
        {
            Log.Info("-------------------------------------------------------------------------", this);
            Log.Info("BenchmarkCalloutSelection: Starting benchmark of the random callout selection performing " + iterations + " iterations. This might take a while.", this);

            // Populate dictionary with all callouts
            Dictionary<Type, int> allCallouts = new Dictionary<Type, int>();
            foreach (Type registeredCallout in this.registeredCallouts)
            {
                allCallouts.Add(registeredCallout, 0);
            }

            // Benchmark callouts
            for (int i = 0; i < iterations; i++)
            {
                Type t = this.GetRandomRegisteredCallout();

                // Increment counter for the callout
                allCallouts[t]++;

                if (!onlyLogFinalResults)
                {
                    Log.Info("Selected " + AssemblyHelper.GetAttribute<CalloutInfoAttribute>(t).Name, this);
                }
            }

            // Sort results by number
            List<KeyValuePair<Type, int>> list = allCallouts.ToList();
            list.Sort((firstPair, nextPair) => firstPair.Value.CompareTo(nextPair.Value));
            list.Reverse();

            // Log final results
            Log.Info("Results of the benchmark with " + iterations + " iterations: ", this);

            int rank = 1;
            foreach (KeyValuePair<Type, int> keyValuePair in list)
            {
                Log.Info(rank + ". " + AssemblyHelper.GetAttribute<CalloutInfoAttribute>(keyValuePair.Key).Name + ": " + keyValuePair.Value, this);
                rank++;
            }
        }

        /// <summary>
        /// Console command for performing a callout benchmark.
        /// </summary>
        /// <param name="parameterCollection">The parameter.</param>
        [ConsoleCommand("CalloutBenchmark", "Peforms a benchmark of the random callout selection. Uses 1000 as default number. First parameter is number of iterations. When second paramter is ''detailed'', every single decision is logged.", false)]
        private void BenchmarkCalloutSelectionCommand(ParameterCollection parameterCollection)
        {
            int iterations = 1000;
            bool detailedMode = false;

            if (parameterCollection.Count > 0)
            {
                if (!int.TryParse(parameterCollection[0], out iterations))
                {
                    Log.Warning("BenchmarkCalloutSelectionCommand: Invalid number", this);
                    return;
                }
            }

            if (parameterCollection.Count > 1)
            {
                if (parameterCollection[1].ToLower() == "detailed")
                {
                    detailedMode = true;
                }
            }

            BaseScript[] scripts = Main.ScriptManager.GetRunningScriptInstances("CalloutManager");
            foreach (BaseScript baseScript in scripts)
            {
                CalloutManager calloutManager = baseScript as CalloutManager;
                calloutManager.BenchmarkCalloutSelection(iterations, !detailedMode);
            }
        }
    }
}