namespace LCPD_First_Response.Engine.Timers
{
    using System;

    /// <summary>
    /// Base class for all actions (such as Tasks or Timers) that can be scheduled.
    /// </summary>
    public abstract class ScheduledActionBase : IScheduledAction
    {
        /// <summary>
        /// The time the action has been suspended
        /// </summary>
        private DateTime suspendedTime;

          /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledActionBase"/> class.
        /// </summary>
        protected ScheduledActionBase()
        {
            this.IsSuspended = false;
            ActionScheduler.Add(this);
        }

        /// <summary>
        /// Gets a value indicating whether the action has been executed since the resumption thus can be considered for being suspended again.
        /// </summary>
        public bool HasActionBeenExecutedSinceResumption { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the action is suspended or not.
        /// </summary>
        public bool IsSuspended { get; private set; }

        /// <summary>
        /// Returns if the action can still stay suspended. Inherit explicitly to prevent access.
        /// </summary>
        /// <returns>True if task can stay suspended, false if not.</returns>
        bool IScheduledAction.CanStillBeSuspended()
        {
            IScheduledAction scheduledAction = this as IScheduledAction;
            TimeSpan ts = DateTime.Now - this.suspendedTime;

            int maxAmountOfTimeToSleep = scheduledAction.GetMaxAmountOfTimeTaskCanSleep();
            if (ActionScheduler.SchedulerStyle == ESchedulerStyle.Optimized)
            {
                maxAmountOfTimeToSleep = maxAmountOfTimeToSleep / 2;
            }

            return ts.TotalMilliseconds <= maxAmountOfTimeToSleep;
        }

        /// <summary>
        /// Called when the action is resumed. Inherit explicitly to prevent access.
        /// </summary>
        void IScheduledAction.Resume()
        {
            this.IsSuspended = false;
        }

        /// <summary>
        /// Called when the action is suspended. Inherit explicitly to prevent access.
        /// </summary>
        void IScheduledAction.Suspend()
        {
            this.IsSuspended = true;
            this.suspendedTime = DateTime.Now;
        }

        /// <summary>
        /// Returns if the action can be suspended now.
        /// </summary>
        /// <returns>True if action can be suspended now, otherwise false.</returns>
        public abstract bool CanBeSuspendedNow();

        /// <summary>
        /// Returns the maximum amount of time the action can be suspended.
        /// </summary>
        /// <returns>Maximum amount of time the action can be suspended.</returns>
        public abstract int GetMaxAmountOfTimeTaskCanSleep();

        /// <summary>
        /// Calls this to let the scheduler know, that the action has been executed and thus can be considered for being suspended again.
        /// </summary>
        protected void ActionHasBeenExecuted()
        {
            this.HasActionBeenExecutedSinceResumption = true;
        }
    }
}
