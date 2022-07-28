namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using LCPD_First_Response.Engine.Scripting.Entities;

    /// <summary>
    /// Executes all tasks stored in a task sequence.
    /// </summary>
    internal class TaskUseSequence : PedTask
    {
        /// <summary>
        /// The current task
        /// </summary>
        private PedTask currentTask;

        /// <summary>
        /// The task sequence
        /// </summary>
        private TaskSequence taskSequence;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskUseSequence"/> class.
        /// </summary>
        /// <param name="taskSequence">
        /// The task sequence.
        /// </param>
        public TaskUseSequence(TaskSequence taskSequence) : base(ETaskID.Sequence)
        {
            this.taskSequence = taskSequence;
        }

        public override void MakeAbortable(CPed ped)
        {
            // Abort current task
            if (currentTask != null && currentTask.Active)
            {
                currentTask.MakeAbortable(ped);
            }
            SetTaskAsDone();
        }

        public override void Process(CPed ped)
        {
            // Check if currentTask is null or no longer active
            if (this.currentTask == null || !this.currentTask.Active)
            {
                // Check if there is a task left
                if (!this.taskSequence.IsTaskAvailable())
                {
                    SetTaskAsDone();
                }
                else
                {
                    this.currentTask = this.taskSequence.GetNextTask();
                    ped.Intelligence.TaskManager.Assign(this.currentTask, ETaskPriority.MainTask);
                }
            }
        }

        public override string ComponentName
        {
            get { return "TaskUseSequence"; }
        }
    }
}
