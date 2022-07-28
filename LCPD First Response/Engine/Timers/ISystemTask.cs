namespace LCPD_First_Response.Engine.Timers
{
    /// <summary>
    /// Provides a set of methods for a system task.
    /// </summary>
    internal interface ISystemTask
    {
        /// <summary>
        /// Called when this task should be aborted. Inherit explicitly to prevent access.
        /// </summary>
        void InternalAbort();

        /// <summary>
        /// Called when this task should be aborted.
        /// </summary>
        void Abort();

        /// <summary>
        /// Called when this task should be processed. Inherit explicitly to prevent access.
        /// </summary>
        void InternalProcess();

        /// <summary>
        /// Called when this task should be processed.
        /// </summary>
        void Process();
    }
}
