namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using GTA;

    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Timers;

    /// <summary>
    /// Makes a ped leave its current position by either walking away or by getting into the last vehicle.
    /// This task frees the specified ped!
    /// </summary>
    internal class TaskLeaveScene : PedTask
    {
        /// <summary>
        /// The vehicle to use.
        /// </summary>
        private CVehicle vehicle;

        /// <summary>
        /// Whether the ped should use a vehicle.
        /// </summary>
        private bool useVehicle;

        /// <summary>
        /// Whether the ped entered a vehicle as driver.
        /// </summary>
        private bool enteredAsDriver;

        /// <summary>
        /// The get in vehicle task.
        /// </summary>
        private TaskGetInVehicle taskGetInVehicle;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskLeaveScene"/> class.
        /// </summary>
        public TaskLeaveScene() : base(ETaskID.LeaveScene)
        {
        }

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get
            {
                return "TaskLeaveScene";
            }
        }

        /// <summary>
        /// Called when the task should be aborted. Remember to call SetTaskAsDone here!
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void MakeAbortable(CPed ped)
        {
            if (this.taskGetInVehicle != null && this.taskGetInVehicle.Active)
            {
                this.taskGetInVehicle.MakeAbortable(ped);
            }

            this.SetTaskAsDone();
        }

        /// <summary>
        /// This is called immediately after the task was assigned to a ped.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void Initialize(CPed ped)
        {
            base.Initialize(ped);

            if (ped.LastVehicle != null && ped.LastVehicle.Exists() && ped.LastVehicle.IsDriveable && ped.LastVehicle.IsAlive)
            {
                this.vehicle = ped.LastVehicle;
                this.useVehicle = true;
            }
        }

        /// <summary>
        /// Processes the task logic.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void Process(CPed ped)
        {
            if (this.useVehicle)
            {
                // If vehicle ceased to exists, walk away
                if (!this.vehicle.Exists())
                {
                    this.useVehicle = false;
                    return;
                }

                // If in combat, pause
                if (ped.IsInCombat)
                {
                    return;
                }

                // If not in vehicle yet
                if (!ped.IsInVehicle)
                {
                    // Vehicle must not be a suspect transporter and must be available
                    if (!this.vehicle.IsSuspectTransporter)
                    {
                        if (!this.vehicle.IsRequiredForMission && !ped.Intelligence.IsVehicleBlacklisted(this.vehicle))
                        {
                            // If vehicle has no driver, enter as driver
                            if (!this.vehicle.HasDriver)
                            {
                                if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.GetInVehicle))
                                {
                                    this.taskGetInVehicle = new TaskGetInVehicle(this.vehicle, VehicleSeat.Driver,  VehicleSeat.Driver, false);
                                    this.taskGetInVehicle.AssignTo(ped, ETaskPriority.MainTask);
                                    this.enteredAsDriver = true;
                                }
                                //if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexNewGetInVehicle))
                                //{
                                //    this.enteredAsDriver = true;
                                //    ped.Task.EnterVehicle(ped.LastVehicle, VehicleSeat.Driver);
                                //}
                            }
                            else
                            {
                                // Vehicle has driver, enter as passenger
                                if (ped.IsGettingIntoAVehicle && this.enteredAsDriver)
                                {
                                    this.enteredAsDriver = false;
                                    ped.Task.ClearAll();

                                    if (ped.Intelligence.TaskManager.IsTaskActive(ETaskID.GetInVehicle))
                                    {
                                        this.taskGetInVehicle = (TaskGetInVehicle)ped.Intelligence.TaskManager.FindTaskWithID(ETaskID.GetInVehicle);
                                        this.taskGetInVehicle.MakeAbortable(ped);
                                    }
                                }

                                if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.GetInVehicle))
                                {
                                    this.taskGetInVehicle = new TaskGetInVehicle(this.vehicle, VehicleSeat.RightFront, VehicleSeat.AnyPassengerSeat, false);
                                    this.taskGetInVehicle.AssignTo(ped, ETaskPriority.MainTask);
                                }
                                //if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexNewGetInVehicle))
                                //{
                                //    ped.Task.EnterVehicle(ped.LastVehicle, VehicleSeat.AnyPassengerSeat);
                                //}
                            }
                        }
                        else
                        {
                            Log.Debug("Process: Vehicle either a mission vehicle or blacklisted by AI", this);
                            this.useVehicle = false;

                            DelayedCaller.Call(delegate { this.useVehicle = true; }, this, 1500);
                        }
                    }
                    else
                    {
                        this.useVehicle = false;
                    }
                }
                else
                {
                    // Cruise away
                    ped.NoLongerNeeded();
                }
            }
            else
            {
                // Walk away
                ped.NoLongerNeeded();
            }
        }
    }
}