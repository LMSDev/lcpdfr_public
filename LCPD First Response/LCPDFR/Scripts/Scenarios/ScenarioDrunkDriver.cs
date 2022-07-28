namespace LCPD_First_Response.LCPDFR.Scripts.Scenarios
{
    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Native;
    using LCPD_First_Response.Engine.Scripting.Scenarios;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.GUI;

    /// <summary>
    /// The drunk driver scenario.
    /// </summary>
    internal class ScenarioDrunkDriver : Scenario, IAmbientScenario, ICanOwnEntities, IPedController
    {
        /// <summary>
        /// Whether player has approached driver for the first time.
        /// </summary>
        private bool notFirstApproach;

        /// <summary>
        /// Whether the scenario is hosted by the drunk driver callout.
        /// </summary>
        private bool isHostedByCallout;

        /// <summary>
        /// The ped.
        /// </summary>
        private CPed ped;

        /// <summary>
        /// The vehicle.
        /// </summary>
        private CVehicle vehicle;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScenarioDrunkDriver"/> class.
        /// </summary>
        public ScenarioDrunkDriver()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScenarioDrunkDriver"/> class.
        /// </summary>
        /// <param name="ped">
        /// The ped to use.
        /// </param>
        public ScenarioDrunkDriver(CPed ped)
        {
            this.ped = ped;
            this.isHostedByCallout = true;
        }

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get
            {
                return "ScenarioDrunkDriver";
            }
        }

        /// <summary>
        /// This is called immediately before the scenario is executed the first time.
        /// </summary>
        public override void Initialize()
        {
            // If not hosted by callout, claim all resources
            if (!this.isHostedByCallout)
            {
                this.ped.RequestOwnership(this);
                this.ped.Intelligence.RequestForAction(EPedActionPriority.RequiredByScript, this);
                // this.ped.AttachBlip().Color = BlipColor.White;
            }

            if (this.ped.IsInVehicle)
            {
                this.vehicle = this.ped.CurrentVehicle;
            }
            else
            {
                Log.Debug("Initialize: Ped not in vehicle", this);
                this.MakeAbortable();
            }

            this.ped.BlockPermanentEvents = true;
        }

        /// <summary>
        /// By default frees all owned entities and deletes the base component reference
        /// </summary>
        public override void MakeAbortable()
        {
            base.MakeAbortable();

            if (this.ped != null && this.ped.Exists())
            {
                if (this.ped.Intelligence.TaskManager.IsTaskActive(ETaskID.DriveDrunk))
                {
                    TaskDriveDrunk taskDriveDrunk = this.ped.Intelligence.TaskManager.FindTaskWithID(ETaskID.DriveDrunk) as TaskDriveDrunk;
                    taskDriveDrunk.MakeAbortable(this.ped);
                }

                if (this.ped.Intelligence.TaskManager.IsTaskActive(ETaskID.WalkDrunk))
                {
                    TaskWalkDrunk taskWalkDrunk = this.ped.Intelligence.TaskManager.FindTaskWithID(ETaskID.WalkDrunk) as TaskWalkDrunk;
                    taskWalkDrunk.MakeAbortable(this.ped);
                }

                if (this.ped.Intelligence.IsStillAssignedToController(this))
                {
                    this.ped.Intelligence.TaskManager.ClearTasks();
                    this.ped.NoLongerNeeded();
                }
                else
                {
                    // this.ped.DeleteBlip();
                }
            }
        }

        /// <summary>
        /// Processes the scenario logic.
        /// </summary>
        public override void Process()
        {
            // Drunk until he has been arrested
            if (this.ped.Wanted.HasBeenArrested)
            {
                this.MakeAbortable();
                return;
            }

            if (this.ped == null || !this.ped.Exists() || !this.ped.IsAliveAndWell)
            {
                this.MakeAbortable();
                return;
            }

            if (!this.notFirstApproach)
            {
                this.notFirstApproach = CameraHelper.PerformEventFocus(this.ped, true, 1000, 3500, true, false, true);
            }

            if (this.ped.IsInVehicle())
            {
                // If ped is owned by pullover, don't drive drunk
                if (this.ped.HasOwner && this.ped.Owner.GetType() == typeof(Pullover))
                {
                    // Cancel task for now
                    if (this.ped.Intelligence.TaskManager.IsTaskActive(ETaskID.DriveDrunk))
                    {
                        TaskDriveDrunk taskDriveDrunk = this.ped.Intelligence.TaskManager.FindTaskWithID(ETaskID.DriveDrunk) as TaskDriveDrunk;
                        taskDriveDrunk.MakeAbortable(this.ped);
                    }
                }
                else
                {
                    if (!this.ped.Intelligence.TaskManager.IsTaskActive(ETaskID.DriveDrunk))
                    {
                        TaskDriveDrunk taskDriveDrunk = new TaskDriveDrunk();
                        taskDriveDrunk.AssignTo(this.ped, ETaskPriority.MainTask);

                        Log.Debug("Process: Drunk driving task assigned", this);
                    }
                }
            }
            else
            {
                // On foot, make driver walk drunk
                if (!this.ped.Intelligence.TaskManager.IsTaskActive(ETaskID.WalkDrunk))
                {
                    TaskWalkDrunk taskWalkDrunk = new TaskWalkDrunk();
                    taskWalkDrunk.AssignTo(this.ped, ETaskPriority.MainTask);
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
            // Look for vehicles with driver
            CVehicle[] vehicles = CVehicle.GetVehiclesAround(130f, EVehicleSearchCriteria.DriverOnly | EVehicleSearchCriteria.NoCop | EVehicleSearchCriteria.NoPlayersLastVehicle, position);
            foreach (CVehicle vehicle in vehicles)
            {
                // If not yet seen, make use of it
                if (vehicle.Driver != null && vehicle.Driver.Exists() && !CPlayer.LocalPlayer.HasVisualOnSuspect(vehicle.Driver) && 
                    vehicle.Position.DistanceTo(CPlayer.LocalPlayer.Ped.Position) > 50f && vehicle.Driver.IsAliveAndWell)
                {
                    // Not the subway, boats or helicopters, but only vehicles (which includes bikes)
                    if (vehicle.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsVehicle) && !vehicle.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsEmergencyServicesVehicle) && 
                        !vehicle.Driver.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.HasJob))
                    {
                        // Driver must be idle.
                        if (vehicle.Driver.Intelligence.IsFreeForAction(EPedActionPriority.RequiredByScript))
                        {
                            this.ped = vehicle.Driver;
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
            if (this.isHostedByCallout)
            {
                return false;
            }

            // Only if ped has not been arrested yet
            if (this.ped.Exists() && !this.ped.Wanted.HasBeenArrested && this.ped.Position.DistanceTo(CPlayer.LocalPlayer.Ped.Position) > 300)
            {
                this.MakeAbortable();
                return true;
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
            // Only free the ped here, but not end the actions
            this.ped.ReleaseOwnership(this);
            this.ped.Intelligence.ResetAction(this);
        }
    }
}