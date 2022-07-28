namespace LCPD_First_Response.LCPDFR.Scripts.Scenarios
{
    using LCPD_First_Response.Engine.Scripting.Scenarios;

    /// <summary>
    /// Contains set of functions for an ambient scenario.
    /// </summary>
    internal interface IAmbientScenario
    {
        /// <summary>
        /// Checks whether the scenario can start at the position depending on available peds.
        /// </summary>
        /// <param name="position">
        /// The position.
        /// </param>
        /// <returns>
        /// True if yes, otherwise false.
        /// </returns>
        bool CanScenarioStart(GTA.Vector3 position);

        /// <summary>
        /// Checks whether the scenario can be disposed now, most likely because player got too far away.
        /// </summary>
        /// <returns>True if yes, false otherwise.</returns>
        bool CanBeDisposedNow();
    }
}