namespace LCPD_First_Response.Engine.Timers
{
    /// <summary>
    /// A normal timer, that will call the callback after the time has elapsed.
    /// </summary>
    public class Timer : SystemTaskTimed
    {
        /// <summary>
        /// The callback.
        /// </summary>
        private TimerCallbackEventHandler callback;

        /// <summary>
        /// The parameter.
        /// </summary>
        private object[] parameter;

        /// <summary>
        /// Initializes a new instance of the <see cref="Timer"/> class.
        /// </summary>
        /// <param name="interval">
        /// The interval.
        /// </param>
        /// <param name="callback">
        /// The callback.
        /// </param>
        public Timer(int interval, TimerCallbackEventHandler callback) : base(interval)
        {
            this.callback = callback;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Timer"/> class.
        /// </summary>
        /// <param name="interval">
        /// The interval.
        /// </param>
        /// <param name="callback">
        /// The callback.
        /// </param>
        /// <param name="parameter">
        /// The parameter.
        /// </param>
        public Timer(int interval, TimerCallbackEventHandler callback, params object[] parameter) : base(interval)
        {
            this.callback = callback;
            this.parameter = parameter;
        }

        /// <summary>
        /// Function that is called by the timer. Function can take any amount of arguments.
        /// </summary>
        /// <param name="parameter">Parameter, if any.</param>
        public delegate void TimerCallbackEventHandler(params object[] parameter);

        /// <summary>
        /// Called when the task should be aborted.
        /// </summary>
        public override void Abort()
        {
            this.SetTaskAsDone();
        }

        /// <summary>
        /// Called when the task should be processed.
        /// </summary>
        public override void Process()
        {
            this.callback.Invoke(this.parameter);
        }

        /// <summary>
        /// Stops the timer.
        /// </summary>
        public void Stop()
        {
            this.Abort();
        }
    }

    /// <summary>
    /// A normal timer, that will call the callback after the time has elapsed and can be used as a simple non-blocking loop.
    /// </summary>
    public class TimerLoop : SystemTaskTimed
    {
        /// <summary>
        /// The callback.
        /// </summary>
        private TimerCallbackNewEventHandler callback;

        /// <summary>
        /// The parameter.
        /// </summary>
        private object[] parameter;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimerLoop"/> class.
        /// </summary>
        /// <param name="interval">
        /// The interval.
        /// </param>
        /// <param name="callback">
        /// The callback.
        /// </param>
        public TimerLoop(int interval, TimerCallbackNewEventHandler callback) : base(interval)
        {
            this.callback = callback;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimerLoop"/> class.
        /// </summary>
        /// <param name="interval">
        /// The interval.
        /// </param>
        /// <param name="callback">
        /// The callback.
        /// </param>
        /// <param name="parameter">
        /// The parameter.
        /// </param>
        public TimerLoop(int interval, TimerCallbackNewEventHandler callback, params object[] parameter) : base(interval)
        {
            this.callback = callback;
            this.parameter = parameter;
        }

        /// <summary>
        /// Function that is called by the timer. Function can take any amount of arguments.
        /// </summary>
        /// <param name="timer">The calling timer.</param>
        /// <param name="parameter">Parameter, if any.</param>
        public delegate void TimerCallbackNewEventHandler(TimerLoop timer, params object[] parameter);

        /// <summary>
        /// Called when the task should be aborted.
        /// </summary>
        public override void Abort()
        {
            this.SetTaskAsDone();
        }

        /// <summary>
        /// Called when the task should be processed.
        /// </summary>
        public override void Process()
        {
            this.callback.Invoke(this, this.parameter);
        }

        /// <summary>
        /// Stops the timer.
        /// </summary>
        public void Stop()
        {
            this.Abort();
        }
    }
}