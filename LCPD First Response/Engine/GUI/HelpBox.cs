namespace LCPD_First_Response.Engine.GUI
{
    using LCPD_First_Response.Engine.Scripting.Native;

    /// <summary>
    /// Helpbox class.
    /// </summary>
    internal static class HelpBox
    {
        /// <summary>
        /// Clears the help box.
        /// </summary>
        public static void Clear()
        {
            Natives.ClearHelp();
        }

        /// <summary>
        /// Returns whether a helpbox is displayed.
        /// </summary>
        /// <returns>True if yes, false if not.</returns>
        public static bool IsBeingDisplayed()
        {
            return Natives.IsHelpMessageBeingDisplayed();
        }

        /// <summary>
        /// Prints <paramref name="text"/> in a help box.
        /// </summary>
        /// <param name="text">The text.</param>
        public static void Print(string text)
        {
            AdvancedHookManaged.AGame.PrintText(text);
        }
    }
}
