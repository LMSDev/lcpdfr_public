namespace LCPD_First_Response.Engine.Timers
{
    /// <summary>
    /// Provides a set of methods for scheduling actions
    /// </summary>
    internal interface IScheduledAction
    {
        /// <summary>
        /// Returns if the action can be suspended now.
        /// </summary>
        /// <returns>True if action can be suspended now, otherwise false.</returns>
        bool CanBeSuspendedNow();

        /// <summary>
        /// Returns if the action can still stay suspended.
        /// </summary>
        /// <returns>True if action can stay suspended, false if not.</returns>
        bool CanStillBeSuspended();

        /// <summary>
        /// Returns the maximum amount of time the action can be suspended.
        /// </summary>
        /// <returns>Maximum amount of time the action can be suspended.</returns>
        int GetMaxAmountOfTimeTaskCanSleep();

        /// <summary>
        /// Called when the action is resumed. Inherit explicitly to prevent access.
        /// </summary>
        void Resume();

        /// <summary>
        /// Called when the action is suspended. Inherit explicitly to prevent access.
        /// </summary>
        void Suspend();
    }
}
