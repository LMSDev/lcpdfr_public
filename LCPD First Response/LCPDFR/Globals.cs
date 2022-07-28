namespace LCPD_First_Response.LCPDFR
{
    using GTA;

    /// <summary>
    /// Global variables for LCPDFR.
    /// </summary>
    internal static class Globals
    {
        /// <summary>
        /// Whether this is the first start of LCPDFR.
        /// </summary>
        private static bool isFirstStart;

        /// <summary>
        /// Initializes static members of the <see cref="Globals"/> class.
        /// </summary>
        static Globals()
        {
            isFirstStart = Settings.GetValue("Main", "FirstStart", true);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this is the first start of LCPDFR.
        /// </summary>
        public static bool IsFirstStart
        {
            get
            {
                return isFirstStart;
            }

            set
            {
                isFirstStart = value;
                Settings.WriteValue("Main", "FirstStart", isFirstStart.ToString());
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the help box for the quick action menu has been displayed.
        /// </summary>
        public static bool HasHelpboxDisplayedQuickActionMenu { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the help box for the quick action menu's partner tab has been displayed.
        /// </summary>
        public static bool HasHelpboxDisplayedQuickActionMenuPartnerTab { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the help box for player updating its state after callout has been displayed.
        /// </summary>
        public static bool HasHelpboxDisplayedPlayerUpdateState { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the help box for random scenarios has been displayed.
        /// </summary>
        public static bool HasHelpboxDisplayedWorldEvents { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the player is on duty.
        /// </summary>
        public static bool IsOnDuty { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether external text input is active
        /// </summary>
        public static bool IsExternalTextInput { get; set; }

        /// <summary>
        /// Gets a value indicating whether it's night (between 19:00 - 06:00).
        /// </summary>
        public static bool IsNightTime
        {
            get
            {
                return (World.CurrentDayTime.Hours > 19 || World.CurrentDayTime.Hours < 6);
            }
        }
    }
}