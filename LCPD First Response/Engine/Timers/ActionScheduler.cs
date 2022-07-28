namespace LCPD_First_Response.Engine.Timers
{
    using System.Collections.Generic;

    /// <summary>
    /// Defines how the task manager should manage scheduling.
    /// </summary>
    internal enum ESchedulerStyle
    {
        /// <summary>
        /// Tasks can't be suspended and are always executed.
        /// </summary>
        Never,

        /// <summary>
        /// Mix between Never and Performance. Tasks will be suspended, half the time compared to Performance mode though.
        /// </summary>
        Optimized,

        /*
        /// <summary>
        /// Tasks that can be suspended for a long time will be suspended.
        /// </summary>
        Partly,*/

        /// <summary>
        /// All tasks that can be suspended, are suspended.
        /// </summary>
        Performance,
    }

    /// <summary>
    /// Schedules all actions that can be scheduled.
    /// </summary>
    internal static class ActionScheduler
    {
        /// <summary>
        /// The minimum amount of time before an action is considered to be suspended when style is set to ETaskSchedulerStyle.Partly
        /// </summary>
        private const int MinAmountOfTimeToPause = 2000;

        /// <summary>
        /// List of scheduled actions
        /// </summary>
        private static List<ScheduledActionBase> scheduledActions;

        /// <summary>
        /// Initializes static members of the <see cref="ActionScheduler"/> class.
        /// </summary>
        static ActionScheduler()
        {
            scheduledActions = new List<ScheduledActionBase>();
            SchedulerStyle = ESchedulerStyle.Never;
        }

        /// <summary>
        /// Gets the scheduler style
        /// </summary>
        public static ESchedulerStyle SchedulerStyle { get; private set; }

        /// <summary>
        /// Adds <paramref name="scheduledAction"/> to the action list.
        /// </summary>
        /// <param name="scheduledAction">The scheduled action.</param>
        public static void Add(ScheduledActionBase scheduledAction)
        {
            scheduledActions.Add(scheduledAction);
        }

        /// <summary>
        /// Schedules all actions.
        /// </summary>
        public static void Process()
        {
            switch (SchedulerStyle)
            {
                    // If scheduling is partly allowed, suspend actions that can wait for a long time (thus are not so important but still run every tick)
                /*case ESchedulerStyle.Partly:
                    foreach (ScheduledActionBase scheduledAction in scheduledActions)
                    {
                        // Access interface because functions are inherited explicitly to prevent usage from other classes
                        IScheduledAction scheduledActionInterface = scheduledAction;
                        int maxAmountOfTimeToSleep = scheduledAction.GetMaxAmountOfTimeTaskCanSleep();

                        // If action can be suspended at its current stage and the maximum amount of time is greater than the minium time needed, suspend
                        if (scheduledActionInterface.CanBeSuspendedNow() && !scheduledAction.IsSuspended
                            && maxAmountOfTimeToSleep > MinAmountOfTimeToPause)
                        {
                            scheduledActionInterface.Suspend();
                            continue;
                        }

                        // If suspended, check if still allowed to be suspended
                        if (scheduledAction.IsSuspended)
                        {
                            if (scheduledActionInterface.CanStillBeSuspended())
                            {
                                continue;
                            }

                            // Action can't stay suspended any longer, so we resume it
                            scheduledActionInterface.Resume();
                        }
                    }

                    break;*/

                    // If scheduling is set to performance, all actions that can be suspended are suspended
                case ESchedulerStyle.Optimized:
                case ESchedulerStyle.Performance:
                    foreach (ScheduledActionBase scheduledAction in scheduledActions)
                    {
                        // Access interface because functions are inherited explicitly to prevent usage from other classes
                        IScheduledAction scheduledActionInterface = scheduledAction;

                        // If action can be suspended at its current stage and the maximum amount of time is greater than the minium time needed, suspend
                        if (scheduledActionInterface.CanBeSuspendedNow() && !scheduledAction.IsSuspended)
                        {
                            scheduledActionInterface.Suspend();
                            continue;
                        }

                        // If suspended, check if still allowed to be suspended
                        if (scheduledAction.IsSuspended)
                        {
                            if (scheduledActionInterface.CanStillBeSuspended())
                            {
                                continue;
                            }

                            // Action can't stay suspended any longer, so we resume it
                            scheduledActionInterface.Resume();
                        }
                    }

                    break;
            }
        }

        /// <summary>
        /// Sets the scheduler style to <paramref name="style"/>.
        /// </summary>
        /// <param name="style">The style.</param>
        public static void SetSchedulerStyle(ESchedulerStyle style)
        {
            SchedulerStyle = style;

            // If scheduling is not allowed, simply resume all actions
            if (SchedulerStyle == ESchedulerStyle.Never)
            {
                foreach (ScheduledActionBase scheduledAction in scheduledActions)
                {
                    if (scheduledAction.IsSuspended)
                    {
                        // Access interface because functions are inherited explicitly to prevent usage from other classes
                        IScheduledAction scheduledActionInterface = scheduledAction;
                        scheduledActionInterface.Resume();
                    }
                }
            }
        }
    }
}
