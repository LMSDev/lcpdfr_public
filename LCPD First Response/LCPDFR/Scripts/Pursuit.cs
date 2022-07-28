namespace LCPD_First_Response.LCPDFR.Scripts
{
    using System;
    using System.Collections.Generic;

    using global::LCPDFR.Networking;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.GUI;
    using LCPD_First_Response.LCPDFR.Input;

    /// <summary>
    /// Handles a pursuit of one more more enemies. Uses Chase as base class, but provides more options, such as managing blips.
    /// </summary>
    internal class Pursuit : Chase
    {
        /// <summary>
        /// The distance for the player to be able to join AI controlled chases.
        /// </summary>
        private const float DistanceToAllowPlayerJoining = 50f;

        private const float TimeAfterTrafficDensityIsSetToLow = 120;

        /// <summary>
        /// Blips created by the pursuit.
        /// </summary>
        private Dictionary<CPed, Blip> blips;

        /// <summary>
        /// Whether the help box that player can join the chase has been displayed.
        /// </summary>
        private bool displayedPlayerCanJoinHelpbox;

        /// <summary>
        /// Whether cop blips should be enabled or not.
        /// </summary>
        private bool dontEnableCopBlips;

        /// <summary>
        /// The start time of the pursuit.
        /// </summary>
        private DateTime startTime;

        /// <summary>
        /// The timer to reduce ped and vehicle density.
        /// </summary>
        private NonAutomaticTimer trafficChangeTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="Pursuit"/> class.
        /// </summary>
        public Pursuit() : base(true, true)
        {
            this.blips = new Dictionary<CPed, Blip>();
            this.MaxCars = Settings.MaximumCopCarsInPursuit;
            this.MaxUnits = Settings.MaximumCopsInPursuit;
            this.startTime = DateTime.Now;

            // Limit randomly created cops and traffic a little.
            PopulationManager.SetRandomCopsDensity(0.5f);
            PopulationManager.SetRandomCarDensity(0.85f);

            this.trafficChangeTimer = new NonAutomaticTimer(4000);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the player can join an AI controlled chase.
        /// </summary>
        public bool CanPlayerJoin { get; set; }

        /// <summary>
        /// Whether or not the suspect blip should show the route to get there
        /// </summary>
        public bool routeActive = true;

        /// <summary>
        /// Gets or sets a value indicating whether cop blips should be enabled.
        /// </summary>
        public bool DontEnableCopBlips
        {
            get
            {
                return this.dontEnableCopBlips;
            }

            set
            {
                this.dontEnableCopBlips = value;
                PoliceScanner.CopBlipsActive = !value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether blips should flash when being added.
        /// </summary>
        public bool FlashBlipsWhenAdding { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the pursuit has been called in.
        /// </summary>
        public bool HasBeenCalledIn { get; set; }

        /// <summary>
        /// Creates a new chase for <paramref name="ped"/> or uses an already ongoing one, if any.
        /// Always prefers the current player's chase.
        /// </summary>
        /// <param name="ped">The suspect.</param>
        /// <param name="disableAI">Whether the AI of the suspect should be disabled.</param>
        /// <returns>The chase instance.</returns>
        public static Chase RegisterSuspect(CPed ped, bool disableAI)
        {
            Chase currentChase = CPlayer.LocalPlayer.Ped.PedData.CurrentChase;
            if (currentChase == null && Chase.AllChases != null)
            {
                foreach (Chase chase in Chase.AllChases)
                {
                    if (chase.IsRunning)
                    {
                        currentChase = chase;

                        // If is of type pursuit, chances are this is a high level chase at user input level and as such should be prefered
                        if (currentChase is Pursuit)
                        {
                            break;
                        }
                    }
                }
            }

            // Create chase if necessary
            if (currentChase == null)
            {
                currentChase = new Pursuit();
                currentChase.AllowSuspectVehicles = false;
                currentChase.AllowSuspectWeapons = false;
            }

            // Add ped to chase
            ped.PedData.DisableChaseAI = disableAI;
            currentChase.AddTarget(ped);

            // If player has no chase yet, set as active
            if (CPlayer.LocalPlayer.Ped.PedData.CurrentChase == null)
            {
                if (currentChase is Pursuit)
                {
                    (currentChase as Pursuit).SetAsCurrentPlayerChase();

                    AudioHelper.EPursuitCallInReason reason = AudioHelper.EPursuitCallInReason.Pursuit;

                    // Before we call it in, we need to find out what the suspect is actually doing so the appropriate audio can be used
                    if (ped.IsInCombat && ped.IsNotInCombatOrOnlyFleeing)
                    {
                        // OH SHIT SON, YOU REALLY GOTTA RUN?!
                        reason = AudioHelper.EPursuitCallInReason.FootPursuit;
                    }
                    else if (CPlayer.LocalPlayer.Ped.HasBeenDamagedBy(ped) && ped.Intelligence.TaskManager.IsInternalTaskActive(Engine.Scripting.Tasks.EInternalTaskID.CTaskComplexGun))
                    {
                        // SHOTS FIRED!
                        reason = AudioHelper.EPursuitCallInReason.ShotAt;
                    }
                    else if (ped.Intelligence.TaskManager.IsInternalTaskActive(Engine.Scripting.Tasks.EInternalTaskID.CTaskComplexGun))
                    {
                        // DISPATCH, THE SUSPECT IS ARMED!
                        reason = AudioHelper.EPursuitCallInReason.Shooting;
                    }
                    else if (CPlayer.LocalPlayer.Ped.HasBeenDamagedBy(ped))
                    {
                        // I'LL DROP YOU LIKE SKINNY BITCHES DROP DIET PILLS, YOU HEAR?!
                        reason = AudioHelper.EPursuitCallInReason.Assaulted;
                    }
                    else if (ped.IsInCombat)
                    {
                        // COME ON NOW, LOOK AT ME!  I EAT PUNKS LIKE YOU FOR BREAKFAST
                        reason = AudioHelper.EPursuitCallInReason.Backup;
                    }
                    else
                    {
                        // Poor, innocent, defenceless victim of an evil player
                        reason = AudioHelper.EPursuitCallInReason.Other;
                    }

                    //Game.Console.Print(reason.ToString());

                    (currentChase as Pursuit).CallIn(reason);
                }
            }

            return currentChase;
        }

        /// <summary>
        /// Processes the chase logic.
        /// </summary>
        public override void Process()
        {
            bool weaponsUsed = false;

            if (this.HasBeenCalledIn)
            {
                // Random chance to deploy roadblocks
                if (Settings.AllowRoadblocks && Common.GetRandomBool(0, 1000, 1))
                {
                    this.DeployRoadblockForClosestSuspect();
                }
            }
            if (this.IsPlayersChase)
            {
                // If player is in heli, enable searchlight
                if (CPlayer.LocalPlayer.Ped.IsInVehicle && CPlayer.LocalPlayer.Ped.CurrentVehicle.Model.IsHelicopter)
                {
                    CPlayer.LocalPlayer.Ped.CurrentVehicle.AVehicle.HeliSearchlightOn = true;
                    foreach (CPed ped in CPed.SortByDistanceToPosition(this.Criminals.ToArray(), CPlayer.LocalPlayer.Ped.Position))
                    {
                         CVehicle.SetHeliSearchlightTarget(ped);
                    }
                }

                // Ability to leave chase
                if (KeyHandler.IsKeyDown(ELCPDFRKeys.JoinPursuit) && this.CanPlayerJoin)
                {
                    DelayedCaller.Call(
                        delegate
                        {
                            if (KeyHandler.IsKeyStillDown(ELCPDFRKeys.JoinPursuit) && !CPlayer.LocalPlayer.Ped.IsAiming)
                            {
                                this.ClearAsCurrentPlayerChase();
                                PoliceScanner.CopBlipsActive = false;
                                TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("PURSUIT_PLAYER_LEFT"));
                            }
                        },
                            this,
                            500);
                }

                // Update blips before processing normal chase logic
                foreach (CPed ped in this.Criminals)
                {
                    if (ped.Exists())
                    {
                        if (ped.Wanted.WeaponUsed) weaponsUsed = true;

                        // If ped is alive
                        if (ped.IsAliveAndWell)
                        {
                            if (ped.SearchArea == null || ped.SearchArea.Deleted)
                            {
                                if (this.HasBeenCalledIn && !ped.Wanted.HasBeenArrested)
                                {
                                    ped.SearchArea = new SearchArea(ped, 200f);
                                }
                            }
                            else
                            {
                                if (ped.Wanted.VisualLostSince > 200 || ped.Wanted.VisualLost)
                                {
                                    ped.SearchArea.FollowPed = false;
                                }
                                else
                                {
                                    ped.SearchArea.FollowPed = true;
                                }

                                if (ped.Wanted.HelicoptersChasing > 0)
                                {
                                    ped.SearchArea.Size = 300f;
                                }
                                else
                                {
                                    ped.SearchArea.Size = 200f;
                                }
                            }

                            // If ped is not in vehicle
                            if (!ped.IsInVehicle)
                            {
                                // Delete blip of old vehicle
                                if (ped.LastVehicle != null && ped.LastVehicle.Exists())
                                {
                                    if (ped.LastVehicle.HasBlip)
                                    {
                                        ped.LastVehicle.DeleteBlip();
                                        ped.ClearLastUsedVehicle();
                                    }
                                }

                                if (!ped.HasBlip)
                                {
                                    if (!ped.Wanted.VisualLost && !ped.Wanted.HasBeenArrested)
                                    {   
                                        ped.AttachBlip();
                                    }
                                }
                                else
                                {
                                    // If visual is lost, make blip flash
                                    if (ped.Wanted.VisualLost)
                                    {
                                        if (ped.Blip.GetBlipDisplay() != BlipDisplay.ArrowOnly)
                                        {
                                            ped.Blip.Display = BlipDisplay.ArrowOnly;
                                            if (Main.NetworkManager.IsNetworkSession && Main.NetworkManager.CanSendData)
                                            {
                                                DynamicData dynamicData = new DynamicData(Main.NetworkManager.ActivePeer);
                                                dynamicData.Write(ped.NetworkID);
                                                dynamicData.Write((uint)BlipDisplay.ArrowOnly);
                                                Main.NetworkManager.SendMessage("CPed", EPedNetworkMessages.SetBlipMode, dynamicData);
                                            }
                                        }

                                        if (this.blips.ContainsKey(ped))
                                        {
                                            this.blips[ped].Delete();
                                            this.blips.Remove(ped);
                                        }
                                    }
                                    else
                                    {
                                        if (ped.Blip.GetBlipDisplay() != BlipDisplay.ArrowAndMap)
                                        {
                                            ped.Blip.Display = BlipDisplay.ArrowAndMap;
                                            if (Main.NetworkManager.IsNetworkSession && Main.NetworkManager.CanSendData)
                                            {
                                                DynamicData dynamicData = new DynamicData(Main.NetworkManager.ActivePeer);
                                                dynamicData.Write(ped.NetworkID);
                                                dynamicData.Write((uint)BlipDisplay.ArrowAndMap);
                                                Main.NetworkManager.SendMessage("CPed", EPedNetworkMessages.SetBlipMode, dynamicData);
                                            }
                                        }

                                        if (this.blips.ContainsKey(ped))
                                        {
                                            this.blips[ped].Delete();
                                            this.blips.Remove(ped);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // If so, remove possible blip for ped and add blip for vehicle, if it doesn't have one already
                                if (ped.HasBlip)
                                {
                                    ped.DeleteBlip();
                                }

                                if (!ped.CurrentVehicle.HasBlip)
                                {
                                    if (!ped.Wanted.VisualLost && !ped.Wanted.HasBeenArrested)
                                    {
                                        ped.CurrentVehicle.AttachBlip();
                                    }
                                }
                                else
                                {
                                    // If visual is lost, make blip flash
                                    if (ped.Wanted.VisualLost)
                                    {
                                        if (ped.CurrentVehicle.Blip.GetBlipDisplay() != BlipDisplay.ArrowOnly)
                                        {
                                            ped.CurrentVehicle.Blip.Display = BlipDisplay.ArrowOnly;
                                            ped.CurrentVehicle.Blip.RouteActive = false;

                                            if (Main.NetworkManager.IsNetworkSession && Main.NetworkManager.CanSendData)
                                            {
                                                DynamicData dynamicData = new DynamicData(Main.NetworkManager.ActivePeer);
                                                dynamicData.Write(ped.CurrentVehicle.NetworkID);
                                                dynamicData.Write((uint)BlipDisplay.ArrowOnly);
                                                Main.NetworkManager.SendMessage("CVehicle", EVehicleNetworkMessages.SetBlipMode, dynamicData);
                                            }
                                        }

                                        if (this.blips.ContainsKey(ped))
                                        {
                                            this.blips[ped].Delete();
                                            this.blips.Remove(ped);
                                        }

                                        /*
                                        Blip blip = AreaBlocker.CreateAreaBlip(ped.Wanted.LastKnownPosition, ped.Position.DistanceTo(ped.Wanted.LastKnownPosition) + 100, searchAreaColour);

                                        if (blip != null)
                                        {
                                            this.blips.Add(ped, blip);
                                        }
                                         * */
                                    }
                                    else
                                    {
                                        if (ped.CurrentVehicle.Blip.GetBlipDisplay() != BlipDisplay.ArrowAndMap)
                                        {
                                            ped.CurrentVehicle.Blip.Display = BlipDisplay.ArrowAndMap;
                                            ped.CurrentVehicle.Blip.RouteActive = true;

                                            if (Main.NetworkManager.IsNetworkSession && Main.NetworkManager.CanSendData)
                                            {
                                                DynamicData dynamicData = new DynamicData(Main.NetworkManager.ActivePeer);
                                                dynamicData.Write(ped.CurrentVehicle.NetworkID);
                                                dynamicData.Write((uint)BlipDisplay.ArrowAndMap);
                                                Main.NetworkManager.SendMessage("CVehicle", EVehicleNetworkMessages.SetBlipMode, dynamicData);
                                            }
                                        }

                                        if (this.blips.ContainsKey(ped))
                                        {
                                            this.blips[ped].Delete();
                                            this.blips.Remove(ped);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            // If not, remove all blips
                            if (ped.HasBlip)
                            {
                                ped.DeleteBlip();
                            }

                            if (this.blips.ContainsKey(ped))
                            {
                                this.blips[ped].Delete();
                                this.blips.Remove(ped);
                            }

                            // Delete blip of old vehicle
                            if (ped.Exists())
                            {
                                if (ped.LastVehicle != null && ped.LastVehicle.Exists())
                                {
                                    if (ped.LastVehicle.HasBlip)
                                    {
                                        // Only delete if no one else is alive in the vehicle
                                        CPed[] peds = ped.LastVehicle.GetAllPedsInVehicle();
                                        int dead = CPed.GetNumberOfDeadPeds(peds, true);
                                        if (dead == peds.Length)
                                        {
                                            ped.LastVehicle.DeleteBlip();
                                            ped.ClearLastUsedVehicle();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                // If player can join
                if (this.CanPlayerJoin)
                {
                    // Player should be pretty close to a suspect
                    foreach (CPed criminal in this.Criminals)
                    {
                        if (criminal.Wanted.WeaponUsed) weaponsUsed = true;

                        if (criminal.Exists() && criminal.Position.DistanceTo(CPlayer.LocalPlayer.Ped.Position) < DistanceToAllowPlayerJoining)
                        {
                            // Let player know
                            if (!this.displayedPlayerCanJoinHelpbox)
                            {
                                TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("PURSUIT_PLAYER_JOIN"), false);
                                this.displayedPlayerCanJoinHelpbox = true;
                                DelayedCaller.Call(delegate { this.displayedPlayerCanJoinHelpbox = false; }, this, 10000);
                            }

                            if (KeyHandler.IsKeyDown(ELCPDFRKeys.JoinPursuit) && !CPlayer.LocalPlayer.IsTargettingOrAimingAtPed && !CPlayer.LocalPlayer.Ped.IsAiming)
                            {
                                DelayedCaller.Call(
                                    delegate
                                        {
                                            if (KeyHandler.IsKeyStillDown(ELCPDFRKeys.JoinPursuit))
                                            {
                                                this.SetAsCurrentPlayerChase();
                                                TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("PURSUIT_PLAYER_JOINED"));
                                            }
                                        },
                                        this,
                                        200);
                            }

                            break;
                        }
                    }                  
                }
            }

            if (this.trafficChangeTimer.CanExecute(true))
            {
                // Lower traffic if chase has been ongoing for a long time or if shots have been fired.
                if ((DateTime.Now - this.startTime).TotalSeconds > TimeAfterTrafficDensityIsSetToLow)
                {
                    PopulationManager.SetPedDensity(0.5f);
                    PopulationManager.SetParkedCarDensity(0.5f);
                    PopulationManager.SetRandomCarDensity(0.5f);
                    PopulationManager.DisableCityServices();
                }

                if (this.ForceKilling || weaponsUsed)
                {
                    PopulationManager.SetPedDensity(0.2f);
                    PopulationManager.SetParkedCarDensity(0.5f);
                    PopulationManager.SetRandomCarDensity(0.2f);
                    PopulationManager.DisableCityServices();
                }
            }

            base.Process();
        }

        /// <summary>
        /// Ends the chase.
        /// </summary>
        public override void EndChase()
        {
            if (!this.IsRunning)
            {
                return;
            }

            // Before calling the base, we clean up all blips
            this.Cleanup();

            if (this.IsPlayersChase)
            {
                this.ClearAsCurrentPlayerChase();
            }

            // If this is the only running chase, reset cop density.
            if (this.IsOnlyActiveChase)
            {
                PopulationManager.SetRandomCopsDensity(0.8f);
                PopulationManager.SetRandomCarDensity(1.0f);

                PopulationManager.SetPedDensity(1.0f);
                PopulationManager.SetParkedCarDensity(1.0f);
                PopulationManager.EnableCityServices();
            }

            base.EndChase();
        }

        /// <summary>
        /// Calls in the pursuit so that backup can be dispatched.
        /// </summary>
        public void CallIn(AudioHelper.EPursuitCallInReason reason)
        {
            if (!this.HasBeenCalledIn)
            {
                //Game.Console.Print("PPR: " + reason);
                AudioHelper.PlayPursuitReported(reason);

                DelayedCaller.Call(delegate { AudioHelper.PlayDispatchAcknowledgeReportedCrime(CPlayer.LocalPlayer.Ped.Position, reason); }, Common.GetRandomValue(3000, 6000));
                this.HasBeenCalledIn = true;
                DelayedCaller.Call(delegate { this.EnableCopsForChase(null); }, Common.GetRandomValue(6000, 9000));
                

                if (this.Criminals[0] != null && this.Criminals[0].Exists())
                {
                    DelayedCaller.Call(
                        delegate
                        {
                            if (this.IsRunning)
                            {
                                AdvancedHookManaged.AGame.ReportPoliceSpottingSuspect(this.Criminals[0].APed);
                            }
                        },
                        15000);
                }
            }
        }

        /// <summary>
        /// Unsets this instance as the current chase instance of the player. This disables blips and texts for this chase.
        /// </summary>
        public void ClearAsCurrentPlayerChase()
        {
            if (CPlayer.LocalPlayer.Ped.PedData.CurrentChase == this)
            {
                CPlayer.LocalPlayer.Ped.PedData.CurrentChase = null;

                // Remove blips
                this.Cleanup();
            }
            else
            {
                Log.Warning("ClearAsCurrentPlayerChase: Attempt to clear chase that is not the current one", this);
            }
        }

        /// <summary>
        /// Returns the closest suspect for the player, so the one the player is most likely chasing.
        /// </summary>
        /// <returns>The suspect.</returns>
        public CPed GetClosestSuspectForPlayer()
        {
            CPed closestCriminal = null;
            CPed[] closestCriminals = CPed.SortByDistanceToPosition(this.Criminals.ToArray(), CPlayer.LocalPlayer.Ped.Position);

            // First of all, we use the closest suspect as the closest
            if (closestCriminals.Length > 0)
            {
                closestCriminal = closestCriminals[0];
            }

            foreach (CPed criminal in closestCriminals)
            {
                if (criminal.Exists() && criminal.IsAliveAndWell)
                {
                    if (!criminal.Wanted.IsBeingArrested || !criminal.Wanted.IsBeingArrestedByPlayer || !criminal.Wanted.HasBeenArrested)
                    {
                        return criminal;
                    }
                }
            }

            return closestCriminal;
        }

        /// <summary>
        /// Deploys a roadblock for the closest suspect. Returns true on success.
        /// </summary>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool DeployRoadblockForClosestSuspect()
        {
            if (this.Criminals.Count <= 0)
            {
                return false;
            }

            CPed suspect = this.Criminals[0];
            if (this.IsPlayersChase)
            {
                suspect = this.GetClosestSuspectForPlayer();
            }
            else
            {
                foreach (CPed criminal in this.Criminals)
                {
                    if (criminal.Exists() && criminal.IsInVehicle)
                    {
                        suspect = criminal;
                        break;
                    }
                }
            }

            if (suspect == null || !suspect.Exists())
            {
                Log.Warning("DeployRoadblockForClosestSuspect: Failed to find suspect", this);
                return false;
            }

            Vector3 position = suspect.GetOffsetPosition(new Vector3(0, 150, 0));

            // Only deploy roadblocks if position is away far enough, can't be seen and vehicles are allowed
            if (!suspect.Wanted.HasBeenArrested && !suspect.Wanted.IsBeingArrested && !suspect.Wanted.IsBeingArrestedByPlayer
                && position.DistanceTo2D(CPlayer.LocalPlayer.Ped.Position) > 140 && !Game.CurrentCamera.isSphereVisible(position, 5f)
                && this.AllowSuspectVehicles && suspect.IsInVehicle)
            {
                this.CreateRoadblock(position, CModel.CurrentCopCarModel, CModel.CurrentCopModel, 2, 4);
                return true;
            }
            else
            {
                Log.Debug("Process: Roadblock too close to player", this);
            }

            return false;
        }

        /// <summary>
        /// Makes the pursuit an active pursuit for the player by setting it as player's chase and letting cops join after the given amounts of time.
        /// This is used to prevent blips from appearing right after a chase has started and to prevent cops around immediately turn on their sirens to add a little realism
        /// since units may take a few seconds to respond.
        /// </summary>
        /// <param name="delayToMakePlayerChase">The amount of time before chase is set as player's. Use -1 to block.</param>
        /// <param name="delayToMakeCopsJoin">The amount of time before cops can join. Use -1 to block.</param>
        public void MakeActiveChase(int delayToMakePlayerChase, int delayToMakeCopsJoin)
        {
            if (delayToMakePlayerChase != -1)
            {
                DelayedCaller.Call(this.EnableChaseForPlayer, delayToMakePlayerChase, null);
            }

            if (delayToMakeCopsJoin != -1)
            {
                DelayedCaller.Call(this.EnableCopsForChase, delayToMakeCopsJoin, null);
            }
        }

        /// <summary>
        /// Sets this instance as the current chase instance of the player. This enables blips and texts for this chase.
        /// </summary>
        public void SetAsCurrentPlayerChase()
        {
            if (CPlayer.LocalPlayer.Ped.PedData.CurrentChase != null)
            {
                Log.Warning("SetAsCurrentPlayerChase: Player already has an active chase!", this);
            }

            CPlayer.LocalPlayer.Ped.PedData.CurrentChase = this;
            Stats.UpdateStat(Stats.EStatType.Chases, 1, CPlayer.LocalPlayer.Ped.Position);

            if (!this.DontEnableCopBlips)
            {
                PoliceScanner.CopBlipsActive = true;
            }

            // Even less cars now.
            PopulationManager.SetRandomCopsDensity(0.25f);
            PopulationManager.SetRandomCarDensity(0.7f);
        }

        /// <summary>
        /// Enables the chase for the player, so blips are displayed etc.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        private void EnableChaseForPlayer(object[] parameter)
        {
            // If chase didn't end yet
            if (this.IsRunning)
            {
                this.SetAsCurrentPlayerChase();
            }
        }

        /// <summary>
        /// Allows cops to chase the suspect.
        /// </summary>
        /// <param name="parameter">
        /// The parameter.
        /// </param>
        private void EnableCopsForChase(object[] parameter)
        {
            // If chase didn't end yet
            if (this.IsRunning)
            {
                this.CanCopsJoin = true;
            }
        }   

        /// <summary>
        /// Cleans up all blips and other things.
        /// </summary>
        private void Cleanup()
        {
            // Clean up all blips
            foreach (CPed criminal in this.Criminals)
            {
                if (criminal.Exists())
                {
                    if (criminal.SearchArea != null)
                    {
                        criminal.SearchArea.Remove();
                    }

                    if (criminal.HasBlip)
                    {
                        criminal.DeleteBlip();
                    }

                    // Delete blip of old vehicle
                    if (criminal.LastVehicle != null && criminal.LastVehicle.Exists())
                    {
                        if (criminal.LastVehicle.HasBlip)
                        {
                            criminal.LastVehicle.DeleteBlip();
                        }
                    }
                }
            }

            foreach (KeyValuePair<CPed, Blip> keyValuePair in this.blips)
            {
               if (keyValuePair.Value.Exists())
               {
                   keyValuePair.Value.Delete();
               }
            }

            PoliceScanner.CopBlipsActive = false;

            if (CPlayer.LocalPlayer.Ped.IsInVehicle && CPlayer.LocalPlayer.Ped.CurrentVehicle.Model.IsHelicopter)
            {
                CPlayer.LocalPlayer.Ped.CurrentVehicle.AVehicle.HeliSearchlightOn = false;
            }
        }
    }
}