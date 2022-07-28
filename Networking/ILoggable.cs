namespace LCPDFR.Networking
{
    /// <summary>
    /// To ease logging, implement this to have your class automatically return its name.
    /// </summary>
    internal interface ILoggable
    {
        /// <summary>
        /// Gets the component name.
        /// </summary>
        string ComponentName { get; }
    }
}