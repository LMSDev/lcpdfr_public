namespace LCPD_First_Response.LCPDFR.Scripts.Scenarios
{
    using System.Linq;

    using AdvancedHookManaged;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Scenarios;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.GUI;

    using Main = LCPD_First_Response.LCPDFR.Main;

    /// <summary>
    /// A scenario where cops will pullover another driver.
    /// </summary>
    internal class ScenarioCopPullover : Scenario, IAmbientScenario, ICanOwnEntities, IPedController
    {
        /// <summary>
        /// The cop performing the pullover.
        /// </summary>
        private CPed cop;

        /// <summary>
        /// The driver.
        /// </summary>
        private CPed driver;

        /// <summary>
        /// Whether player has approached driver for the first time.
        /// </summary>
        private bool notFirstApproach;

        /// <summary>
        /// The pullover script.
        /// </summary>
        private Pullover pullover;

        /// <summary>
        /// Whether the pullover has started already or the cop is still waiting.
        /// </summary>
        private bool started;

        /// <summary>
        /// The vehicle.
        /// </summary>
        private CVehicle vehicle;

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get
            {
                return "ScenarioCopPullover";
            }
        }

        /// <summary>
        /// Initializes the scenario.
        /// </summary>
        public override void Initialize()
        {
            if (!this.vehicle.Exists())
            {
                Log.Warning("Initialize: Vehicle disposed before scenario could start. Is the system running out of memory?", this);
                base.MakeAbortable();
                return;
            }

            // Create driver
            this.driver = new CPed(CModel.GetRandomModel(EModelFlags.IsPed | EModelFlags.IsCivilian), this.vehicle.GetOffsetPosition(new Vector3(0, 0, 10)), EPedGroup.MissionPed);
            if (this.driver.Exists())
            {
                this.driver.WarpIntoVehicle(this.vehicle, VehicleSeat.Driver);

                // Set indicator
                EStreetSide side = this.vehicle.GetSideOfStreetVehicleIsAt();
                if (side == EStreetSide.Left)
                {
                    // Set indicator light
                    this.vehicle.AVehicle.IndicatorLightsMode = VehicleIndicatorLightsMode.Blinking;
                    this.vehicle.AVehicle.IndicatorLightsOn = false;

                    this.vehicle.AVehicle.IndicatorLight(VehicleLight.LeftFront).On = true;
                    this.vehicle.AVehicle.IndicatorLight(VehicleLight.LeftRear).On = true;
                }
                else if (side == EStreetSide.Right)
                {
                    // Set indicator light
                    this.vehicle.AVehicle.IndicatorLightsMode = VehicleIndicatorLightsMode.Blinking;
                    this.vehicle.AVehicle.IndicatorLightsOn = false;

                    this.vehicle.AVehicle.IndicatorLight(VehicleLight.RightFront).On = true;
                    this.vehicle.AVehicle.IndicatorLight(VehicleLight.RightRear).On = true;
                }

                // Teleport cop car
                this.cop.CurrentVehicle.Position = this.vehicle.GetOffsetPosition(new Vector3(0, -this.vehicle.Model.GetDimensions().Y - 1.5f, 0));
                this.cop.CurrentVehicle.Heading = this.vehicle.Heading;

                if (!this.cop.CurrentVehicle.IsSeatFree(VehicleSeat.RightFront))
                {
                    this.cop.CurrentVehicle.GetPedOnSeat(VehicleSeat.RightFront).Delete();
                }

                // this.cop.AttachBlip().Friendly = true;
                // this.driver.AttachBlip().Color = BlipColor.White;

                this.cop.RequestOwnership(this);
                this.cop.GetPedData<PedDataCop>().RequestPedAction(ECopState.Blocker, this);
                this.cop.Task.Wait(-1);
                this.cop.CurrentVehicle.SirenActive = true;
            }
            else
            {
                Log.Warning("Initialize: Failed to create driver", this);
                this.MakeAbortable();
            }
        }

        /// <summary>
        /// By default frees all owned entities and deletes the base component reference
        /// </summary>
        public override void MakeAbortable()
        {
            if (this.cop.Exists())
            {
                if (this.cop.HasBlip) this.cop.DeleteBlip();
                this.cop.ReleaseOwnership(this);
                this.cop.GetPedData<PedDataCop>().ResetPedAction(this);
            }

            if (this.driver.Exists())
            {
                if (this.driver.HasBlip) this.driver.DeleteBlip();
                this.driver.ReleaseOwnership(this);
                this.driver.Intelligence.ResetAction(this);
            }

            if (this.pullover != null)
            {
                // Because End triggers pullover_OnEnd and this would call MakeAbortable again and we would be stuck in an infinite loop,
                // we null the instance here already
                Pullover copiedInstance = this.pullover;
                this.pullover = null;
                copiedInstance.End();
            }

            if (this.cop != null && this.cop.Exists())
            {
                this.cop.NoLongerNeeded();
                if (this.cop.LastVehicle != null && this.cop.LastVehicle.Exists())
                {
                    this.cop.Task.CruiseWithVehicle(this.cop.LastVehicle, 17f, true);
                }
            }

            base.MakeAbortable();
        }

        /// <summary>
        /// Processes the scenario logic.
        /// </summary>
        public override void Process()
        {
            if (this.cop.Exists() && !this.cop.IsAliveAndWell)
            {
                // Only end when pullover not yet started or is not a pursuit
                if (this.pullover == null || !this.pullover.IsPursuit)
                {
                    this.MakeAbortable();
                }
            }

            if (this.driver.Exists() && !this.driver.IsAliveAndWell)
            {
                // Only end when pullover not yet started or is not a pursuit
                if (this.pullover == null || !this.pullover.IsPursuit)
                {
                    this.MakeAbortable();
                }
            }

            if (!this.started && this.driver.Exists() && this.cop.Exists())
            {
                if (CPlayer.LocalPlayer.HasVisualOnSuspect(this.driver) || CPlayer.LocalPlayer.HasVisualOnSuspect(this.cop))
                {
                    // Free cop as pullover will request ownership
                    this.cop.ReleaseOwnership(this);
                    this.cop.GetPedData<PedDataCop>().ResetPedAction(this);

                    this.pullover = new Pullover(this.cop, this.vehicle, true);
                    this.pullover.OnEnd += this.pullover_OnEnd;
                    Main.ScriptManager.RegisterScriptInstance(this.pullover);
                    this.started = true;
                }
            }

            if (!this.notFirstApproach)
            {
                this.notFirstApproach = CameraHelper.PerformEventFocus(this.cop, true, 1000, 3500, true, false, true);
            }

            if (this.pullover != null)
            {
                if (this.pullover.SuspectLeftVehicle)
                {
                    if (this.driver.Exists() && !this.driver.IsInVehicle)
                    {
                        // Request cop and start arresting
                        if (this.cop.GetPedData<PedDataCop>().RequestPedAction(ECopState.Blocker, this))
                        {
                            this.cop.RequestOwnership(this);
                            this.cop.BlockPermanentEvents = false;
                            this.cop.Invincible = true;

                            this.cop.SayAmbientSpeech(this.cop.VoiceData.ArrestSpeech);
                            this.driver.Intelligence.RequestForAction(EPedActionPriority.RequiredByScript, this);
                            this.driver.RequestOwnership(this);
                            this.driver.Task.ClearAll();
                            //this.driver.PedData.DontAllowEmptyVehiclesAsTransporter = true;
                            this.driver.PedData.CanBeArrestedByPlayer = false;

                            TaskBustPed taskBustPed = new TaskBustPed(this.driver);
                            taskBustPed.AssignTo(this.cop, ETaskPriority.MainTask);
                        }
                    }

                    // Wait until suspect is cuffed
                    if (this.driver.Exists())
                    {
                        // Prefer last vehicle of arresting cop as suspect transporter
                        if (this.driver.Intelligence.TaskManager.IsTaskActive(ETaskID.BeingBusted))
                        {
                            TaskBeingBusted taskBeingBusted = this.driver.Intelligence.TaskManager.FindTaskWithID(ETaskID.BeingBusted) as TaskBeingBusted;
                            if (!taskBeingBusted.HasVehicle)
                            {
                                if (this.cop.LastVehicle != null && this.cop.LastVehicle.Exists() && this.cop.LastVehicle.IsDriveable)
                                {
                                    taskBeingBusted.SetVehicleToUse(this.cop.LastVehicle);
                                }
                            }
                        }

                        if (this.driver.Wanted.IsCuffed)
                        {
                            if (!this.driver.Wanted.HasBeenArrested)
                            {
                                if (!this.cop.Intelligence.TaskManager.IsTaskActive(ETaskID.ArrestPed))
                                {
                                    TaskArrestPed taskArrestPed = new TaskArrestPed(this.driver, 5f, 10000);
                                    taskArrestPed.AssignTo(this.cop, ETaskPriority.MainTask);
                                }
                            }
                            else
                            {
                                this.MakeAbortable();
                            }
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
            // We at least need a cop vehicle
            CPed[] peds = CPed.GetPedsAround(300f, EPedSearchCriteria.CopsOnly, position);
            foreach (CPed ped in peds.Where(ped => ped.IsInVehicle && ped.IsDriver))
            {
                this.cop = ped;
                break;
            }

            if (this.cop != null)
            {
                // Now look for an already parked civilian vehicle
                CVehicle[] vehicles = this.cop.Intelligence.GetVehiclesAround(300f, EVehicleSearchCriteria.NoDriverOnly | EVehicleSearchCriteria.StoppedOnly | EVehicleSearchCriteria.NoCop | EVehicleSearchCriteria.NoPlayersLastVehicle);
                foreach (CVehicle vehicle in vehicles)
                {
                    // Not seen by player and not too close
                    if (!CPlayer.LocalPlayer.HasVisualOnSuspect(this.cop)
                        && vehicle.Position.DistanceTo(CPlayer.LocalPlayer.Ped.Position) > 70f)
                    {
                        // Not the subway, boats or helicopters, but only vehicles (which includes bikes)
                        if (vehicle.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsVehicle))
                        {
                            this.vehicle = vehicle;
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
            // Dispose depending on state
            if (this.pullover != null && this.pullover.IsPursuit)
            {
                if (this.driver.PedData.CurrentChase != null)
                {
                    if (!this.driver.PedData.CurrentChase.IsPlayersChase)
                    {
                        Pursuit pursuit = this.driver.PedData.CurrentChase as Pursuit;
                        if (pursuit != null)
                        {
                            CPed closestSuspect = pursuit.GetClosestSuspectForPlayer();
                            if (closestSuspect != null && closestSuspect.Exists()
                                && closestSuspect.Position.DistanceTo(CPlayer.LocalPlayer.Ped.Position) > 500)
                            {
                                this.MakeAbortable();
                                return true;
                            }
                        }
                    }
                }
            }
            else
            {
                if (this.driver.Exists() && this.driver.Position.DistanceTo(CPlayer.LocalPlayer.Ped.Position) > 200)
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
            this.driver.ReleaseOwnership(this);
            this.cop.ReleaseOwnership(this);

            if (this.driver.Intelligence.IsStillAssignedToController(this))
            {
                this.driver.Intelligence.ResetAction(this);
            }

            if (this.cop.GetPedData<PedDataCop>().IsPedStillUseable(this))
            {
                this.cop.GetPedData<PedDataCop>().ResetPedAction(this);
            }
        }

        /// <summary>
        /// Called when pullover ended.
        /// </summary>
        /// <param name="sender">The sender</param>
        void pullover_OnEnd(object sender)
        {
            // This is only the case if MakeAbortable has already been called to prevent it from being called again
            if (this.pullover == null)
            {
                return;
            }

            // If suspect left vehicle, don't end yet (because we are arresting)
            if (!this.pullover.SuspectLeftVehicle)
            {
                this.MakeAbortable();
            }

            // However if either suspect or cop is dead, end
            if ((this.cop.Exists() && !this.cop.IsAliveAndWell) || (this.driver.Exists() && !this.driver.IsAliveAndWell))
            {
                this.MakeAbortable();
            }
        }
    }
}