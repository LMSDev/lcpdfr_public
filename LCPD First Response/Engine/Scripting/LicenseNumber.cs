namespace LCPD_First_Response.Engine.Scripting
{
    /// <summary>
    /// The state of license number.
    /// </summary>
    internal enum ELicenseNumberState
    {
        /// <summary>
        /// Nothing wrong.
        /// </summary>
        None,

        /// <summary>
        /// The vehicle has been reported as stolen.
        /// </summary>
        Stolen,

        /// <summary>
        /// The owner of the vehicle is wanted on warrant.
        /// </summary>
        OwnerWarrant,

        /// <summary>
        /// The vehicle was involved in a hit and run.
        /// </summary>
        HitAndRunInvolvement,

        /// <summary>
        /// The vehicle was used to flee a crime scene.
        /// </summary>
        FledCrimeScene,

        /// <summary>
        /// The vehicle was involved in a pedestrian struck on.
        /// </summary>
        StruckPedestrian,

        /// <summary>
        /// A ticket for the vehicle has not been paid.
        /// </summary>
        UnpaidTicket,

        /// <summary>
        /// The plate is expired.
        /// </summary>
        PlateExpired,
    }

    /// <summary>
    /// The license number of a vehicle.
    /// </summary>
    internal class LicenseNumber
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LicenseNumber"/> class.
        /// </summary>
        public LicenseNumber()
        {
            this.GenerateLicenseNumber();

            // Randomize state
            int randomValue = Common.GetRandomValue(0, 100);
            if (randomValue < 95)
            {
                this.State = ELicenseNumberState.None;
            }
            else
            {
                this.State = (ELicenseNumberState)Common.GetRandomEnumValue(typeof(ELicenseNumberState));
            }
        }

        /// <summary>
        /// Gets the number.
        /// </summary>
        public string Number { get; private set; }

        /// <summary>
        /// Gets the state.
        /// </summary>
        public ELicenseNumberState State { get; private set; }

        /// <summary>
        /// Generates a new random license number.
        /// </summary>
        private void GenerateLicenseNumber()
        {
            // Generate 3 random letters
            for (int i = 0; i < 3; i++)
            {
                this.Number += Common.GetRandomLetter(true);
            }

            // Generate a 4-digit number
            int number = Common.GetRandomValue(1000, 10000);
            this.Number += number.ToString();
        }
    }
}