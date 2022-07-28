namespace LCPD_First_Response.LCPDFR.API
{    
    /// <summary>
    /// The LCPDFRBlip, which provides access to LCPDFR specific functions.
    /// </summary>
    public class LBlip
    {
        /// <summary>
        /// The internal blip.
        /// </summary>
        private GTA.Blip blip;

        /// <summary>
        /// Initializes a new instance of the <see cref="LBlip"/> class.
        /// </summary>
        /// <param name="blip">
        /// The blip.
        /// </param>
        internal LBlip(GTA.Blip blip)
        {
            this.blip = blip;
        }
    }
}
