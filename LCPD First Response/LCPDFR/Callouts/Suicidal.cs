namespace LCPD_First_Response.LCPDFR.Callouts
{
    using System;

    using GTA;

    using LCPD_First_Response.Engine;

    /// <summary>
    /// The suicidal callout.
    /// </summary>
    [CalloutInfo("Suicidal", ECalloutProbability.Never)]
    internal class Suicidal : Callout
    {
        /// <summary>
        /// The blip of the position.
        /// </summary>
        private Blip blip;

        /// <summary>
        /// The spawn point.
        /// </summary>
        private SpawnPoint spawnPoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="Suicidal"/> class.
        /// </summary>
        public Suicidal()
        {
            this.CalloutMessage = CultureHelper.GetText("CALLOUT_SUICIDAL_MESSAGE");
        }

        /// <summary>
        /// The suicidal state.
        /// </summary>
        [Flags]
        internal enum ESuicidalState
        {
            /// <summary>
            /// No state.
            /// </summary>
            None = 0x0,

            /// <summary>
            /// Waiting for player.
            /// </summary>
            WaitingForPlayer = 0x1,

            /// <summary>
            /// Player is close.
            /// </summary>
            PlayerClose = 0x2,
        }
    }
}