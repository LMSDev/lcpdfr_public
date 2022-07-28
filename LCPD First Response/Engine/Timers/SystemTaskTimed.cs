namespace LCPD_First_Response.Engine.Timers
{
    /// <summary>
    /// Base class for all system tasks that are required to run at a certain interval.
    /// </summary>
    public abstract class SystemTaskTimed : SystemTask, ISystemTask
    {
        /// <summary>
        /// Timer that is used.
        /// </summary>
        private NonAutomaticTimer timer;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemTaskTimed"/> class.
        /// </summary>
        /// <param name="interval">
        /// The interval.
        /// </param>
        protected SystemTaskTimed(int interval)
        {
            this.timer = new NonAutomaticTimer(interval);
        }

        /// <summary>
        /// Called when this task should be processed. Inherit explicitly to prevent access.
        /// </summary>
        void ISystemTask.InternalProcess()
        {
            if (this.timer.CanExecute())
            {
                this.Process();
            }
        }
    }
}
