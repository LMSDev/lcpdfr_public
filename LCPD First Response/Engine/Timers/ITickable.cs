namespace LCPD_First_Response.Engine.Timers
{
    /// <summary>
    /// Interface that forces the class to implement Process which will be called from the main loop.
    /// </summary>
    internal interface ITickable
    {
        /// <summary>
        /// Called every tick.
        /// </summary>
        void Process();
    }
}
