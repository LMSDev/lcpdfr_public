namespace LCPD_First_Response.LCPDFR.API
{
    /// <summary>
    /// Handle to a LCPDFR object.
    /// </summary>
    public class LHandle
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LHandle"/> class.
        /// </summary>
        /// <param name="obj">
        /// The object.
        /// </param>
        internal LHandle(object obj)
        {
            this.Object = obj;
        }

        /// <summary>
        /// Gets the object.
        /// </summary>
        internal object Object { get; private set; }
    }
}
