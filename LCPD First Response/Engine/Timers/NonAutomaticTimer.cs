namespace LCPD_First_Response.Engine.Timers
{
    using System;

    /// <summary>
    /// Provides options for the <see cref="NonAutomaticTimer"/> class.
    /// </summary>
    public enum ETimerOptions
    {
        /// <summary>
        /// Timer will reset after it elapsed
        /// </summary>
        Default,

        /// <summary>
        /// After the timer has elapsed once, check for execution allowance will always return true
        /// </summary>
        OneTimeReturnTrue,

        /// <summary>
        /// After timer has elapsed once, check for execution allowance will always return false (therefore the timer code will be disabled)
        /// </summary>
        OneTimeReturnFalse,
    }

    /// <summary>
    /// Special timer class, which doesn't call a callback when an interval has elasped, but checks if the timer has elasped when calling Execute(). 
    /// This way, you have more control, when the timer is allowed to fire. Supports scheduling.
    /// For functions you only want to execute once after a certain amount of time, use <see cref="DelayedCaller"/>.
    /// </summary>
    public class NonAutomaticTimer : ScheduledActionBase
    {
        /// <summary>
        /// If CanExecute has been already called or not.
        /// </summary>
        private bool alreadyCalled;

        /// <summary>
        /// The actual timer interval
        /// </summary>
        private double interval;

        /// <summary>
        /// Indicating how the timer should behave
        /// </summary>
        private ETimerOptions timerOptions;

        /// <summary>
        /// DateTime object that represents the time to wait for
        /// </summary>
        private DateTime timerDate;

        /// <summary>
        /// Initializes a new instance of the <see cref="NonAutomaticTimer"/> class. 
        /// </summary>
        /// <param name="interval">
        /// Time in milliseconds.
        /// </param>
        public NonAutomaticTimer(int interval)
        {
            this.interval = interval;
            this.timerDate = DateTime.Now.AddMilliseconds(this.interval);
            this.timerOptions = ETimerOptions.Default;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NonAutomaticTimer"/> class. 
        /// </summary>
        /// <param name="interval">
        /// Time in milliseconds.
        /// </param>
        /// <param name="timerOptions">
        /// Timer options, specifying the timer behavior.
        /// </param>
        public NonAutomaticTimer(int interval, ETimerOptions timerOptions)
        {
            this.interval = interval;
            this.timerDate = DateTime.Now.AddMilliseconds(this.interval);
            this.timerOptions = timerOptions;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this timer can be considered for scheduling.
        /// </summary>
        public bool AllowScheduling { get; set; }

        /// <summary>
        /// Gets or sets the maximum amount of time this timer can be suspended.
        /// </summary>
        public int MaxAmountOfTimeToSleep { get; set; }

        /// <summary>
        /// Checks if the internal timer is high enough for the action to be executed. Resets the internal timer if action can be executed.
        /// </summary>
        /// <param name="resetIfFirstCall">
        /// If true and the call to CanExecute is the first made after the timer has been reset, the internal timer will be reset. This can be used to ensure a timer is only started
        /// when the code first reached the CanExecute call.
        /// </param>
        /// <returns>
        /// True if the action can be executed, false if not.
        /// </returns>
        public bool CanExecute(bool resetIfFirstCall = false)
        {
            if (resetIfFirstCall)
            {
                if (!this.alreadyCalled)
                {
                    this.Reset();
                    this.alreadyCalled = true;
                }
            }

            // Check if internal timer is higher than interval
            // HACK: *25 so 1000ms are equal to 1second. Very dirty, but needed for now. Switch to DateTime approach later?
            //if (this.timer * 25 > this.interval)
            if (DateTime.Now.CompareTo(this.timerDate) > 0)
            {
                // Ensure this timer is not suspended
                if (this.IsSuspended)
                {
                    return false;
                }

                // We are about to allow the action to be executed, so we let our base know
                this.ActionHasBeenExecuted();

                // The timer should be stopped after it has elapsed, so we return false
                if (this.timerOptions == ETimerOptions.OneTimeReturnFalse)
                {
                    return false;
                }

                // Reset timer and return true
                if (this.timerOptions == ETimerOptions.Default)
                {
                    this.Reset();
                }

                // The timer shouldn't reset after it has elapsed, so we return true (no need to check for the option since we cover the other two options above)
                return true;
            }

            return false;
        }

        /// <summary>
        /// Increments the internal counter and (if high enough) executes the given action.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        public void Execute(Action action)
        {
            if (CanExecute())
            {
                action.Invoke();
            }
        }

        /// <summary>
        /// Resets the waiting time to CurrentTime + Interval
        /// </summary>
        public void Reset()
        {
            this.alreadyCalled = false;
            this.timerDate = DateTime.Now.AddMilliseconds(this.interval);
        }

        /// <summary>
        /// Sets the timer to elapsed so the next call to CanExecute will pass. This action can't be undone.
        /// </summary>
        public void Trigger()
        {
            this.timerDate = DateTime.MinValue;
        }

        /// <summary>
        /// Returns if the timer can be suspended now.
        /// </summary>
        /// <returns>True if timer can be suspended now, otherwise false.</returns>
        public override bool CanBeSuspendedNow()
        {
            return this.AllowScheduling;
        }

        /// <summary>
        /// Returns the maximum amount of time the timer can be suspended.
        /// </summary>
        /// <returns>Maximum amount of time the timer can be suspended.</returns>
        public override int GetMaxAmountOfTimeTaskCanSleep()
        {
            return this.MaxAmountOfTimeToSleep;
        }
    }
}
