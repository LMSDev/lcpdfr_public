namespace LCPD_First_Response.Engine.GUI
{
    /// <summary>
    /// Represents a forms class that can have controls.
    /// </summary>
    internal interface ICanHaveControls
    {
        /// <summary>
        /// Adds <paramref name="control"/>.
        /// </summary>
        /// <param name="control">The control.</param>
        void AddControl(Control control);

        /// <summary>
        /// Gets all controls.
        /// </summary>
        /// <returns>All controls.</returns>
        Control[] GetControls();
    }
}
