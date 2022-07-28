namespace LCPD_First_Response.Engine.Timers
{
    /// <summary>
    /// Should be used for core classes such as ped intelligence. Shouldn't be used for core classes such as CopManager or Chase.
    /// </summary>
    internal interface ICoreTickable
    {
        /// <summary>
        /// Called every tick.
        /// </summary>
        void Process();
    }
}
