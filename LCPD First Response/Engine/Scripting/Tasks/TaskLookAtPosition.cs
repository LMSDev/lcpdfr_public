namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Native;

    /// <summary>
    /// Task to look at a certain position.
    /// </summary>
    internal class TaskLookAtPosition : PedTask
    {
        /// <summary>
        /// The position.
        /// </summary>
        private GTA.Vector3 position;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskLookAtPosition"/> class.
        /// </summary>
        /// <param name="position">
        /// The position.
        /// </param>
        public TaskLookAtPosition(GTA.Vector3 position) : base(ETaskID.LookAtPosition)
        {
            this.position = position;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskLookAtPosition"/> class.
        /// </summary>
        /// <param name="position">
        /// The position.
        /// </param>
        /// <param name="timeOut">
        /// The time out.
        /// </param>
        public TaskLookAtPosition(GTA.Vector3 position, int timeOut) : base(ETaskID.LookAtPosition, timeOut)
        {
            this.position = position;
        }

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get
            {
                return "TaskLookAtPosition";
            }
        }

        /// <summary>
        /// Aborts the task.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void MakeAbortable(CPed ped)
        {
            this.SetTaskAsDone();
        }

        /// <summary>
        /// Processes the task.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void Process(CPed ped)
        {
            if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleStandStill))
            {
                ped.Task.StandStill(3000);
            }

            // If not already doing, achieve heading and look at ped
            if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleMoveAchieveHeading) &&
                !ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleTriggerLookAt))
            {
                // Turn until we can see the player
                if (!ped.Intelligence.CanSeePosition(this.position))
                {
                    ped.Task.AchieveHeading(ped.Heading + 5);
                }
                ped.Task.LookAtCoord(this.position, 1000, EPedLookType.MoveHeadAsMuchAsPossible);
            }
        }
    }
}
