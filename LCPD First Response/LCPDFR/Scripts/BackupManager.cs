namespace LCPD_First_Response.LCPDFR.Scripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using AdvancedHookManaged;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.IO;
    using LCPD_First_Response.Engine.Scripting;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Native;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Scripting.Scenarios;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.GUI;
    using LCPD_First_Response.LCPDFR.Input;

    using Main = LCPD_First_Response.LCPDFR.Main;

    /// <summary>
    /// The reported backup type.
    /// </summary>
    internal enum EReportBackupType
    {
        /// <summary>
        /// Default police backup.
        /// </summary>
        Police,

        /// <summary>
        /// Noose backup.
        /// </summary>
        Noose,

        /// <summary>
        /// Air backup.
        /// </summary>
        Air,

        /// <summary>
        /// Fbi backup.
        /// </summary>
        Fbi,
    }

    /// <summary>
    /// Resposible for handling backup requests. Depending on the situation (backup needed by AI, by user, in callout, pullover etc.) will report via speech
    /// and textwall.
    /// </summary>
    [ScriptInfo("BackupManager", true)]
    internal class BackupManager : GameScript, IPedController
    {
        /// <summary>
        /// Event handler for dispatched backup.
        /// </summary>
        /// <param name="peds">The cops.</param>
        public delegate void BackupDispatchedEventHandler(CPed[] peds);

        /// <summary>
        /// Flag for whether or not we're waiting for fire/EMS backup, LMS you might want to make this better at some point.
        /// </summary>
        private bool WaitingForFire;

        /// <summary>
        /// Flag for whether or not we're waiting for fire/EMS backup, LMS you might want to make this better at some point.
        /// </summary>
        private bool waitingForEMS;

        /// <summary>
        /// Requests a police backup to <paramref name="position"/>. If no close units are around, an unit will be dispatched.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="reporter">The reporter, e.g. control or officer LMS. If null, no report is made.</param>
        public void RequestPoliceBackup(Vector3 position, string reporter)
        {
            this.RequestPoliceBackup(position, reporter, null);
        }

        /// <summary>
        /// Requests a police backup to <paramref name="position"/>. If no close units are around, an unit will be dispatched.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="reporter">The reporter.</param>
        /// <param name="backupDispatchedCallback">The callback.</param>
        public void RequestPoliceBackup(Vector3 position, string reporter, BackupDispatchedEventHandler backupDispatchedCallback)
        {
            this.RequestPoliceBackup(position, reporter, false, EModelFlags.IsNormalUnit, EModelFlags.IsNormalUnit, backupDispatchedCallback);
        }

        /// <summary>
        /// Requests a police backup to <paramref name="position"/>. If no close units are around, an unit will be dispatched.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="reporter">The reporter.</param>
        /// <param name="doNothing">The unit will just be spawned/returned and no further tasks are applied. Unit can respond to events.</param>
        /// <param name="policePedModelFlags">The ped model flags. <see cref="EModelFlags.IsPolice"/> is added automatically.</param>
        /// <param name="policeVehicleModelFlags">The vehicle model flags. <see cref="EModelFlags.IsPolice"/> is added automatically.</param>
        /// <param name="backupDispatchedCallback">The callback.</param>
        public void RequestPoliceBackup(Vector3 position, string reporter, bool doNothing, EModelFlags policePedModelFlags, EModelFlags policeVehicleModelFlags, BackupDispatchedEventHandler backupDispatchedCallback)
        {
            this.RequestPoliceBackup(position, reporter, doNothing, policePedModelFlags, policeVehicleModelFlags, true, ECopState.Investigating, backupDispatchedCallback);
        }

        /// <summary>
        /// Requests a police backup to <paramref name="position"/>. If no close units are around, an unit will be dispatched.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="reporter">The reporter.</param>
        /// <param name="doNothing">The unit will just be spawned/returned and no further tasks are applied. Unit can respond to events.</param>
        /// <param name="policePedModelFlags">The ped model flags. <see cref="EModelFlags.IsPolice"/> is added automatically.</param>
        /// <param name="policeVehicleModelFlags">The vehicle model flags. <see cref="EModelFlags.IsPolice"/> is added automatically.</param>
        /// <param name="allowUnitsAround">Whether or not units around can be included without dispatching a new unit.</param>
        /// <param name="desiredNewState">The desired new state of the unit to check whether the found unit can be used.</param>
        /// <param name="backupDispatchedCallback">The callback.</param>
        public void RequestPoliceBackup(Vector3 position, string reporter, bool doNothing, EModelFlags policePedModelFlags, EModelFlags policeVehicleModelFlags, bool allowUnitsAround, ECopState desiredNewState, BackupDispatchedEventHandler backupDispatchedCallback)
        {
            if (reporter != null)
            {
                string area = AreaHelper.GetAreaNameMeaningful(position);
                Main.TextWall.AddText("[" + reporter + "] " + string.Format(CultureHelper.GetText("BACKUP_NEED_BACKUP"), area));
                AudioHelper.PlayPoliceBackupRequested();
            }

            if (CPlayer.LocalPlayer.Ped.PedData.CurrentChase != null)
            {
                // If on pursuit, play pursuit backup dispatch over radio
                DelayedCaller.Call(delegate { AudioHelper.PlayPolicePursuitBackupDispatched(position); }, this, Common.GetRandomValue(3000, 6000));
            }
            else
            {
                // Otherwise use normal dispatch
                if (reporter != null) DelayedCaller.Call(delegate { AudioHelper.PlayPoliceBackupDispatched(position); }, this, Common.GetRandomValue(3000, 6000));
            }

            this.RequestBackupUnit(position, doNothing, EModelFlags.IsPolice | policePedModelFlags, EModelFlags.IsPolice | policeVehicleModelFlags, 2, allowUnitsAround, desiredNewState, backupDispatchedCallback);
        }

        /// <summary>
        /// Requests a noose backup unit to <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="reporter">The reporter.</param>
        /// <param name="doNothing">The unit will just be spawned/returned and no further tasks are applied. Unit can respond to events.</param>
        /// <param name="desiredNewState">The desired new state of the unit to check whether the found unit can be used.</param>
        /// <param name="backupDispatchedCallback">The callback.</param>
        public void RequestNooseBackup(Vector3 position, string reporter, bool doNothing, ECopState desiredNewState, BackupDispatchedEventHandler backupDispatchedCallback)
        {
            if (reporter != null)
            {
                string area = AreaHelper.GetAreaNameMeaningful(CPlayer.LocalPlayer.Ped.Position);
                Main.TextWall.AddText("[" + reporter + "] " + string.Format(CultureHelper.GetText("BACKUP_NEED_NOOSE_BACKUP"), area));
            }

            // if (CPlayer.LocalPlayer.Ped.PedData.CurrentChase != null) doNothing = true;

            AudioHelper.PlayNooseBackupRequested();

            DelayedCaller.Call(
                delegate(object[] parameter) { AudioHelper.PlayNooseBackupDispatched(position); }, this, Common.GetRandomValue(6000, 8000));
           // DelayedCaller.Call(delegate { this.RequestBackupUnit(position, doNothing, EModelFlags.IsNoose, EModelFlags.IsNoose, 4, false, desiredNewState, backupDispatchedCallback); }, Common.GetRandomValue(15000, 20000));
            this.RequestBackupUnit(position, doNothing, EModelFlags.IsNoose, EModelFlags.IsNoose, 4, false, desiredNewState, backupDispatchedCallback);
        }

        /// <summary>
        /// Requests a backup backup to <paramref name="position"/>. If no close units are around, a unit will be dispatched.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="doNothing">The unit will just be spawned/returned and no further tasks are applied. Unit can respond to events.</param>
        /// <param name="policePedModelFlags">The ped model flags.</param>
        /// <param name="policeVehicleModelFlags">The vehicle model flags.</param>
        /// <param name="numberOfCops">The number of cops.</param>
        /// <param name="allowUnitsAround">Whether or not units around can be included without dispatching a new unit.</param>
        /// <param name="desiredNewState">The desired new state of the unit to check whether the found unit can be used.</param>
        /// <param name="backupDispatchedCallback">The callback.</param>
        public void RequestBackupUnit(Vector3 position, bool doNothing, EModelFlags policePedModelFlags, EModelFlags policeVehicleModelFlags, int numberOfCops, bool allowUnitsAround, ECopState desiredNewState, BackupDispatchedEventHandler backupDispatchedCallback)
        {
            Action action = delegate
            {
                // If no state given, use investigate
                if (desiredNewState == ECopState.None)
                {
                    desiredNewState = ECopState.Investigating;
                }

                // If player is chasing, dispatch to player's current location, not the one where it was called in.
                if (desiredNewState == ECopState.Chase)
                {
                    position = CPlayer.LocalPlayer.Ped.Position;
                }

                // Check for close unit
                CPed[] peds = null;
                if (allowUnitsAround)
                {
                    peds = Engine.Main.CopManager.RequestUnitInVehicle(position, policeVehicleModelFlags, 80);

                    // Verify that unit is idle
                    if (peds != null)
                    {
                        foreach (CPed ped in peds)
                        {
                            Log.Debug("State: " + ped.GetPedData<PedDataCop>().CopState, this);

                            if (!ped.GetPedData<PedDataCop>().IsFreeForAction(desiredNewState, this))
                            {
                                peds = null;
                                break;
                            }
                        }
                    }
                }

                if (peds == null)
                {
                    CopRequest copRequest = Engine.Main.CopManager.RequestDispatch(position, true, false, policePedModelFlags, policeVehicleModelFlags, numberOfCops, !doNothing, this.DispatchedRequestedUnit, backupDispatchedCallback);

                    if (LCPDFRPlayer.LocalPlayer.IsChasing || (Game.GetWaypoint() != null && Game.GetWaypoint().Exists()))
                    {
                        copRequest.OnBeforeCopCarCreated += this.copRequest_OnBeforeCopCarCreated;
                    }
                }
                else
                {
                    // Block ped for now, because we don't want any other calls return this unit, while it has not yet responded
                    foreach (CPed ped in peds)
                    {
                        ped.GetPedData<PedDataCop>().RequestPedAction(ECopState.Blocker, this);
                        Log.Debug("Forced state: " + ped.GetPedData<PedDataCop>().CopState, this);
                    }

                    DelayedCaller.Call(this.DispatchedCloseUnit, this, 1500, peds, position, doNothing, backupDispatchedCallback);
                }
            };

            DelayedCaller.Call(delegate { action(); }, this, 5000);
        }

        /// <summary>
        /// Called right before the cop vehicle is created.
        /// </summary>
        /// <param name="copRequest">The cop request.</param>
        /// <returns>The new position.</returns>
        private Vector3 copRequest_OnBeforeCopCarCreated(CopRequest copRequest)
        {
            Blip waypoint = Game.GetWaypoint();

            if (waypoint != null && waypoint.Exists())
            {
                if (waypoint.Position.DistanceTo(CPlayer.LocalPlayer.Ped.Position) > 50.0f)
                {
                    // If a waypoint exists and is far away, we want to spawn the units close to the player and let them go to the waypoint
                    Vector3 closestNodePosition = Vector3.Zero;
                    float closestNodeHeading = 0f;
                    CVehicle.GetClosestCarNodeWithHeading(CPlayer.LocalPlayer.Ped.Position.Around(25.0f), ref closestNodePosition, ref closestNodeHeading);
                    return closestNodePosition;  
                }
            }

            if (LCPDFRPlayer.LocalPlayer.IsChasing)
            {
                bool teleport = true;
                if (LCPDFRPlayer.LocalPlayer.Ped.PedData.CurrentChase is Pursuit)
                {
                    teleport = ((Pursuit)LCPDFRPlayer.LocalPlayer.Ped.PedData.CurrentChase).HasBeenCalledIn;
                }

                if (teleport)
                {
                    CPed suspect = ((Pursuit)LCPDFRPlayer.LocalPlayer.Ped.PedData.CurrentChase).GetClosestSuspectForPlayer();
                    if (suspect == null)
                    {
                        Log.Warning("copRequest_OnBeforeCopCarCreated: GetClosestSuspectForPlayer returned null", this);
                    }
                    else
                    {
                        return suspect.Position;
                    }
                }
            }

            return Vector3.Zero;
        }

        /// <summary>
        /// Requests a police helicopter backup to <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="requester">The reporter, e.g. control or officer LMS. If null, no report is made.</param>
        public void RequestPoliceHelicopter(Vector3 position, string requester)
        {
            if (requester != null)
            {
                string area = AreaHelper.GetAreaNameMeaningful(position);
                Main.TextWall.AddText("[" + requester + "] " + string.Format(CultureHelper.GetText("BACKUP_NEED_AIR_SUPPORT"), area));
            }

            AudioHelper.PlayAirSupportRequested();
            DelayedCaller.Call(delegate { AudioHelper.PlayAirUnitDispatched(position); }, this, Common.GetRandomValue(5000, 7000));

            // Delay the air support a little and spawn it far away
            DelayedCaller.Call(this.DispatchHelicopterUnit, this, Common.GetRandomValue(15000, 30000), CPlayer.LocalPlayer.Ped);
        }

        /// <summary>
        /// Requests a police boat backup to <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="requester">The reporter, e.g. control or officer LMS. If null, no report is made.</param>
        public void RequestPoliceBoat(Vector3 position, string requester)
        {
            if (requester != null)
            {
                string area = AreaHelper.GetAreaNameMeaningful(position);
                Main.TextWall.AddText("[" + requester + "] " + CultureHelper.GetText("BACKUP_NEED_WATER_SUPPORT"));
            }

            if (LCPDFRPlayer.LocalPlayer.IsChasing)
            {
                bool teleport = true;
                if (LCPDFRPlayer.LocalPlayer.Ped.PedData.CurrentChase is Pursuit)
                {
                    teleport = ((Pursuit)LCPDFRPlayer.LocalPlayer.Ped.PedData.CurrentChase).HasBeenCalledIn;
                }

                if (teleport)
                {
                    CPed suspect = ((Pursuit)LCPDFRPlayer.LocalPlayer.Ped.PedData.CurrentChase).GetClosestSuspectForPlayer();
                    position = suspect.GetOffsetPosition(new Vector3(0, 200, 0));
                }
            }

            AudioHelper.PlayPoliceBackupRequested();

            DelayedCaller.Call(
                delegate(object[] parameter) { AudioHelper.PlayPoliceWaterBackupDispatched(position); }, this, Common.GetRandomValue(6000, 8000));

            CopRequest copRequest = Engine.Main.CopManager.RequestDispatch(position, true, false, EUnitType.Boat, 1, !LCPDFRPlayer.LocalPlayer.IsChasing, this.WaterUnitDispatched);
            copRequest.ForceStaticPositionFindingForBoats = Settings.ForceStaticPositionForBoatBackup;

            if (LCPDFRPlayer.LocalPlayer.IsChasing) copRequest.OnBeforeCopCarCreated += delegate(CopRequest request) { CPed suspect = ((Pursuit)LCPDFRPlayer.LocalPlayer.Ped.PedData.CurrentChase).GetClosestSuspectForPlayer(); return suspect.GetOffsetPosition(new Vector3(0, 160, 0)); };
        }

        /// <summary>
        /// Requests a noose boat to <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="requester">The reporter, e.g. control or officer LMS. If null, no report is made.</param>
        public void RequestNooseBoat(Vector3 position, string requester)
        {
            if (requester != null)
            {
                string area = AreaHelper.GetAreaNameMeaningful(position);
                Main.TextWall.AddText("[" + requester + "] " + CultureHelper.GetText("BACKUP_NEED_NOOSE_WATER_SUPPORT"));
            }

            if (LCPDFRPlayer.LocalPlayer.IsChasing)
            {
                bool teleport = true;
                if (LCPDFRPlayer.LocalPlayer.Ped.PedData.CurrentChase is Pursuit)
                {
                    teleport = ((Pursuit)LCPDFRPlayer.LocalPlayer.Ped.PedData.CurrentChase).HasBeenCalledIn;
                }

                if (teleport)
                {
                    CPed suspect = ((Pursuit)LCPDFRPlayer.LocalPlayer.Ped.PedData.CurrentChase).GetClosestSuspectForPlayer();
                    position = suspect.GetOffsetPosition(new Vector3(0, 200, 0));
                }
            }

            AudioHelper.PlayNooseBackupRequested();

            DelayedCaller.Call(
                delegate(object[] parameter) { AudioHelper.PlayNooseWaterBackupDispatched(position); }, this, Common.GetRandomValue(6000, 8000));

            CopRequest copRequest = Engine.Main.CopManager.RequestDispatch(position, false, false, EUnitType.NooseBoat, 4, !LCPDFRPlayer.LocalPlayer.IsChasing, this.WaterUnitDispatched);
            copRequest.ForceStaticPositionFindingForBoats = Settings.ForceStaticPositionForBoatBackup;
            if (LCPDFRPlayer.LocalPlayer.IsChasing)  copRequest.OnBeforeCopCarCreated += delegate(CopRequest request) { CPed suspect = ((Pursuit)LCPDFRPlayer.LocalPlayer.Ped.PedData.CurrentChase).GetClosestSuspectForPlayer(); return suspect.GetOffsetPosition(new Vector3(0, 160, 0)); };
        }

        /// <summary>
        /// Requests an ambulance to <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="requester">The reporter, e.g. control or officer LMS. If null, no report is made.</param>
        public void RequestAmbulance(Vector3 position, string requester)
        {
            if (requester != null)
            {
                string area = AreaHelper.GetAreaNameMeaningful(position);
                Main.TextWall.AddText("[" + requester + "] " + string.Format(CultureHelper.GetText("BACKUP_NEED_AMBULANCE"), area));
            }

            AudioHelper.PlayAmbulanceRequested();
            DelayedCaller.Call(delegate { AudioHelper.PlayAmbulanceDispatched(position); }, this, Common.GetRandomValue(4000, 5000));

            // Delay the ambulance dispatch a little
            DelayedCaller.Call(
                delegate
                {
                    if (this.DispatchEmergencyServicesCar("AMBULANCE", "M_Y_PMEDIC", position))
                    {
                        TextHelper.PrintFormattedHelpBox("An ambulance is en-route.");
                    }
                    else
                    {
                        TextHelper.PrintFormattedHelpBox("Medical assistance not available.");
                    }

                }, 
                this,
                Common.GetRandomValue(10000, 12000));

            Stats.UpdateStat(Stats.EStatType.AmbulanceCalled, 1);
        }

        /// <summary>
        /// Requests a firetruck to <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="requester">The reporter, e.g. control or officer LMS. If null, no report is made.</param>
        public void RequestFiretruck(Vector3 position, string requester)
        {
            if (requester != null)
            {
                string area = AreaHelper.GetAreaNameMeaningful(position);
                Main.TextWall.AddText("[" + requester + "] " + string.Format(CultureHelper.GetText("BACKUP_NEED_FIRETRUCK"), area));
            }

            AudioHelper.PlaySpeechInScannerFromPed(LCPDFRPlayer.LocalPlayer.Ped, "EXPLOSION_IS_IMMINENT");
            DelayedCaller.Call(delegate { AudioHelper.PlayFiretruckDispatched(position); }, this, Common.GetRandomValue(4000, 5000));

            // Delay the firetruck dispatch a little
            DelayedCaller.Call(
                delegate
                {
                    if (this.DispatchEmergencyServicesCar("FIRETRUK", "M_Y_FIREMAN", position))
                    {
                        TextHelper.PrintFormattedHelpBox("A fire truck is en-route.");
                    }
                    else
                    {
                        TextHelper.PrintFormattedHelpBox("Fire department assistance not available.");
                    }

                },
                this,
                Common.GetRandomValue(10000, 12000));

            Stats.UpdateStat(Stats.EStatType.FirefighterCalled, 1);
        }

        /// <summary>
        /// Called every tick to process all script logic.
        /// </summary>
        public override void Process()
        {
            base.Process();

            //int scum = GTA.Native.Function.Call<int>("GET_CURRENT_ZONE_SCUMMINESS");
            //Game.DisplayText("Height: " + CPlayer.LocalPlayer.Ped.HeightAboveGround + "  Scum: " + scum);

            if (KeyHandler.IsKeyDown(ELCPDFRKeys.RequestBackup))
            {
                Stats.UpdateStat(Stats.EStatType.BackupCalled, 1, CPlayer.LocalPlayer.Ped.Position);

                Vector3 position = CPlayer.LocalPlayer.Ped.Position;
                Blip waypoint = Game.GetWaypoint();

                if (waypoint != null && waypoint.Exists())
                {
                    if (waypoint.Position != null)
                    {
                        // If waypoint exists, set the units to go there.
                        position = waypoint.Position;
                    }
                }

                // If player is actively chasing suspects
                if (CPlayer.LocalPlayer.Ped.PedData.CurrentChase != null)
                {
                    // If it's a water pursuit, only dispatch boats.
                    if (CPlayer.LocalPlayer.Ped.PedData.CurrentChase.IsWaterPursuit())
                    {
                        string reporter = CultureHelper.GetText("POLICE_SCANNER_OFFICER") + " " + CPlayer.LocalPlayer.GetOfficerName();

                        // Dispatch boat.
                        this.RequestPoliceBoat(CPlayer.LocalPlayer.Ped.Position, reporter);
                    }
                    else
                    {

                        string reporter = CultureHelper.GetText("POLICE_SCANNER_OFFICER") + " " + CPlayer.LocalPlayer.GetOfficerName();
                        this.RequestPoliceBackup(position, reporter);
                    }
                }
                else
                {
                    string reporter = CultureHelper.GetText("POLICE_SCANNER_OFFICER") + " " + CPlayer.LocalPlayer.GetOfficerName();
                    if (CPlayer.LocalPlayer.Ped.IsInVehicle)
                    {
                        if (CPlayer.LocalPlayer.Ped.CurrentVehicle.Exists())
                        {
                            if (CPlayer.LocalPlayer.Ped.CurrentVehicle.Model.IsBoat && CPlayer.LocalPlayer.Ped.CurrentVehicle.IsInWater)
                            {
                                // Water backup please
                                this.RequestPoliceBoat(CPlayer.LocalPlayer.Ped.Position, reporter);
                                return;
                            }
                        }
                    }
                    
                    this.RequestPoliceBackup(position, reporter);
                }
            }

            if (KeyHandler.IsKeyDown(ELCPDFRKeys.RequestAmbulance))
            {
                this.RequestAmbulance(Game.LocalPlayer.Character.Position, CultureHelper.GetText("POLICE_SCANNER_OFFICER") + " " + CPlayer.LocalPlayer.GetOfficerName());
            }

            if (KeyHandler.IsKeyDown(ELCPDFRKeys.RequestFirefighter))
            {
                this.RequestFiretruck(Game.LocalPlayer.Character.Position, CultureHelper.GetText("POLICE_SCANNER_OFFICER") + " " + CPlayer.LocalPlayer.GetOfficerName());
            }

            if (KeyHandler.IsKeyDown(ELCPDFRKeys.RequestNooseBackup))
            {
                Stats.UpdateStat(Stats.EStatType.NooseBackupCalled, 1, CPlayer.LocalPlayer.Ped.Position);

                string reporter = CultureHelper.GetText("POLICE_SCANNER_OFFICER") + " " + CPlayer.LocalPlayer.GetOfficerName();

                Vector3 position = CPlayer.LocalPlayer.Ped.Position;
                Blip waypoint = Game.GetWaypoint();

                if (waypoint != null && waypoint.Exists())
                {
                    if (waypoint.Position != null)
                    {
                        // If waypoint exists, set the units to go there.
                        position = waypoint.Position;
                    }
                }

                if (CPlayer.LocalPlayer.Ped.PedData.CurrentChase != null)
                {
                    // If it's a water pursuit, only dispatch boats.
                    if (CPlayer.LocalPlayer.Ped.PedData.CurrentChase.IsWaterPursuit())
                    {
                        this.RequestNooseBoat(CPlayer.LocalPlayer.Ped.Position, reporter);
                    }
                    else
                    {
                        this.RequestNooseBackup(position, reporter, false, ECopState.Chase, this.NooseUnitDispatched);
                    }
                }
                else
                {
                    if (CPlayer.LocalPlayer.Ped.IsInVehicle)
                    {
                        if (CPlayer.LocalPlayer.Ped.CurrentVehicle.Exists())
                        {
                            if (CPlayer.LocalPlayer.Ped.CurrentVehicle.Model.IsBoat && CPlayer.LocalPlayer.Ped.CurrentVehicle.IsInWater)
                            {
                                // Water backup please
                                this.RequestNooseBoat(CPlayer.LocalPlayer.Ped.Position, reporter);
                                return;
                            }
                        }
                    }

                    this.RequestNooseBackup(position, reporter, false, ECopState.Investigating, this.NooseUnitDispatched);
                }

            }

            // Heli backup is available in chases only, if not in chase a noose gunship will be dispatched instead
            if (KeyHandler.IsKeyDown(ELCPDFRKeys.RequestHelicopterBackup))
            {
                // If player is actively chasing suspects
                if (CPlayer.LocalPlayer.Ped.PedData.CurrentChase != null)
                {
                    Stats.UpdateStat(Stats.EStatType.HelicopterBackupCalled, 1, CPlayer.LocalPlayer.Ped.Position);

                    string reporter = CultureHelper.GetText("POLICE_SCANNER_OFFICER") + " " + CPlayer.LocalPlayer.GetOfficerName();

                    // Dispatch heli
                    this.RequestPoliceHelicopter(CPlayer.LocalPlayer.Ped.Position, reporter);
                }
            }
        }




        /// <summary>
        /// Called when a close unit was used.
        /// </summary>
        /// <param name="objects">
        /// The objects.
        /// </param>
        private void DispatchedCloseUnit(params object[] objects)
        {
            CPed[] peds = (CPed[])objects[0];
            Vector3 position = (Vector3)objects[1];
            bool doNothing = (bool)objects[2];

            // Check if peds are still valid
            foreach (CPed ped in peds)
            {
                if (!ped.Exists())
                {
                    Main.TextWall.AddText("[" + CultureHelper.GetText("POLICE_SCANNER_CONTROL") + "] " + CultureHelper.GetText("BACKUP_NO_UNIT_AVAILABLE"));
                    return;
                }
            }

            // Free units again, so the scenario can use them
            foreach (CPed ped in peds)
            {
                ped.GetPedData<PedDataCop>().ResetPedAction(this);
            }

            if (!doNothing)
            {
                var scenario = new ScenarioCopsInvestigateCrimeScene(peds, position, false);
                var taskScenario = new TaskScenario(scenario);
                ContentManager.AddScenario(scenario);
            }

            string name = peds[0].PedData.Persona.Forename + " " + peds[0].PedData.Persona.Surname;
            Main.TextWall.AddText("[" + CultureHelper.GetText("POLICE_SCANNER_OFFICER") + " " + name + "] " + CultureHelper.GetText("BACKUP_IN_VICINITY"));

            this.Dispatched(peds, peds[0].CurrentVehicle);

            // Invoke callback
            BackupDispatchedEventHandler backupDispatchedEventHandler = (BackupDispatchedEventHandler)objects[3];
            if (backupDispatchedEventHandler != null)
            {
                backupDispatchedEventHandler.Invoke(peds);
            }
        }

        /// <summary>
        /// Called when a requested unit has been dispatched.
        /// </summary>
        /// <param name="copRequest">The cop request.</param>
        private void DispatchedRequestedUnit(CopRequest copRequest)
        {
            Main.TextWall.AddText("[" + CultureHelper.GetText("POLICE_SCANNER_CONTROL") + "] " + CultureHelper.GetText("BACKUP_UNIT_DISPATCHED"));

            this.Dispatched(copRequest.Cops, copRequest.Vehicle);

            if (copRequest.CallbackParameter.Length > 0)
            {
                // Invoke callback
                BackupDispatchedEventHandler backupDispatchedEventHandler = (BackupDispatchedEventHandler)copRequest.CallbackParameter[0];
                if (backupDispatchedEventHandler != null)
                {
                    backupDispatchedEventHandler.Invoke(copRequest.Cops);
                }
            }
        }

        /// <summary>
        /// Dispatches an ambulance or fire truck
        /// </summary>
        /// <param name="parameter">The parameters for CreateEmergencyServicesCarReturnDriver native (model, pedmodel, position)</param>
        private bool DispatchEmergencyServicesCar(params object[] parameter)
        {
            // Get the model to be created
            String vehicleModel = parameter[0] as String;
            String pedModel = parameter[1] as String;
            Vector3 position = (Vector3)parameter[2];

            CPed driver = null;
            CPed passenger = null;
            CVehicle vehicle = null;

            int attemptsRemaining = 10;

            while (attemptsRemaining > 0)
            {
                // Natives.CreateEmergencyServicesCarReturnDriver(vModel, pModel, position, ref driver, ref passenger, ref vehicle);
                // Game.Console.Print(String.Format("CESCRD ({0}, {1}, {2})", vModel, pModel, position));

                // New code which I think will work better - in response to bug with CESCRD native spawning civilians as drivers
                Vector3 spawnPosition = World.GetNextPositionOnStreet(position.Around(150.0f));

                // This gets a good position far away
                while (spawnPosition.DistanceTo(position) < 100.0f)
                {
                    spawnPosition = World.GetNextPositionOnStreet(position.Around(150.0f));
                }

                // Create the vehicle
                Vector3 closestNodePosition = Vector3.Zero;
                float closestNodeHeading = 0f;
                CVehicle.GetClosestCarNodeWithHeading(spawnPosition, ref closestNodePosition, ref closestNodeHeading);

                vehicle = new CVehicle(new CModel(vehicleModel), closestNodePosition, EVehicleGroup.Normal);
                if (vehicle.Exists())
                {
                    // Vehicle exists, now we need to create the peds
                    driver = new CPed(pedModel, vehicle.Position, EPedGroup.Pedestrian);
                    passenger = new CPed(pedModel, vehicle.Position, EPedGroup.Pedestrian);

                    // Check if the peds exist.
                    if (driver.Exists() && passenger.Exists())
                    {
                        driver.WarpIntoVehicle(vehicle, VehicleSeat.Driver);
                        passenger.WarpIntoVehicle(vehicle, VehicleSeat.RightFront);

                        // If they do, assign tasks
                        Natives.TaskCarMission(driver, vehicle, position, 4, 30.0f, 2, 10, 10);

                        // Siren and Engine
                        vehicle.SirenActive = true;
                        vehicle.EngineRunning = true;
                        vehicle.Heading = closestNodeHeading;

                        // If firetruck or ambulance, assign appropriate task
                        if (vehicleModel == "AMBULANCE")
                        {
                            driver.APed.TaskMedicTreatInjuredPed(passenger.APed, vehicle.AVehicle);
                        }
                        else if (vehicleModel == "FIRETRUK")
                        {
                            driver.APed.TaskDriveFireTruck(passenger.APed, vehicle.AVehicle);
                        }
                    }
                }

                // Now we check that everything went okay

                if (driver != null && driver.Exists() && passenger != null && passenger.Exists() && vehicle != null && vehicle.Exists())
                {
                    // Everything is fine - no action required so break the loop.
                    break;
                }
                else
                {
                    // Either a ped or the vehicle is missing - delete all and try again.
                    if (driver != null && driver.Exists())
                    {
                        driver.Delete();
                    }
                    if (passenger != null && passenger.Exists())
                    {
                        passenger.Delete();
                    }
                    if (vehicle != null && vehicle.Exists())
                    {
                        vehicle.Delete();
                    }

                    attemptsRemaining--;
                }
            }

            if (driver != null && driver.Exists() && passenger != null && passenger.Exists() && vehicle != null && vehicle.Exists())
            {
                ContentManager.AddPed(driver, 200f);
                ContentManager.AddPed(passenger, 200f);
                ContentManager.AddVehicle(vehicle, 200f);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Dispatches a helicopter unit.
        /// </summary>
        /// <param name="parameter">The parameter for DelayedCaller.</param>
        private void DispatchHelicopterUnit(params object[] parameter)
        {
            // Get the ped where the heli should be spawned
            CPed ped = parameter[0] as CPed;

            if (ped.PedData.CurrentChase != null)
            {
                if (!ped.PedData.CurrentChase.ForceKilling && !Settings.ForceAnnihilatorHelicopter)
                {
                    // If not using lethal force, dispatch normal helicopter
                    Engine.Main.CopManager.RequestDispatch(ped.Position, true, true, CModel.DefaultCopHelicopterModel, CModel.CurrentCopModel, 2, false, this.DispatchedRequestHelicopterUnit, ped.Position);
                }
                else
                { 
                    // Otherwise dispatch NOOSE Annihilator
                    Engine.Main.CopManager.RequestDispatch(ped.Position, true, true, CModel.DefaultNooseHelicopterModel, CModel.CurrentCopModel, 4, false, this.DispatchedRequestHelicopterUnit, ped.Position);
                }
            }
        }

        /// <summary>
        /// Called when a requested helicopter unit has been dispatched.
        /// </summary>
        /// <param name="copRequest">The cop request.</param>
        private void DispatchedRequestHelicopterUnit(CopRequest copRequest)
        {
            Main.TextWall.AddText("[" + CultureHelper.GetText("POLICE_SCANNER_CONTROL") + "] " + CultureHelper.GetText("BACKUP_AIR_UNIT_DISPATCHED"));
            if (copRequest.Vehicle.Model == "ANNIHILATOR")
            {
                copRequest.Vehicle.AVehicle.SetAsPoliceVehicle(true);
            }

            copRequest.Vehicle.NoLongerNeeded();

            var scenario = new ScenarioCopHelicopterInvestigate(copRequest.Cops, copRequest.Vehicle, (Vector3)copRequest.CallbackParameter[0]);
            var taskScenario = new TaskScenario(scenario);
            ContentManager.AddScenario(scenario);
        }

        /// <summary>
        /// Called when the unit has been dispatched.
        /// </summary>
        /// <param name="peds">
        /// The peds.
        /// </param>
        /// <param name="vehicle">
        /// The vehicle.
        /// </param>
        private void Dispatched(CPed[] peds, CVehicle vehicle)
        {
            // There's a small chance that the cop has left the vehicle already
            if (vehicle != null && vehicle.Exists())
            {
                vehicle.NoLongerNeeded();
                // vehicle.AttachBlip().Friendly = true;
            }

            // Get scenario and add to content manager as well
            CPed ped = peds[0];
            if (ped.Exists())
            {
                IPedController pedController = ped.Intelligence.PedController;
                if (pedController is ScenarioCopsInvestigateCrimeScene)
                {
                    ContentManager.AddScenario((ScenarioCopsInvestigateCrimeScene)pedController);
                } 
            }

            foreach (CPed cop in peds)
            {
                if (cop != null && cop.Exists())
                {
                    Blip copBlip = cop.AttachBlip(sync: false);
                    if (copBlip != null && copBlip.Exists())
                    {
                        copBlip.Friendly = true;
                        // copBlip.Display = BlipDisplay.ArrowOnly;
                        if (cop.PedData.Persona.Surname != null)
                        {
                            if (cop.PedSubGroup == EPedSubGroup.FBI)
                            {
                                copBlip.Name = "Agent " + cop.PedData.Persona.Surname;
                            }
                            else
                            {
                                copBlip.Name = "Officer " + cop.PedData.Persona.Surname;
                            }
                        }
                    }
                }
            }

            // AudioHelper.PlayPoliceBackupDispatched(ped.Position);
        }

        /// <summary>
        /// Called when noose backup has been dispatched.
        /// </summary>
        /// <param name="peds">The peds.</param>
        private void NooseUnitDispatched(CPed[] peds)
        {
            foreach (CPed ped in peds)
            {
                if (ped.Exists())
                {
                    ped.PedData.Flags |= EPedFlags.IgnoreMaxUnitsLimitInChase;
                }
            }
        }

        /// <summary>
        /// Called when a water unit has been dispatched unit.
        /// </summary>
        /// <param name="copRequest">The cop request.</param>
        private void WaterUnitDispatched(CopRequest copRequest)
        {
            foreach (CPed ped in copRequest.Cops)
            {
                if (ped.Exists())
                {
                    // TODO: Remove
                    ped.PedData.Flags |= EPedFlags.IgnoreMaxUnitsLimitInChase;
                }
            }

            if (copRequest.Vehicle.Exists())
            {
                if (copRequest.Vehicle.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsCopBoat)
                    || copRequest.Vehicle.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsNoose))
                {
                    copRequest.Vehicle.AVehicle.SetAsPoliceVehicle(true);
                }
            }
        }

        /// <summary>
        /// Invoked when a cop that has not yet responded but was chosen to respond has left. We don't care.
        /// </summary>
        /// <param name="ped"></param>
        public void PedHasLeft(CPed ped)
        {
        }
    }
}