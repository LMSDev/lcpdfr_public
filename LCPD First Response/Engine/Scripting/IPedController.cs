namespace LCPD_First_Response.Engine.Scripting
{
    using LCPD_First_Response.Engine.Scripting.Entities;

    /// <summary>
    /// Defines a class that controls a ped.
    /// </summary>
    internal interface IPedController
    {
        /// <summary>
        /// Called when a ped has 'left' from the current controller, e.g. due to a more important event. Use this to clean up things.
        /// </summary>
        /// <param name="ped">
        /// The ped.
        /// </param>
        void PedHasLeft(CPed ped);
    }
}