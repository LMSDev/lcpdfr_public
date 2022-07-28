namespace LCPD_First_Response.Engine.Timers
{
    /// <summary>
    /// Task priorities. This doesn't affect the order or performance of the tasks, but is only used when clearing the tasks.
    /// </summary>
    internal enum ESystemTaskPriority
    {
        /// <summary>
        /// Priority below normal. Use for really unimportant things.
        /// </summary>
        BelowNormal,

        /// <summary>
        /// Normal prioty. Use for most things.
        /// </summary>
        Normal,

        /// <summary>
        /// Urgent priorty. Use for really important tasks.
        /// </summary>
        Urgent,
    }

    /// <summary>
    /// Base class for all system tasks.
    /// </summary>
    public abstract class SystemTask : ISystemTask
    {
        /// <summary>
        /// Gets a value indicating whether the task is active or if this task has finished and will be deleted.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Registers the task to the global task manager and starts it.
        /// </summary>
        public void Start()
        {
            TaskManager.AddTask(this);
            this.IsActive = true;
        }

        /// <summary>
        /// Called before the task is processed for the first time.
        /// </summary>
        public virtual void Initialize()
        {
        }

        /// <summary>
        /// Called when the task should be aborted. Inherit explicitly to prevent access.
        /// </summary>
        void ISystemTask.InternalAbort()
        {
            // Set task as done and call task finisher
            this.SetTaskAsDone();
            this.Abort();
        }

        /// <summary>
        /// Called when the task should be aborted.
        /// </summary>
        public abstract void Abort();

        /// <summary>
        /// Called when this task should be processed. Inherit explicitly to prevent access.
        /// </summary>
        void ISystemTask.InternalProcess()
        {
            this.Process();
        }

        /// <summary>
        /// Called when the task should be processed.
        /// </summary>
        public abstract void Process();

        /// <summary>
        /// Sets the tasks as done and will delete it.
        /// </summary>
        protected void SetTaskAsDone()
        {
            this.IsActive = false;
        }
    }
}
