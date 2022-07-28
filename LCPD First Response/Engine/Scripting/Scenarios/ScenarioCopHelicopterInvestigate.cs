namespace LCPD_First_Response.Engine.Scripting.Scenarios
{
    using GTA;

    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;

    /// <summary>
    /// Scenario for a helicopter unit that will investigate a crime scene for some time and then free all units and fly off. Can respond to any events while investigating.
    /// </summary>
    internal class ScenarioCopHelicopterInvestigate : Scenario, IPedController
    {
        /// <summary>
        /// The cops.
        /// </summary>
        private CPed[] cops;

        /// <summary>
        /// The helicopter.
        /// </summary>
        private CVehicle helicopter;

        /// <summary>
        /// The position.
        /// </summary>
        private Vector3 position;

        /// <summary>
        /// The timer.
        /// </summary>
        private NonAutomaticTimer timer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScenarioCopHelicopterInvestigate"/> class.
        /// </summary>
        /// <param name="cops">The cops.</param>
        /// <param name="helicopter">The helicopter. </param>
        /// <param name="position">The position.</param>
        public ScenarioCopHelicopterInvestigate(CPed[] cops, CVehicle helicopter, GTA.Vector3 position)
        {
            this.cops = cops;
            this.helicopter = helicopter;
            this.position = position;
            this.timer = new NonAutomaticTimer(60000);
        }

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get
            {
                return "ScenarioCopHelicopterInvestigate";
            }
        }

        /// <summary>
        /// Called when the scenario is initialized.
        /// </summary>
        public override void Initialize()
        {
            foreach (CPed cop in this.cops)
            {
                if (cop.Exists())
                {
                    // Request investigating as action
                    PedDataCop dataCop = cop.PedData as PedDataCop;
                    if (dataCop.RequestPedAction(ECopState.Investigating, this))
                    {
                        if (cop.IsDriver)
                        {
                            Route route = new Route();
                            Vector3 point = World.GetPositionAround(this.position, 80f);
                            route.AddWaypoint(new Vector3(point.X, point.Y, point.Z + 50f));
                            point = World.GetPositionAround(this.position, 80f);
                            route.AddWaypoint(new Vector3(point.X, point.Y, point.Z + 50f));
                            point = World.GetPositionAround(this.position, 80f);
                            route.AddWaypoint(new Vector3(point.X, point.Y, point.Z + 50f));
                            point = World.GetPositionAround(this.position, 80f);
                            route.AddWaypoint(new Vector3(point.X, point.Y, point.Z + 50f));
                            point = World.GetPositionAround(this.position, 80f);
                            route.AddWaypoint(new Vector3(point.X, point.Y, point.Z + 50f));

                            TaskHeliFollowRoute taskHeliFollowRoute = new TaskHeliFollowRoute(this.helicopter, route, true);
                            taskHeliFollowRoute.Speed = 10f;
                            taskHeliFollowRoute.StopAtWaypoints = true;
                            taskHeliFollowRoute.StopTime = 8000;
                            taskHeliFollowRoute.AssignTo(cop, ETaskPriority.MainTask);
                        }
                        else
                        {
                            cop.Task.Wait(int.MaxValue);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// By default frees all owned entities and deletes the base component reference
        /// </summary>
        public override void MakeAbortable()
        {
            foreach (CPed cop in this.cops)
            {
                if (cop.Exists())
                {
                    // If still assigned to this scenario, make sure ped will be freed
                    if (cop.GetPedData<PedDataCop>().IsPedStillUseable(this))
                    {
                        cop.NoLongerNeeded();
                        cop.GetPedData<PedDataCop>().ResetPedAction(this);
                        if (!cop.IsDriver)
                        {
                            cop.Task.Wait(int.MaxValue);
                        }
                        else
                        {
                            cop.Intelligence.TaskManager.ClearTasks();

                            Log.Debug("MakeAbortable: Making heli fly off", this);
                            TaskHeliFlyOff taskHeliFlyOff = new TaskHeliFlyOff();
                            taskHeliFlyOff.AssignTo(cop, ETaskPriority.MainTask);
                        }
                    }
                }
            }

            base.MakeAbortable();
        }

        /// <summary>
        /// Processes the scenario logic.
        /// </summary>
        public override void Process()
        {
            if (this.timer.CanExecute())
            {
                this.MakeAbortable();
            }
        }

        /// <summary>
        /// Called when a ped has 'left' from the current controller, e.g. due to a more important event. Use this to clean up things
        /// </summary>
        /// <param name="ped">
        /// The ped.
        /// </param>
        public void PedHasLeft(CPed ped)
        {
            if (ped.IsDriver)
            {
                if (ped.Intelligence.TaskManager.IsTaskActive(ETaskID.HeliFollowRoute))
                {
                    TaskHeliFollowRoute taskHeliFollowRoute = ped.Intelligence.TaskManager.FindTaskWithID(ETaskID.HeliFollowRoute) as TaskHeliFollowRoute;
                    taskHeliFollowRoute.MakeAbortable(ped);
                    ped.Task.ClearAll();
                }
            }
        }
    }
}