namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using GTA;

    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Timers;

    /// <summary>
    /// Makes a ped drive as if it was drunk.
    /// </summary>
    internal class TaskDriveDrunk : PedTask
    {
        /// <summary>
        /// Whether the ped has been sober long enough.
        /// </summary>
        private bool canBeDrunkenAgain;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskDriveDrunk"/> class.
        /// </summary>
        public TaskDriveDrunk() : base(ETaskID.DriveDrunk)
        {
            this.canBeDrunkenAgain = true;
        }

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get
            {
                return "TaskDriveDrunk";
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
            if (!ped.IsInVehicle)
            {
                this.MakeAbortable(ped);
                return;
            }

            if (this.canBeDrunkenAgain)
            {
                if (Common.GetRandomBool(0, 100, 1))
                {
                    // Get position to the left or right of the ped
                    if (Common.GetRandomBool(0, 2, 1))
                    {
                        ped.Task.DriveTo(ped.CurrentVehicle.GetOffsetPosition(new Vector3(1.5f, 15f, 0)), 25f, false, true);
                    }
                    else
                    {
                        ped.Task.DriveTo(ped.CurrentVehicle.GetOffsetPosition(new Vector3(-1.5f, 15f, 0)), 25f, false, true);
                    }

                    this.canBeDrunkenAgain = false;

                    DelayedCaller.Call(delegate { this.canBeDrunkenAgain = true; }, this, Common.GetRandomValue(8000, 12000));
                }
            }
            else
            {
                if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCarDriveMission))
                {
                    ped.Task.CruiseWithVehicle(ped.CurrentVehicle, 25f, false);
                }
            }
        }
    }
}