namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Native;

    /// <summary>
    /// Task to look at peds.
    /// </summary>
    internal class TaskLookAtPed : PedTask
    {
        /// <summary>
        /// Whether only when ped CAN be seen.
        /// </summary>
        private bool lookOnlyIfPedCanSeePed;

        /// <summary>
        /// Whether only when ped COULD be seen.
        /// </summary>
        private bool lookOnlyIfPedCouldSeePed;

        /// <summary>
        /// Whether ped should stand still.
        /// </summary>
        private bool standStill;

        /// <summary>
        /// The ped to look at.
        /// </summary>
        private CPed pedToLookAt;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskLookAtPed"/> class.
        /// </summary>
        /// <param name="ped">
        /// The ped.
        /// </param>
        /// <param name="onlyIfPedCanSeePed">
        /// Whether only when ped CAN be seen.
        /// </param>
        /// <param name="onlyIfPedCouldSeePed">
        /// Whether only when ped COULD be seen.
        /// </param>
        /// <param name="standStill">
        /// Whether ped should stand still.
        /// </param>
        /// <param name="timeOut">
        /// The time out.
        /// </param>
        public TaskLookAtPed(CPed ped, bool onlyIfPedCanSeePed, bool onlyIfPedCouldSeePed, bool standStill, int timeOut = int.MaxValue) : base(ETaskID.LookAtPed, timeOut)
        {
            this.lookOnlyIfPedCanSeePed = onlyIfPedCanSeePed;
            this.lookOnlyIfPedCouldSeePed = onlyIfPedCouldSeePed;
            this.standStill = standStill;
            this.pedToLookAt = ped;
        }

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get { return "TaskLootAtPed"; }
        }

        /// <summary>
        /// Called when the task should be aborted. Remember to call SetTaskAsDone here!
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void MakeAbortable(CPed ped)
        {
            SetTaskAsDone();
        }

        /// <summary>
        /// Processes the task logic.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void Process(CPed ped)
        {
            if (!this.pedToLookAt.Exists())
            {
                this.MakeAbortable(ped);
                return;
            }
            if (this.lookOnlyIfPedCanSeePed && !ped.Intelligence.CanSeePed(this.pedToLookAt))
            {
                this.MakeAbortable(ped);
                return;
            }
            if (this.lookOnlyIfPedCouldSeePed && !ped.Intelligence.CouldSeePed(this.pedToLookAt))
            {
                this.MakeAbortable(ped);
                return;
            }

            // If not already doing, achieve heading and look at ped
            if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexTurnToFaceEntityOrCoord) &&
                !ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleTriggerLookAt))
            {
                // Turn until we can see the player
                if (!ped.Intelligence.CanSeePed(this.pedToLookAt))
                {
                    if (this.standStill)
                    {
                        ped.Task.TurnTo(this.pedToLookAt);
                    }
                }
                else
                {
                    if (this.standStill)
                    {
                        ped.Task.StandStill(3000);
                    }

                    ped.Task.LookAtCoord(this.pedToLookAt.Position, 1000, EPedLookType.MoveHeadAsMuchAsPossible);
                }
            }
        }
    }
}
