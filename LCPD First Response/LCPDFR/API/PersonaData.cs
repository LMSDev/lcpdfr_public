namespace LCPD_First_Response.LCPDFR.API
{
    using System;

    /// <summary>
    /// Contains persona data about a ped, such as date of birth.
    /// </summary>
    public class PersonaData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PersonaData"/> class.
        /// </summary>
        /// <param name="birthDay">
        /// The birth day.
        /// </param>
        /// <param name="citations">
        /// The citations.
        /// </param>
        /// <param name="forename">
        /// The forename.
        /// </param>
        /// <param name="lastname">
        /// The lastname.
        /// </param>
        /// <param name="validLicense">
        /// The valid license.
        /// </param>
        /// <param name="timesStopped">
        /// The times stopped.
        /// </param>
        /// <param name="wanted">
        /// The wanted.
        /// </param>
        public PersonaData(DateTime birthDay, int citations, string forename, string lastname, bool validLicense, int timesStopped, bool wanted)
        {
            this.BirthDay = birthDay;
            this.Citations = citations;
            this.Forename = forename;
            this.Surname = lastname;
            this.HasValidLicense = validLicense;
            this.TimesStopped = timesStopped;
            this.Wanted = wanted;
        }

        /// <summary>
        /// Gets the birth day of the ped.
        /// </summary>
        public DateTime BirthDay { get; private set; }

        /// <summary>
        /// Gets or sets the number of citations.
        /// </summary>
        public int Citations { get; set; }

        /// <summary>
        /// Gets the forename.
        /// </summary>
        public string Forename { get; private set; }

        /// <summary>
        /// Gets the full name (forename and surname).
        /// </summary>
        public string FullName
        {
            get
            {
                return this.Forename + " " + this.Surname;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the ped has valid license.
        /// </summary>
        public bool HasValidLicense { get; private set; }

        /// <summary>
        /// Gets the surname.
        /// </summary>
        public string Surname { get; private set; }

        /// <summary>
        /// Gets or sets the times a ped was stopped.
        /// </summary>
        public int TimesStopped { get; set; }

        /// <summary>
        /// Gets a value indicating whether or not the ped is wanted.
        /// </summary>
        public bool Wanted { get; private set; }

        /// <summary>
        /// Creates a new instance of <see cref="PersonaData"/>.
        /// </summary>
        /// <param name="persona">The persona.</param>
        /// <returns>The new persona data object.</returns>
        internal static PersonaData FromPersona(Engine.Scripting.Entities.Persona persona)
        {
            return new PersonaData(persona.BirthDay, persona.Citations, persona.Forename, persona.Surname, persona.LicenseState == Engine.Scripting.Entities.ELicenseState.Valid, persona.TimesStopped, persona.Wanted);
        }
    }
}