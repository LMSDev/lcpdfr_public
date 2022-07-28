namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using GTA;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Native;
    using LCPD_First_Response.Engine.Timers;

    /// <summary>
    /// Provides advanced parameter for getting into a vehicle.
    /// </summary>
    internal class TaskGetInVehicle : PedTask
    {
        /// <summary>
        /// Whether weapons are allowed.
        /// </summary>
        private bool allowWeapons;

        /// <summary>
        /// Whether weapon has been fired.
        /// </summary>
        private bool hasFired;

        /// <summary>
        /// Range to abort the task.
        /// </summary>
        private float rangeToAbort;

        /// <summary>
        /// The seat to use.
        /// </summary>
        private VehicleSeat seat;

        /// <summary>
        /// The alternative seat.
        /// </summary>
        private VehicleSeat alternativeSeat;

        /// <summary>
        /// Whether the enter vehicle task was just assigned.
        /// </summary>
        private bool justAssigned;

        /// <summary>
        /// Whether the seat has just been changed to alternative seat.
        /// </summary>
        private bool justChangedSeat;

        /// <summary>
        /// Whether the movement task has been assigned.
        /// </summary>
        private bool movementAssigned;

        /// <summary>
        /// Whether the ped has been placed in the vehicle successfully.
        /// </summary>
        private bool successfulInVehicle;
        
        /// <summary>
        /// Whether the ped uses the alternative seat because of path finding errors.
        /// </summary>
        private bool usesAlternative;

        /// <summary>
        /// Whether to warp the ped into the vehicle when entering fails.
        /// </summary>
        private bool warpWhenCantEnter;

        /// <summary>
        /// Whether seat can only be used if free.
        /// </summary>
        private bool freeSeatsOnly;

        /// <summary>
        /// The target vehicle.
        /// </summary>
        private CVehicle vehicle;

        /// <summary>
        /// Timer to check how long cuffing is taking and to teleport the ped if necessary.
        /// </summary>
        private NonAutomaticTimer maximumDurationTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskGetInVehicle"/> class.
        /// </summary>
        /// <param name="vehicle">
        /// The vehicle.
        /// </param>
        /// <param name="allowWeapons">
        /// The allow weapons.
        /// </param>
        /// <param name="rangeToAbort">
        /// The range to abort.
        /// </param>
        public TaskGetInVehicle(CVehicle vehicle, bool allowWeapons, float rangeToAbort) : base(ETaskID.GetInVehicle)
        {
            this.vehicle = vehicle;
            this.allowWeapons = allowWeapons;
            this.rangeToAbort = rangeToAbort;
            this.maximumDurationTimer = new NonAutomaticTimer(20000, ETimerOptions.OneTimeReturnTrue);
            this.seat = VehicleSeat.Driver;
            this.EnterStyle = EPedMoveState.Walk;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskGetInVehicle"/> class.
        /// </summary>
        /// <param name="vehicle">
        /// The vehicle.
        /// </param>
        /// <param name="seat">
        /// The seat.
        /// </param>
        /// <param name="alternativeSeat">
        /// The alternative seat.
        /// </param>
        /// <param name="warpWhenCantEnter">
        /// Whether the ped should be warped into the vehicle if entering fails.
        /// </param>
        public TaskGetInVehicle(CVehicle vehicle, VehicleSeat seat, VehicleSeat alternativeSeat, bool warpWhenCantEnter) : base(ETaskID.GetInVehicle)
        {
            this.vehicle = vehicle;
            this.seat = seat;
            this.alternativeSeat = alternativeSeat;
            this.warpWhenCantEnter = warpWhenCantEnter;
            this.rangeToAbort = float.MaxValue;
            this.maximumDurationTimer = new NonAutomaticTimer(20000, ETimerOptions.OneTimeReturnTrue);
            this.EnterStyle = EPedMoveState.Walk;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskGetInVehicle"/> class.
        /// </summary>
        /// <param name="vehicle">
        /// The vehicle.
        /// </param>
        /// <param name="seat">
        /// The seat.
        /// </param>
        /// <param name="alternativeSeat">
        /// The alternative seat.
        /// </param>
        /// <param name="warpWhenCantEnter">
        /// The warp when cant enter.
        /// </param>
        /// <param name="freeSeatsOnly">
        /// The free seats only.
        /// </param>
        public TaskGetInVehicle(CVehicle vehicle, VehicleSeat seat, VehicleSeat alternativeSeat, bool warpWhenCantEnter, bool freeSeatsOnly) : base(ETaskID.GetInVehicle)
        {
            this.vehicle = vehicle;
            this.seat = seat;
            this.alternativeSeat = alternativeSeat;
            this.warpWhenCantEnter = warpWhenCantEnter;
            this.freeSeatsOnly = freeSeatsOnly;
            this.rangeToAbort = float.MaxValue;
            this.maximumDurationTimer = new NonAutomaticTimer(20000, ETimerOptions.OneTimeReturnTrue);
            this.EnterStyle = EPedMoveState.Walk;
        }

        /// <summary>
        /// Gets or sets the style for entering the vehicle. <see cref="Native.EPedMoveState.Walk"/> by default.
        /// </summary>
        public EPedMoveState EnterStyle { get; set; }

        /// <summary>
        /// Gets the desired seat.
        /// </summary>
        public VehicleSeat Seat
        {
            get
            {
                return this.seat;
            }
        }

        /// <summary>
        /// Gets the desired vehicle.
        /// </summary>
        public CVehicle Vehicle
        {
            get
            {
                return this.vehicle;
            }
        }

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get { return "TaskGetInVehicle"; }
        }

        /// <summary>
        /// Called when the task should be aborted. Remember to call SetTaskAsDone here!
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void MakeAbortable(CPed ped)
        {
            if (ped.Exists() && !this.successfulInVehicle)
            {
                ped.Task.ClearAll();
            }

            this.SetTaskAsDone();
        }

        /// <summary>
        /// Processes the task logic.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void Process(CPed ped)
        {
            if (!this.vehicle.Exists())
            {
                this.MakeAbortable(ped);
                return;
            }

            // Check if vehicle is too far away
            if (this.vehicle.Position.DistanceTo(ped.Position) > this.rangeToAbort)
            {
                this.MakeAbortable(ped);
                return;
            }

            // If already in vehicle, abort
            if (ped.IsInVehicle(this.vehicle))
            {
                this.successfulInVehicle = true;
                this.MakeAbortable(ped);
                return;
            }

            // If vehicle has driver and weapons are allowed
            if (this.vehicle.HasDriver && this.allowWeapons)
            {
                bool inCombat = ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCombat);

                CPed driver = this.vehicle.Driver;

                // If driver is valid and alive and ped has not fired yet
                if (driver != null && driver.Exists() && driver.IsAliveAndWell && !this.hasFired)
                {
                    // If not already fighting  
                    if (!inCombat)
                    {
                        ped.EnsurePedHasWeapon();
                        ped.Task.FightAgainst(driver, 4000);
                        DelayedCaller.Call(delegate { this.hasFired = true; }, 1000);
                    }
                }
                else
                {
                    if (!inCombat)
                    {
                        this.EnterVehicle(ped);
                    }
                }
            }
            // If not, simply enter
            else
            {
                this.EnterVehicle(ped);
            }
        }

        /// <summary>
        /// The vehicle enter process.
        /// </summary>
        /// <param name="ped">
        /// The ped.
        /// </param>
        private void EnterVehicle(CPed ped)
        {
            // If taking too long, warp ped in vehicle
            if (this.maximumDurationTimer.CanExecute())
            {
                if (this.warpWhenCantEnter)
                {
                    this.WarpPed(ped);

                    // When ped is still not in vehicle, abort task
                    if (!ped.IsInVehicle(this.vehicle))
                    {
                        Log.Debug("EnterVehicle: Failed to warp ped into vehicle. No seat available or blocked", this);
                        ped.Intelligence.AddVehicleToBlacklist(this.vehicle, 10000);
                        this.MakeAbortable(ped);
                        return;
                    }

                    this.MakeAbortable(ped);
                    return;
                }
                else
                {
                    Log.Debug("EnterVehicle: Failed to enter vehicle, task timed out", this);
                    ped.Intelligence.AddVehicleToBlacklist(this.vehicle, 10000);
                    this.MakeAbortable(ped);
                    return;
                }
            }

            if (ped.Position.DistanceTo(this.vehicle) > 12)
            {
                if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexControlMovement) || !this.movementAssigned)
                {
                    ped.SetNextDesiredMoveState(this.EnterStyle);
                    ped.Task.RunTo(this.vehicle.Position);
                    this.movementAssigned = true;
                }
            }
            else if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexNewGetInVehicle))
            {
                // If task was just assigned, wait a short moment. This is because the enter vehicle task sometimes bugs for a short time
                if (this.justAssigned)
                {
                    return;
                }

                // If seat check is activated, make sure seat is free and can be entered. If not, simply check if vehicle can be entered
                if ((this.freeSeatsOnly && this.vehicle.IsSeatFree(this.seat) && ped.CanEnterVehicle(this.vehicle, this.seat)) || (!this.freeSeatsOnly && ped.CanEnterVehicle(this.vehicle, this.seat)))
                {
                    ped.SetNextDesiredMoveState(this.EnterStyle);
                    ped.Task.EnterVehicle(this.vehicle, this.seat);
                    this.justAssigned = true;
                    DelayedCaller.Call(delegate { this.justAssigned = false; }, this, 500);
                }
                else if ((this.freeSeatsOnly && this.vehicle.IsSeatFree(this.alternativeSeat) && ped.CanEnterVehicle(this.vehicle, this.alternativeSeat)) || (!this.freeSeatsOnly && ped.CanEnterVehicle(this.vehicle, this.alternativeSeat)))
                {
                    ped.SetNextDesiredMoveState(this.EnterStyle);
                    ped.Task.EnterVehicle(this.vehicle, this.alternativeSeat);
                    this.justAssigned = true;
                    DelayedCaller.Call(delegate { this.justAssigned = false; }, this, 500);
                }
                else
                {
                    Log.Debug("EnterVehicle: No path available to enter car or no seats available", "TasKGetInVehicle");
                    if (this.warpWhenCantEnter)
                    {
                        this.WarpPed(ped);
                    }
                    else
                    {
                        Log.Debug("EnterVehicle: Pathfind error", "TasKGetInVehicle");

                        // Blacklist vehicle and abort
                        ped.Intelligence.AddVehicleToBlacklist(this.vehicle, 10000);
                        this.MakeAbortable(ped);
                    }
                }
            }
            else
            {
                // Ensure ped isn't stuck
                if (ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleHitWall))
                {
                    Log.Debug("EnterVehicle: Hit wall", this);

                    if (!this.usesAlternative)
                    {
                        Log.Debug("EnterVehicle: Using alternative seat", this);
                        this.seat = this.alternativeSeat;
                        this.usesAlternative = true;

                        // Since the hit wall task will remain for a short time even though we changed seats, we block the code below from being executed for 1.5seconds
                        this.justChangedSeat = true;
                        DelayedCaller.Call(delegate { this.justChangedSeat = false; }, this, 1500);
                    }
                    else if (!this.justChangedSeat)
                    {
                        Log.Debug("Failed to reach seat", this);

                        // Blacklist vehicle and abort
                        ped.Intelligence.AddVehicleToBlacklist(this.vehicle, 10000);
                        this.MakeAbortable(ped);
                    }
                }

                float ground0 = World.GetGroundZ(ped.Position, GroundType.Closest);
                float ground1 = World.GetGroundZ(this.vehicle.Position, GroundType.Closest);
                float diff = ground0 - ground1;

                if (diff < 0)
                {
                    diff *= -1;
                }

                if (diff > 5)
                {
                    Log.Warning("EnterVehicle: Aborted due to vehicle probably being above the ped and unreachable", this);

                    // Blacklist vehicle and abort
                    ped.Intelligence.AddVehicleToBlacklist(this.vehicle, 10000);
                    this.MakeAbortable(ped);
                }
            }
        }

        /// <summary>
        /// Warps the ped into the vehicle.
        /// </summary>
        /// <param name="ped">The ped.</param>
        private void WarpPed(CPed ped)
        {
            // Ensure seats are free
            if (this.freeSeatsOnly && !this.vehicle.IsSeatFree(this.seat))
            {
                if (this.vehicle.IsSeatFree(this.alternativeSeat))
                {
                    ped.WarpIntoVehicle(this.vehicle, this.alternativeSeat);
                }
                else
                {
                    return;
                }
            }
            else
            {
                ped.WarpIntoVehicle(this.vehicle, this.seat);
            }

            if (!this.freeSeatsOnly)
            {
                ped.WarpIntoVehicle(this.vehicle, this.seat);
            }

            Log.Debug("EnterVehicle: Ped warped", "TasKGetInVehicle");
        }
    }
}