namespace LCPD_First_Response.Engine.Scripting.Entities
{
    using System;

    using LCPD_First_Response.Engine.IO;

    /// <summary>
    /// The license state.
    /// </summary>
    internal enum ELicenseState
    {
        /// <summary>
        ///  No state.
        /// </summary>
        None,

        /// <summary>
        /// License expired.
        /// </summary>
        Expired,

        /// <summary>
        /// License valid.
        /// </summary>
        Valid,

        /// <summary>
        /// License revoked.
        /// </summary>
        Revoked,
    }

    /// <summary>
    /// Contains data associated to a ped.
    /// </summary>
    internal class Persona
    {
        /// <summary>
        /// Weak reference to the name data.
        /// </summary>
        private static string[][] names;

        /// <summary>
        /// The ped.
        /// </summary>
        private CPed ped;

        /// <summary>
        /// Initializes a new instance of the <see cref="Persona"/> class.
        /// </summary>
        /// <param name="ped">
        /// The ped.
        /// </param>
        public Persona(CPed ped)
        {
            this.ped = ped;
            this.Gender = this.ped.Gender;
            this.Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Persona"/> class.
        /// </summary>
        /// <param name="ped">The ped.</param>
        /// <param name="gender">The gender.</param>
        /// <param name="birthDay">The birth day.</param>
        /// <param name="citations">The number of citations.</param>
        /// <param name="forename">The forename.</param>
        /// <param name="surname">The surname.</param>
        /// <param name="licenseState">The license state.</param>
        /// <param name="timesStopped">The times stopped.</param>
        /// <param name="wanted">Whether ped is wanted.</param>
        public Persona(CPed ped, GTA.Gender gender, DateTime birthDay, int citations, string forename, string surname, ELicenseState licenseState, int timesStopped, bool wanted)
        {
            this.ped = ped;
            this.Gender = gender;
            this.BirthDay = birthDay;
            this.Citations = citations;
            this.Forename = forename;
            this.Surname = surname;
            this.LicenseState = licenseState;
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
        /// Gets the gender.
        /// </summary>
        public GTA.Gender Gender { get; private set; }

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
        /// Gets the license state.
        /// </summary>
        public ELicenseState LicenseState { get; private set; }

        /// <summary>
        /// Gets the surname.
        /// </summary>
        public string Surname { get; private set; }

        /// <summary>
        /// Gets or sets the nuber of times stopped.
        /// </summary>
        public int TimesStopped { get; set; }

        /// <summary>
        /// Gets a value indicating whether or not the ped is wanted.
        /// </summary>
        public bool Wanted { get; private set; }

        /// <summary>
        /// Initializes all properties with random values.
        /// </summary>
        private void Initialize()
        {
            this.LoadPersonaData();

            // Generate random birth date. Use model info if available.
            CModelInfo modelInfo = this.ped.Model.ModelInfo;
            int randomMonth = Common.GetRandomValue(1, 13);
            int minAge = 18;
            int maxAge = 70;
            if (modelInfo != null)
            {
                if (modelInfo.ModelFlags.HasFlag(EModelFlags.IsYoung))
                {
                    maxAge = 28;
                }

                if (modelInfo.ModelFlags.HasFlag(EModelFlags.IsAdult))
                {
                    minAge = 29;
                    maxAge = 50;
                }

                if (modelInfo.ModelFlags.HasFlag(EModelFlags.IsOld))
                {
                    minAge = 51;
                    maxAge = 70;
                }
            }

            // Random year
            int randomYear = DateTime.Today.Year - Common.GetRandomValue(minAge, maxAge);

            // Random day
            int randomDay = Common.GetRandomValue(1, DateTime.DaysInMonth(randomYear, randomMonth));
            this.BirthDay = new DateTime(randomYear, randomMonth, randomDay);

            // Generate random name
            this.GetRandomName();

            // Generate random values
            this.Citations = Common.GetRandomValue(0, 4);
            this.TimesStopped = Common.GetRandomValue(this.Citations, this.Citations + 2);

            // Chance of 1/10 being wanted
            this.Wanted = Common.GetRandomBool(0, 10, 1);

            int licenseChance = Common.GetRandomValue(0, 10);
            if (licenseChance <= 7)
            {
                this.LicenseState = ELicenseState.Valid;
            }
            else if (licenseChance <= 8)
            {
                this.LicenseState = ELicenseState.Expired;
            }
            else if (licenseChance <= 9)
            {
                this.LicenseState = ELicenseState.Revoked;
            }
            else
            {
                this.LicenseState = ELicenseState.None;
            }
        }

        /// <summary>
        /// Sets a random name.
        /// </summary>
        private void GetRandomName()
        {
            // Ensure persona data is loaded
            this.LoadPersonaData();

            string[][] allNamesData = names;
            if (this.Gender == GTA.Gender.Female)
            {
                string[] femaleNameData = allNamesData[0];
                int randomNumber = Common.GetRandomValue(0, femaleNameData.Length - 1);
                this.Forename = femaleNameData[randomNumber];
            }

            if (this.Gender == GTA.Gender.Male)
            {
                string[] maleNameData = allNamesData[1];
                int randomNumber = Common.GetRandomValue(0, maleNameData.Length - 1);
                this.Forename = maleNameData[randomNumber];
            }

            string[] surnameData = allNamesData[2];
            int randomNum = Common.GetRandomValue(0, surnameData.Length - 1);
            this.Surname = surnameData[randomNum];

            // Only first letter should be capital
            this.Forename = this.Forename.ToLower();
            char[] letters = this.Forename.ToCharArray();
            letters[0] = char.ToUpper(letters[0]);
            this.Forename = new string(letters);

            this.Surname = this.Surname.ToLower();
            letters = this.Surname.ToCharArray();
            letters[0] = char.ToUpper(letters[0]);
            this.Surname = new string(letters);
        }
        
        /// <summary>
        /// Loads the persona data.
        /// </summary>
        private void LoadPersonaData()
        {
            if (names == null)
            {
                Log.Debug("Loading persona data", "Persona");
                string[] data = FileParser.ParseString(Properties.Resources.Names);

                string[] femaleNameData = FileParser.ParseStringData(data[0]);
                string[] maleNameData = FileParser.ParseStringData(data[1]);
                string[] surnameData = FileParser.ParseStringData(data[2]);
                string[][] allNamesData = new string[][] { femaleNameData, maleNameData, surnameData };
                names = allNamesData;
                Log.Debug("Persona data loaded", "Persona");
            }
        }
    }
}
