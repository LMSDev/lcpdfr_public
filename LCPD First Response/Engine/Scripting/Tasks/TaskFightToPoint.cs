namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Timers;

    /// <summary>
    /// Follows a route to a point and looks out for enemies. The further the distance, the more waypoints are required!
    /// </summary>
    internal class TaskFightToPoint : PedTask
    {
        /// <summary>
        /// The current waypoint.
        /// </summary>
        private GTA.Vector3 currentWaypoint;

        /// <summary>
        /// The route.
        /// </summary>
        private Route route;

        /// <summary>
        /// Timer to check for enemies.
        /// </summary>
        private Timers.NonAutomaticTimer timerCheckForEnemies;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskFightToPoint"/> class.
        /// </summary>
        /// <param name="route">
        /// The route.
        /// </param>
        public TaskFightToPoint(Route route) : base(ETaskID.FightToPoint)
        {
            this.currentWaypoint = GTA.Vector3.Zero;
            this.route = route;
            this.timerCheckForEnemies = new NonAutomaticTimer(5000);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskFightToPoint"/> class.
        /// </summary>
        /// <param name="route">
        /// The route.
        /// </param>
        /// <param name="timeOut">
        /// The time out.
        /// </param>
        public TaskFightToPoint(Route route, int timeOut) : base(ETaskID.FightToPoint, timeOut)
        {
            this.currentWaypoint = GTA.Vector3.Zero;
            this.route = route;
            this.timerCheckForEnemies = new NonAutomaticTimer(5000);
        }

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get
            {
                return "TaskFightToPoint";
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
            if (ped.Position.DistanceTo(this.currentWaypoint) < 5)
            {
                this.currentWaypoint = GTA.Vector3.Zero;
                Log.Debug("Process: Waypoint reached", this);
            }

            if (this.currentWaypoint == GTA.Vector3.Zero)
            {
                // Check if there is a waypoint left.
                if (this.route.IsWaypointAvailable())
                {
                    this.currentWaypoint = this.route.GetNextWaypoint();
                    ped.Task.RunTo(this.currentWaypoint);
                }
                else
                {
                    this.MakeAbortable(ped);
                }
            }

            if (this.timerCheckForEnemies.CanExecute())
            {
                if (!ped.IsInCombat)
                {
                    ped.Task.FightAgainstHatedTargets(25f);
                    DelayedCaller.Call(this.IsPedInCombatCheck, 250, ped);
                    Log.Debug("Process: Checking for enemies", this);
                }
            }
        }

        /// <summary>
        /// Checks if the ped is in combat, if not, continue running.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        private void IsPedInCombatCheck(params object[] parameter)
        {
            CPed ped = parameter[0] as CPed;

            if (ped != null && ped.Exists())
            {
                if (!ped.IsInCombat)
                {
                    ped.Task.RunTo(this.currentWaypoint);
                }
            }
        }
    }
}
