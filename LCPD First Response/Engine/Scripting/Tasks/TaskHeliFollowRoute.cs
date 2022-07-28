namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Timers;

    /// <summary>
    /// Makes a helicopter follow a route.
    /// </summary>
    internal class TaskHeliFollowRoute : PedTask
    {
        /// <summary>
        /// The current waypoint.
        /// </summary>
        private GTA.Vector3 currentWaypoint;

        /// <summary>
        /// The helicopter.
        /// </summary>
        private CVehicle helicopter;

        /// <summary>
        /// Indicates whether or not the route should be repeated.
        /// </summary>
        private bool patrol;

        /// <summary>
        /// The route.
        /// </summary>
        private Route route;

        /// <summary>
        /// The timer used when stopping at a waypoint.
        /// </summary>
        private Timers.NonAutomaticTimer stopTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskHeliFollowRoute"/> class.
        /// </summary>
        /// <param name="helicopter">
        /// The helicopter.
        /// </param>
        /// <param name="route">
        /// The route.
        /// </param>
        /// <param name="patrol">
        /// If true, route will be reset when finished, so the heli will follow it again and again.
        /// </param>
        public TaskHeliFollowRoute(CVehicle helicopter, Route route, bool patrol) : base(ETaskID.HeliFollowRoute)
        {
            this.helicopter = helicopter;
            this.currentWaypoint = GTA.Vector3.Zero;
            this.patrol = patrol;
            this.route = route;
            this.Speed = 20f;
            this.stopTimer = new NonAutomaticTimer(5000);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskHeliFollowRoute"/> class.
        /// </summary>
        /// <param name="helicopter">
        /// The helicopter.
        /// </param>
        /// <param name="route">
        /// The route.
        /// </param>
        /// <param name="timeOut">
        /// The time out.
        /// </param>
        public TaskHeliFollowRoute(CVehicle helicopter, Route route, int timeOut) : base(ETaskID.HeliFollowRoute, timeOut)
        {
            this.helicopter = helicopter;
            this.currentWaypoint = GTA.Vector3.Zero;
            this.route = route;
            this.Speed = 20f;
            this.stopTimer = new NonAutomaticTimer(5000);
        }

        /// <summary>
        /// Gets or sets the maximum speed of the helicopter.
        /// </summary>
        public float Speed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the helicopter will make a short stop when reaching a waypoint.
        /// </summary>
        public bool StopAtWaypoints { get; set; }

        /// <summary>
        /// Sets the time to stop between waypoints.
        /// </summary>
        public int StopTime
        {
            set
            {
                this.stopTimer = new NonAutomaticTimer(value);
            }
        }

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get
            {
                return "TaskHeliFollowRoute";
            }
        }

        /// <summary>
        /// Called when the task should be aborted. Remember to call SetTaskAsDone here!
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void MakeAbortable(CPed ped)
        {
            this.SetTaskAsDone();
        }

        /// <summary>
        /// Processes the task logic.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void Process(CPed ped)
        {
            // Check if waypoint has been reached, if so, set to zero.
            if (ped.Position.DistanceTo2D(this.currentWaypoint) < 15)
            {
                this.currentWaypoint = GTA.Vector3.Zero;
                Log.Debug("Process: Waypoint reached", this);
            }

            if (!ped.IsSittingInVehicle(this.helicopter))
            {
                Log.Debug("Process: No longer in heli, aborting", this);
                this.MakeAbortable(ped);
                return;
            }

            if (this.currentWaypoint == GTA.Vector3.Zero)
            {
                if (this.StopAtWaypoints)
                {
                    // Wait
                    if (!this.stopTimer.CanExecute(true))
                    {
                        this.helicopter.Stabilize();
                        return;
                    }
                }

                // Check if there is a waypoint left.
                if (this.route.IsWaypointAvailable())
                {
                    this.currentWaypoint = this.route.GetNextWaypoint();
                    ped.Task.HeliMission(ped.CurrentVehicle, 0, 0, this.currentWaypoint, 4, this.Speed, 5, -1.0f, (int)this.currentWaypoint.Z + 2, (int)this.currentWaypoint.Z + 10);
                }
                else
                {
                    if (this.patrol)
                    {
                        this.route.Reset();
                    }
                    else
                    {
                        this.MakeAbortable(ped);
                    }
                }
            }
        }
    }
}
