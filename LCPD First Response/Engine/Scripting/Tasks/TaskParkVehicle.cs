namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using AdvancedHookManaged;

    using GTA;

    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Native;

    using Timer = LCPD_First_Response.Engine.Timers.NonAutomaticTimer;

    /// <summary>
    /// The parking style. Note that it can't be guaranteed that the vehicle is parked properly, but depending on the style the best parameters are chosen.
    /// </summary>
    internal enum EVehicleParkingStyle
    {
        /// <summary>
        /// Right side of the road.
        /// </summary>
        RightSideOfRoad,
        
        /// <summary>
        /// Right side of the road and the left front wheel is already on the pavement.
        /// </summary>
        RightSideOfRoadLeftFrontWheelOnPavement,

        /// <summary>
        /// Right side of road, the right wheels are on the pavement.
        /// </summary>
        RightSideOfRoadOnPavement,
    }

    /// <summary>
    /// Task to park a vehicle (useful for e.g. pullover).
    /// </summary>
    internal class TaskParkVehicle : PedTask
    {
        /// <summary>
        /// Whether or not, the direction has been computed.
        /// </summary>
        private bool directionSet;

        /// <summary>
        /// Whether the mirror animation has finished.
        /// </summary>
        private bool hasAnimFinished;

        /// <summary>
        /// Whether the cruising task has been assigned to speed up the vehicle.
        /// </summary>
        private bool hasCruisingTaskAssigned;

        /// <summary>
        /// Going left.
        /// </summary>
        private bool left;

        /// <summary>
        /// The heading of the vehicle at start.
        /// </summary>
        private float startHeading;

        /// <summary>
        /// The parking style.
        /// </summary>
        private EVehicleParkingStyle parkingStyle; 

        /// <summary>
        /// The state.
        /// </summary>
        private ETaskParkVehicleState state;

        /// <summary>
        /// Timer to wait before terminating.
        /// </summary>
        private Timers.NonAutomaticTimer waitTimer;

        /// <summary>
        /// The vehicle.
        /// </summary>
        private CVehicle vehicle;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskParkVehicle"/> class.
        /// </summary>
        /// <param name="vehicle">
        /// The vehicle.
        /// </param>
        /// <param name="parkingStyle">
        /// The parking Style.
        /// </param>
        public TaskParkVehicle(CVehicle vehicle, EVehicleParkingStyle parkingStyle) : base(ETaskID.ParkVehicle)
        {
            this.vehicle = vehicle;
            this.state = ETaskParkVehicleState.None;
            this.parkingStyle = parkingStyle;
            this.waitTimer = new Timers.NonAutomaticTimer(1000);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskParkVehicle"/> class.
        /// </summary>
        /// <param name="vehicle">
        /// The vehicle.
        /// </param>
        /// <param name="parkingStyle">
        /// The parking Style.
        /// </param>
        /// <param name="timeOut">
        /// The time out.
        /// </param>
        public TaskParkVehicle(CVehicle vehicle, EVehicleParkingStyle parkingStyle, int timeOut) : base(ETaskID.ParkVehicle, timeOut)
        {
            this.vehicle = vehicle;
            this.state = ETaskParkVehicleState.None;
            this.parkingStyle = parkingStyle;
            this.waitTimer = new Timers.NonAutomaticTimer(1000);
        }

        /// <summary>
        /// State of the park vehicle task.
        /// </summary>
        private enum ETaskParkVehicleState
        {
            /// <summary>
            /// No state.
            /// </summary>
            None,

            /// <summary>
            /// Look in mirror before slowing down.
            /// </summary>
            LookInMirror,

            /// <summary>
            /// Slow down before going left/right.
            /// </summary>
            SlowingDown,

            /// <summary>
            /// Driving to the right.
            /// </summary>
            DrivingToTheRight,

            /// <summary>
            /// Driving forwards.
            /// </summary>
            DrivingForwards,

            /// <summary>
            /// Driving on pavement.
            /// </summary>
            DrivingOnPavement,

            /// <summary>
            /// Before terminating the task, wait a little
            /// </summary>
            Wait,
        }

        /// <summary>
        /// Gets or sets a value indicating whether the indicator lights should be cleared when parking has finished.
        /// </summary>
        public bool DontClearIndicatorLightsWhenFinished { get; set; }

        /// <summary>
        /// Gets a value indicating whether the vehicle has been parked successfully.
        /// </summary>
        public bool HasBeenParkedSuccessfully { get; private set; }

        /// <summary>
        /// The component name.
        /// </summary>
        public override string ComponentName
        {
            get
            {
                return "TaskParkVehicle";
            }
        }

        /// <summary>
        /// Called when the task should be aborted. Remember to call SetTaskAsDone here!
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void MakeAbortable(CPed ped)
        {
            if (!this.DontClearIndicatorLightsWhenFinished)
            {
                this.vehicle.AVehicle.IndicatorLightsOn = false;
                this.vehicle.AVehicle.IndicatorLightsMode = VehicleIndicatorLightsMode.Off;
            }

            SetTaskAsDone();
        }

        /// <summary>
        /// Processes the task logic.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void Process(CPed ped)
        {
            if (!this.vehicle.Exists())
            {
                Log.Warning("Process: Vehicle disposed while still being in use", this);
                this.MakeAbortable(ped);
                return;
            }

            if (!ped.IsSittingInVehicle(this.vehicle))
            {
                Log.Debug("Process: No longer in vehicle", this);
                this.MakeAbortable(ped);
                return;
            }

            switch (this.state)
            {
                case ETaskParkVehicleState.None:
                    Log.Debug("None", this);

                    // Mirror anim has been removed for now, because it stops the vehicle
                    this.state = ETaskParkVehicleState.LookInMirror;
                    this.hasAnimFinished = true;
                    break;

                case ETaskParkVehicleState.LookInMirror:
                    if (ped.Animation.isPlaying(new AnimationSet("amb@car_std_ds_a"), "mirror_a") || this.vehicle.Model.IsBike || this.hasAnimFinished)
                    {
                        this.hasAnimFinished = true;

                        float time = ped.Animation.GetCurrentAnimationTime(new AnimationSet("amb@car_std_ds_a"), "mirror_a");

                        if (time > 0.35f || this.vehicle.Model.IsBike || this.hasAnimFinished)
                        {
                            // If vehicle is too slow, the parking task will fail, so we speed up a little in case it's needed
                            if (this.vehicle.Speed < 6)
                            {
                                if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCarDriveWander) || !this.hasCruisingTaskAssigned)
                                {
                                    ped.Task.CruiseWithVehicle(this.vehicle, 8f, true);
                                    this.hasCruisingTaskAssigned = true;
                                }
                            }
                            else
                            {
                                if (this.vehicle.Speed > 6)
                                {
                                    ped.Task.CarTempAction(ECarTempActionType.SlowDownSoftlyThenBackwards, 20000);
                                }

                                this.state = ETaskParkVehicleState.SlowingDown;
                            }
                        }
                    }

                    // If stuck, proceed
                    if (this.vehicle.IsStuck(15000))
                    {
                        Log.Debug("Process: LookInMirror - Vehicle is stuck", this);
                        this.state = ETaskParkVehicleState.SlowingDown;
                    }

                    break;


                case ETaskParkVehicleState.SlowingDown:
                    // Game.DisplayText(this.vehicle.Speed.ToString());
                    if (this.vehicle.Speed < 7)
                    {
                        ped.Task.ClearAll();
                        this.startHeading = this.vehicle.Heading;
                        ped.Task.CarMission(this.vehicle, 0, EVehicleDrivingStyle.ParkToTheRight, 7f, 4, 3, 3);
                        this.state = ETaskParkVehicleState.DrivingToTheRight;
                    }
                    break;

                case ETaskParkVehicleState.DrivingToTheRight:
                    if (!this.directionSet)
                    {
                        EStreetSide streetSide = this.vehicle.GetSideOfStreetVehicleIsAt();

                        if (streetSide == EStreetSide.Left)
                        {
                            Log.Debug("Going left", this);
                            this.left = true;

                            // Set indicator light
                            this.vehicle.AVehicle.IndicatorLightsMode = VehicleIndicatorLightsMode.Blinking;
                            this.vehicle.AVehicle.IndicatorLightsOn = false;

                            this.vehicle.AVehicle.IndicatorLight(VehicleLight.LeftFront).On = true;
                            this.vehicle.AVehicle.IndicatorLight(VehicleLight.LeftRear).On = true;

                            this.directionSet = true;
                        }
                        else if (streetSide == EStreetSide.Right)
                        {
                            Log.Debug("Going right", this);

                            // Set indicator light
                            this.vehicle.AVehicle.IndicatorLightsMode = VehicleIndicatorLightsMode.Blinking;
                            this.vehicle.AVehicle.IndicatorLightsOn = false;

                            this.vehicle.AVehicle.IndicatorLight(VehicleLight.RightFront).On = true;
                            this.vehicle.AVehicle.IndicatorLight(VehicleLight.RightRear).On = true;

                            this.directionSet = true;
                        }

                        // If stuck, proceed
                        if (this.vehicle.IsStuck(15000))
                        {
                            Log.Debug("Process: DrivingToTheRight - Vehicle is stuck", this);
                            this.state = ETaskParkVehicleState.Wait;
                        }
                    }

                    if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCarDriveMission))
                    {
                        Log.Debug("DrivingToTheRight", this);


                        // The vehicle is at the right side of the road, now we want it to drive forwards a little so it doesn't block the road anymore
                        Vector3 closestNodePosition = Vector3.Zero;
                        float closestNodeHeading = 0f;
                        if (CVehicle.GetClosestCarNodeWithHeading(this.vehicle.Position, ref closestNodePosition, ref closestNodeHeading))
                        {
                            // Spawn an invisible vehicle in the air with the road's heading to get a point in front of our vehicle.
                            // We can't use our vehicle since the heading might be wrong.
                            CVehicle tempVehicle = new CVehicle("ADMIRAL", new Vector3(this.vehicle.Position.X, this.vehicle.Position.Y, 100), EVehicleGroup.Normal);
                            if (tempVehicle.Exists())
                            {
                                tempVehicle.FreezePosition = true;
                                tempVehicle.Visible = false;
                                tempVehicle.Heading = closestNodeHeading;

                                Vector3 position = tempVehicle.GetOffsetPosition(new Vector3(0, 8, 0));
                                tempVehicle.Delete();

                                // Make our car drive to the point
                                if (!this.IsVehicleInFront(ped))
                                {
                                    ped.Task.DriveTo(position, 7, false, true);
                                }
                                else
                                {
                                    Log.Debug("Vehicle in front, don't drive forwards", this);
                                }

                                this.state = ETaskParkVehicleState.DrivingForwards;
                            }
                        }
                    }

                    break;

                case ETaskParkVehicleState.DrivingForwards:
                    if (this.waitTimer.CanExecute(true) && !ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCarDriveMission))
                    {
                        Log.Debug("DrivingForwards", this);

                        // If vehicle should be either a little or completely on the pavement, proceed
                        if (this.parkingStyle == EVehicleParkingStyle.RightSideOfRoadLeftFrontWheelOnPavement || this.parkingStyle == EVehicleParkingStyle.RightSideOfRoadOnPavement)
                        {
                            if (this.parkingStyle == EVehicleParkingStyle.RightSideOfRoadLeftFrontWheelOnPavement && this.directionSet)
                            {
                                this.SlowDownTurning(ped);
                                this.state = ETaskParkVehicleState.Wait;
                            }
                            else if (this.parkingStyle == EVehicleParkingStyle.RightSideOfRoadOnPavement && this.directionSet)
                            {
                                this.SlowDownTurning(ped);
                                this.state = ETaskParkVehicleState.DrivingOnPavement;
                            }
                        }
                        else
                        {
                            ped.Task.CarTempAction(ECarTempActionType.SlowDownSoftly, 5000);
                            this.state = ETaskParkVehicleState.Wait;
                        }
                    }

                    break;

                case ETaskParkVehicleState.DrivingOnPavement:
                    // Better check for low speed than IsStopped so we have smoother driving
                    if (this.waitTimer.CanExecute(true) && this.vehicle.Speed < 2)
                    {
                        Log.Debug("DrivingOnPavement", this);

                        // Drive forwards using the road's heading
                        Vector3 closestNodePosition = Vector3.Zero;
                        float closestNodeHeading = 0f;
                        if (CVehicle.GetClosestCarNodeWithHeading(this.vehicle.Position, ref closestNodePosition, ref closestNodeHeading))
                        {
                            // Spawn an invisible vehicle in the air with the road's heading to get a point in front of our vehicle.
                            // We can't use our vehicle since the heading might be wrong.
                            CVehicle tempVehicle = new CVehicle("ADMIRAL", new Vector3(this.vehicle.Position.X, this.vehicle.Position.Y, 100), EVehicleGroup.Normal);
                            if (tempVehicle.Exists())
                            {
                                tempVehicle.FreezePosition = true;
                                tempVehicle.Visible = false;
                                tempVehicle.Heading = closestNodeHeading;

                                Vector3 position = tempVehicle.GetOffsetPosition(new Vector3(0, 8, 0));
                                tempVehicle.Delete();

                                // Make our car drive to the point
                                if (!this.IsVehicleInFront(ped))
                                {
                                    ped.Task.DriveTo(position, 8, false, true);
                                }
                                else
                                {
                                    Log.Debug("Vehicle in front, don't drive forwards on pavement", this);
                                }

                                this.waitTimer = new Timer(2000);
                                this.state = ETaskParkVehicleState.Wait;
                            }
                        }
                    }

                    break;

                case ETaskParkVehicleState.Wait:
                    if (this.waitTimer.CanExecute(true))
                    {
                        if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCarSetTempAction))
                        {
                            this.SlowDownTurning(ped);
                        }

                        this.HasBeenParkedSuccessfully = true;
                        this.MakeAbortable(ped);
                    }

                    break;
            }
        }

        /// <summary>
        /// Returns if the vehicle is turning left.
        /// </summary>
        /// <returns>True if left, false if not.</returns>
        private bool IsTurningLeft()
        {
            // Based on the changed heading, decide whether the vehicle is going left or right
            float diff = this.vehicle.Heading - this.startHeading;
            Log.Debug(diff.ToString(), this);

            if (diff > 2)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns if the vehicle is turning right.
        /// </summary>
        /// <returns>True if right, false if not.</returns>
        private bool IsTurningRight()
        {
            // Based on the changed heading, decide whether the vehicle is going left or right
            float diff = this.vehicle.Heading - this.startHeading;

            Log.Debug(diff.ToString(), this);

            if (diff < -2)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if there is a vehicle in front of <paramref name="ped"/>.
        /// </summary>
        /// <param name="ped">The ped.</param>
        /// <returns>True if there is a vehicle in front, false if not.</returns>
        private bool IsVehicleInFront(CPed ped)
        {
            // Check if there is a parked car infront
            bool vehicleInFront = false;
            foreach (CVehicle poolVehicle in ped.Intelligence.GetVehiclesAround(12, EVehicleSearchCriteria.All))
            {
                if (poolVehicle.Exists())
                {
                    if (poolVehicle == this.vehicle)
                    {
                        continue;
                    }

                    // Check if vehicle can be seen using 30 as fov so we only see vehicles straight infront of the ped
                    if (Common.CanPointBeSeenFromPoint(poolVehicle.Position, this.vehicle.Position, this.vehicle.Direction, 0.94f))
                    {
                        vehicleInFront = true;
                        break;
                    }
                }
            }

            return vehicleInFront;
        }

        /// <summary>
        /// Slows down the vehicle turning either left or right depending on this.left
        /// </summary>
        /// <param name="ped">
        /// The ped.
        /// </param>
        private void SlowDownTurning(CPed ped)
        {
            if (this.left)
            {
                ped.Task.CarTempAction(ECarTempActionType.SlowDownSoftlyTurnLeft, 1000);
            }
            else
            {
                ped.Task.CarTempAction(ECarTempActionType.SlowDownSoftlyTurnRight, 1000);
            }
        }
    }
}
