namespace LCPD_First_Response.Engine.Timers
{
    /// <summary>
    /// Scheduled action class that can be used to make your code schedulable via an easy to use object rather than inheriting.
    /// </summary>
    internal class ScheduledAction : ScheduledActionBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledAction"/> class.
        /// </summary>
        /// <param name="maxAmountOfTimeToSleep">
        /// The max amount of time to sleep.
        /// </param>
        public ScheduledAction(int maxAmountOfTimeToSleep)
        {
            this.MaxAmountOfTimeToSleep = maxAmountOfTimeToSleep;
        }

        /// <summary>
        /// Gets or sets the maximum amount of time this timer can be suspended.
        /// </summary>
        public int MaxAmountOfTimeToSleep { get; set; }

        /// <summary>
        /// Checks if the action can be executed.
        /// </summary>
        /// <returns>True if the action can be executed, false if not.</returns>
        public bool CanExecute()
        {
            // Ensure this action is not suspended
            if (this.IsSuspended)
            {
                return false;
            }

            // We are about to allow the action to be executed, so we let our base know
            this.ActionHasBeenExecuted();

            return true;
        }

        /// <summary>
        /// Returns if the action can be suspended now.
        /// </summary>
        /// <returns>True if action can be suspended now, otherwise false.</returns>
        public override bool CanBeSuspendedNow()
        {
            return true;
        }

        /// <summary>
        /// Returns the maximum amount of time the action can be suspended.
        /// </summary>
        /// <returns>Maximum amount of time the action can be suspended.</returns>
        public override int GetMaxAmountOfTimeTaskCanSleep()
        {
            return this.MaxAmountOfTimeToSleep;
        }
    }
}
