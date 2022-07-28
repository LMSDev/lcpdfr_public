namespace LCPD_First_Response.LCPDFR.Scripts.Scenarios
{
    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Scenarios;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.GUI;

    /// <summary>
    /// The scenario for a basic on foot pursuit.
    /// </summary>
    internal class ScenarioRandomPursuit : Scenario, IAmbientScenario, ICanOwnEntities, IPedController
    {
        /// <summary>
        /// Whether the chase is on foot only.
        /// </summary>
        private bool isOnFootChase;

        /// <summary>
        /// Whether player has approached driver for the first time.
        /// </summary>
        private bool notFirstApproach;

        /// <summary>
        /// The ped.
        /// </summary>
        private CPed ped;

        /// <summary>
        /// The position.
        /// </summary>
        private Vector3 position;

        /// <summary>
        /// The pursuit instance.
        /// </summary>
        private Pursuit pursuit;

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get
            {
                return "ScenarioRandomPursuit";
            }
        }

        public ScenarioRandomPursuit()
        {}

        public ScenarioRandomPursuit(CPed officer, CPed suspect)
        {
            this.ped = suspect;

            if (!this.ped.Exists())
            {
                Log.Warning("ScenarioRandomPursuit: Suspect didn't exist", this);
                return;
            }

            if (officer.Exists())
            {
                if (!officer.IsAmbientSpeechPlaying) officer.SayAmbientSpeech(officer.VoiceData.StopSpeech);
            }

            this.ped.BecomeMissionCharacter();
            this.ped.RequestOwnership(this);
            this.ped.Intelligence.RequestForAction(EPedActionPriority.RequiredByScript, this);
            this.ped.PedData.ComplianceChance = 30;
            this.ped.BlockPermanentEvents = true;

            this.pursuit = new Pursuit();
            this.pursuit.CanCopsJoin = true;
            this.pursuit.CanPlayerJoin = false;
            this.pursuit.HasBeenCalledIn = true;
            this.pursuit.SetupCop(officer, true);
            this.pursuit.AddTarget(ped);
            this.pursuit.MakeActiveChase(-1, 1000);
            this.pursuit.CanCopsJoin = false;

            DelayedCaller.Call(delegate { if (officer.Exists()) if (!officer.IsAmbientSpeechPlaying) officer.SayAmbientSpeech("REQUEST_BACKUP"); }, this, 4000);
            DelayedCaller.Call(delegate {InitializeAmbient(officer);}, this, Common.GetRandomValue(7000, 8000));
        }

        /// <summary>
        /// This is called to initialize an ambient foot pursuit
        /// </summary>
        public void InitializeAmbient(CPed officer)
        {
            if (!ped.Exists())
            {
                this.MakeAbortable();
                return;
            }

            // This is an ambient foot chase which was turned into a pursuit using this scenario
            this.pursuit.CanCopsJoin = true;
            this.pursuit.CanPlayerJoin = true;

            if (officer.Exists())
            {
                officer.BecomeMissionCharacter();
                officer.Task.ClearAll();
            }

            this.position = ped.Position;
            // this.ped.AttachBlip().Color = BlipColor.White;

            int randomValue = Common.GetRandomValue(0, 5);
            switch (randomValue)
            {
                case 0:
                    AudioHelper.PlayActionInScannerUsingPosition("INS_AVAILABLE_UNITS_RESPOND_TO CRIM_A_CRIMINAL_RESISTING_ARREST IN_IN POSITION", this.position);
                    break;
                case 1:
                    AudioHelper.PlayActionInScannerUsingPosition("INS_THIS_IS_CONTROL_I_NEED_ASSISTANCE_FOR CRIM_A_SUSPECT_RESISTING_ARREST IN_IN POSITION", this.position);
                    break;
                case 2:
                    AudioHelper.PlayActionInScannerUsingPosition("INS_UNITS_REPORT CRIM_A_SUSPECT_RESISTING_ARREST IN_IN POSITION", this.position);
                    break;
                case 3:
                    AudioHelper.PlayActionInScannerUsingPosition("ASSISTANCE_REQUIRED FOR CRIM_A_CRIMINAL_FLEEING_A_CRIME_SCENE IN_IN POSITION", this.position);
                    break;
                case 4:
                    AudioHelper.PlayActionInScannerUsingPosition("INS_UNITS_REPORT CRIM_A_CRIMINAL_FLEEING_A_CRIME_SCENE IN_IN POSITION", this.position);
                    break;
            }

            return;
        }

        /// <summary>
        /// This is called immediately before the scenario is executed the first time.
        /// </summary>
        public override void Initialize()
        {
            this.ped.RequestOwnership(this);
            this.ped.Intelligence.RequestForAction(EPedActionPriority.RequiredByScript, this);
            this.ped.PedData.ComplianceChance = 30;
            this.ped.BlockPermanentEvents = true;

            // Simple chase
            this.pursuit = new Pursuit();
            this.pursuit.AllowSuspectVehicles = Common.GetRandomBool(0, 5, 1) || !this.isOnFootChase;
            this.pursuit.AllowSuspectWeapons = Common.GetRandomBool(0, 5, 1);
            this.pursuit.CanCopsJoin = true;
            this.pursuit.CanPlayerJoin = true;
            this.pursuit.HasBeenCalledIn = true;
            this.pursuit.MakeActiveChase(-1, 2500);

            if (this.isOnFootChase)
            {
                this.pursuit.AddTarget(this.ped);

                // Spawn close cop
                CPed cop = new CPed(CModel.CurrentCopModel, World.GetNextPositionOnPavement(this.ped.GetOffsetPosition(new Vector3(0, -15, 0))), EPedGroup.Cop);
                // cop.AttachBlip();

                int randomValue = Common.GetRandomValue(0, 6);
                switch (randomValue)
                {
                    case 0:
                        AudioHelper.PlayActionInScannerUsingPosition("INS_WE_HAVE_A_REPORT_OF_ERRR CRIM_A_SUSPECT_RESISTING_ARREST ON_FOOT IN POSITION", this.position);
                        break;
                    case 1:
                        AudioHelper.PlayActionInScannerUsingPosition("ATTENTION_ALL_UNITS INS_WEVE_GOT CRIM_A_CRIMINAL_FLEEING_A_CRIME_SCENE ON_FOOT IN POSITION", this.position);
                        break;
                    case 2:
                        AudioHelper.PlayActionInScannerUsingPosition("INS_AVAILABLE_UNITS_RESPOND_TO CRIM_A_CRIMINAL_RESISTING_ARREST ON_FOOT IN POSITION", this.position);
                        break;
                    case 3:
                        AudioHelper.PlayActionInScannerUsingPosition("INS_THIS_IS_CONTROL_I_NEED_ASSISTANCE_FOR CRIM_A_SUSPECT_RESISTING_ARREST ON_FOOT IN POSITION", this.position);
                        break;
                    case 4:
                        AudioHelper.PlayActionInScannerUsingPosition("INS_UNITS_REPORT CRIM_A_CRIMINAL_RESISTING_ARREST ON_FOOT IN POSITION", this.position);
                        break;
                    case 5:
                        AudioHelper.PlayActionInScannerUsingPosition("ASSISTANCE_REQUIRED FOR CRIM_A_CRIMINAL_FLEEING_A_CRIME_SCENE ON_FOOT IN POSITION", this.position);
                        break;
                }
            }
            else
            {
                foreach (CPed ped in this.ped.CurrentVehicle.GetAllPedsInVehicle())
                {
                    this.pursuit.AddTarget(ped);
                }

                CVehicle copCar = new CVehicle(CModel.CurrentCopCarModel, World.GetNextPositionOnStreet(this.ped.Position), EVehicleGroup.Police);
                if (copCar.Exists())
                {
                    copCar.PlaceOnNextStreetProperly();
                    copCar.NoLongerNeeded();

                    CPed copDriver = copCar.CreatePedOnSeat(VehicleSeat.Driver);
                    if (copDriver != null && copDriver.Exists())
                    {
                        copDriver.NoLongerNeeded();
                        copCar.SirenActive = true;
                        copDriver.SayAmbientSpeech("PULL_OVER_WARNING");
                    }
                }

                AudioHelper.PlayDispatchAcknowledgeReportedCrime(this.position, AudioHelper.EPursuitCallInReason.Pursuit);
            }

            // this.ped.AttachBlip().Color = BlipColor.White;
        }

        /// <summary>
        /// By default frees all owned entities and deletes the base component reference
        /// </summary>
        public override void MakeAbortable()
        {
            base.MakeAbortable();

            if (this.pursuit != null)
            {
                this.pursuit.EndChase();
            }

            this.ped.ReleaseOwnership(this);
            this.ped.Intelligence.ResetAction(this);

            DelayedCaller.ClearAllRunningCalls(false, this);

            Log.Debug("ScenarioRandomPursuit: End", this);
        }

        /// <summary>
        /// Processes the scenario logic.
        /// </summary>
        public override void Process()
        {
            if (this.OwnedEntities.Count == 0)
            {
                this.MakeAbortable();
                return;
            }

            // Deleted, dead or arrest -> End
            if (this.ped != null && (!this.ped.Exists() || !this.ped.IsAliveAndWell || this.ped.Wanted.HasBeenArrested))
            {
                this.PedHasLeft(this.ped);
            }

            if (!this.pursuit.IsRunning)
            {
                this.MakeAbortable();
            }
            else
            {
                if (!this.pursuit.IsPlayersChase)
                {
                    if (this.ped != null && this.ped.Exists())
                    {
                        if (!this.notFirstApproach)
                        {
                            this.notFirstApproach = CameraHelper.PerformEventFocus(this.ped, true, 1000, 3500, true, false, true);
                        }

                        if (this.ped.HasBlip)
                        {
                            if (this.ped.Wanted.VisualLost)
                            {
                                this.ped.Blip.Display = BlipDisplay.Hidden;
                            }
                            else
                            {
                                this.ped.Blip.Display = BlipDisplay.ArrowAndMap;
                            }
                        }
                        else
                        {
                            // this.ped.AttachBlip().Color = BlipColor.White;
                        }
                    }
                }
                else
                {
                    if (this.ped != null && this.ped.Exists() && this.ped.HasBlip)
                    {
                        if (this.ped.HasBlip && this.ped.Blip.Color == BlipColor.White)
                        {
                            this.ped.Blip.Delete();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks whether the scenario can start at the position depending on available peds.
        /// </summary>
        /// <param name="position">
        /// The position.
        /// </param>
        /// <returns>
        /// True if yes, otherwise false.
        /// </returns>
        public bool CanScenarioStart(Vector3 position)
        {
            this.position = position;
            this.isOnFootChase = Common.GetRandomBool(0, 3, 1);

            if (this.isOnFootChase)
            {
                // Look for peds meeting the requirements
                CPed[] peds = CPed.GetPedsAround(80f, EPedSearchCriteria.AmbientPed | EPedSearchCriteria.NotInVehicle, position);
                foreach (CPed ped in peds)
                {
                    // Ped should not be seen by player at the moment
                    if (ped.Intelligence.IsFreeForAction(EPedActionPriority.RequiredByScript) && !ped.IsOnStreet() && !CPlayer.LocalPlayer.HasVisualOnSuspect(ped) 
                        && !ped.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.HasJob) && ped.IsAliveAndWell)
                    {
                        this.ped = ped;
                        return true;
                    }
                }
            }
            else
            {
                // Look for vehicles with driver
                CVehicle[] vehicles = CVehicle.GetVehiclesAround(300f, EVehicleSearchCriteria.DriverOnly | EVehicleSearchCriteria.NoCop | EVehicleSearchCriteria.NoPlayersLastVehicle, position);
                foreach (CVehicle vehicle in vehicles)
                {
                    if (vehicle.Driver == null || !vehicle.Driver.Exists())
                    {
                        Log.Warning("CanScenarioStart: Vehicle without driver returned", this);
                        return false;
                    }

                    // If not yet seen, make use of it
                    if (!CPlayer.LocalPlayer.HasVisualOnSuspect(vehicle.Driver) && !vehicle.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsEmergencyServicesVehicle)
                        && vehicle.Position.DistanceTo(CPlayer.LocalPlayer.Ped.Position) > 100f && vehicle.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsVehicle))
                    {
                        CPed ped = vehicle.Driver;

                        // Ped should not be seen by player at the moment
                        if (ped.Intelligence.IsFreeForAction(EPedActionPriority.RequiredByScript) && !ped.IsOnStreet() && !CPlayer.LocalPlayer.HasVisualOnSuspect(ped) 
                            && !ped.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.HasJob) && ped.IsAliveAndWell)
                        {
                            this.ped = ped;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks whether the scenario can be disposed now, most likely because player got too far away.
        /// </summary>
        /// <returns>True if yes, false otherwise.</returns>
        public bool CanBeDisposedNow()
        {
            if (!this.pursuit.IsPlayersChase)
            {
                if (this.ped != null && this.ped.Exists() && this.ped.Position.DistanceTo(CPlayer.LocalPlayer.Ped.Position) > 500)
                {
                    this.MakeAbortable();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Called when a ped has 'left' from the current controller, e.g. due to a more important event. Use this to clean up things.
        /// </summary>
        /// <param name="ped">
        /// The ped.
        /// </param>
        public void PedHasLeft(CPed ped)
        {
            this.MakeAbortable();
        }
    }
}