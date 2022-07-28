namespace LCPD_First_Response.Engine.Scripting.Entities
{
    using System;

    using GTA;

    /// <summary>
    /// Describes special flags of the ped.
    /// </summary>
    [Flags]
    internal enum EPedFlags
    {
        /// <summary>
        /// No flags.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Whether the ped can only be frisked.
        /// </summary>
        OnlyAllowFrisking = 0x1,

        /// <summary>
        /// Whether the ped has been frisked.
        /// </summary>
        HasBeenFrisked = 0x2,

        /// <summary>
        /// Whether the ped is at a roadblock.
        /// </summary>
        IsRoadblockPed = 0x4,

        /// <summary>
        /// The ped is the player ped in debug mode. This has various effects, e.g. won't proceed any chase logic.
        /// </summary>
        PlayerDebug = 0x8,

        /// <summary>
        /// When this flag is set, the ped will added to a chase no matter what the limit is and will never be disbanded because of too many units.
        /// </summary>
        IgnoreMaxUnitsLimitInChase = 0x10,

        /// <summary>
        /// Whether the ped won't show its license or ID when asked to.
        /// </summary>
        WontShowLicense = 0x20,

        /// <summary>
        /// Whether the ID of the ped has been checked by the player.
        /// </summary>
        IdHasBeenChecked = 0x40,
    }


    /// <summary>
    /// Stores ped (group) specific data will defines the ped's behavior.
    /// </summary>
    internal class PedData
    {
        /// <summary>
        /// The ped.
        /// </summary>
        private CPed ped;

        /// <summary>
        /// Describes the various types of luggage a ped can carry.
        /// </summary>
        [Flags]
        internal enum EPedLuggage
        {
            /// <summary>
            /// Carries drugs.
            /// </summary>
            Nothing = 0x1,

            /// <summary>
            /// Carries drugs.
            /// </summary>
            Drugs = 0x2,

            /// <summary>
            /// Carries weapons.
            /// </summary>
            Weapons = 0x4,

            /// <summary>
            /// Carries stolen credit cards.
            /// </summary>
            StolenCards = 0x8,
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PedData"/> class.
        /// </summary>
        /// <param name="ped">
        /// The ped.
        /// </param>
        public PedData(CPed ped)
        {
            this.ped = ped;

            this.Available = true;
            this.CanBeArrestedByPlayer = true;
            this.CanResistArrest = true;
            this.ComplianceChance = Common.GetRandomValue(70, 101);
            this.DefaultWeapon = Weapon.None;
            this.DropWeaponWhenAskedByCop = true;
            this.Flags = EPedFlags.None;
            this.Luggage = EPedLuggage.Nothing;
            this.Persona = new Persona(ped);
            this.SenseRange = 20f;
            this.ReportGainedVisual = true;
            this.ReportLostVisual = true;
            this.ReportBeingAttacked = true;
            this.LastScannerAreaOrStreet = "None";
            this.Speeding = false;

            if (Common.GetRandomBool(0, 7, 1))
            {
                this.Flags = EPedFlags.WontShowLicense;
            }
        }

        // Properties with get,set to manipulate behavior

        /// <summary>
        /// Gets or sets a value indicating whether the ped is available for a new task.
        /// </summary>
        public bool Available { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the ped will always resist arresting.
        /// </summary>
        public bool AlwaysResistArrest { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the ped will always surrender.
        /// </summary>
        public bool AlwaysSurrender { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the cop should provide a dispatch update after losing visual
        /// </summary>
        public bool ReportLostVisual { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the cop should provide a dispatch update after gaining visual
        /// </summary>
        public bool ReportGainedVisual { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the cop should report that they have been attacked by a suspect
        /// </summary>
        public bool ReportBeingAttacked { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the ped can be arrested by the player.
        /// </summary>
        public bool CanBeArrestedByPlayer { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether there is a chance the ped can resist arresting.
        /// </summary>
        public bool CanResistArrest { get; set; }

        /// <summary>
        /// Gets or sets the chance to comply when asked to stop. The maximum value is 100, the minimum is 0. The value is increased when the ped
        /// is being tased.
        /// </summary>
        public int ComplianceChance { get; set; }

        /// <summary>
        /// Gets or sets the current chase. Used for both, criminals and cops.
        /// </summary>
        public Chase CurrentChase { get; set; }

        /// <summary>
        /// Gets or sets the default weapon. Overrides ModelInfo.DefaultWeapon.
        /// </summary>
        public GTA.Weapon DefaultWeapon { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the chase AI of the ped is disabled, that is the fleeing task will no longer be assigned resulting in the ped doing nothing. Useful when you want the
        /// ped to be addeed to the chase already, but not yet want it to make any actions.
        /// </summary>
        public bool DisableChaseAI { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the ped will drop its weapon, if any, when asked by a cop.
        /// </summary>
        public bool DropWeaponWhenAskedByCop { get; set; }

        /// <summary>
        /// Gets or sets flags of the ped.
        /// </summary>
        public EPedFlags Flags { get; set; }

        /// <summary>
        /// Gets or sets the luggage of the ped.
        /// </summary>
        public EPedLuggage Luggage { get; set; }

        /// <summary>
        /// Gets or sets the last reported area or street of the ped.
        /// </summary>
        public string LastScannerAreaOrStreet { get; set; }
        
        /// <summary>
        /// Gets the persona data of the ped.
        /// </summary>
        public Persona Persona { get; private set; }

        /// <summary>
        /// Gets or sets the sense range to detect enemies during chase.
        /// </summary>
        public float SenseRange { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if the ped has been going over 50 speed in a chase
        /// </summary>
        public bool Speeding { get; set; }

        // Readonly properties that will return values based on the propery values set above

        /// <summary>
        /// Gets a value indicating whether the ped will stop. This check is based on <see cref="ComplianceChance"/> and also takes the current
        /// health of the ped into account, making it less likely to resist when low life.
        /// </summary>
        public bool WillStop
        {
            get
            {
                int missingHealth = this.ped.Health;
                if (missingHealth < 0)
                {
                    missingHealth = 0;
                }

                missingHealth = 100 - missingHealth;

                int compliance = this.ComplianceChance + missingHealth;
                if (compliance > 100)
                {
                    compliance = 100;
                }

                int range = 100 - compliance;
                return Common.GetRandomValue(0, 100) >= range;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the ped will surrender. Checks several flags for this.
        /// </summary>
        public bool WillSurrender
        {
            get
            {
                // If already surrendered
                if (this.ped.Wanted.Surrendered)
                {
                    return true;
                }

                if (this.AlwaysResistArrest)
                {
                    return false;
                }

                if (this.AlwaysSurrender)
                {
                    return true;
                }

                if (!this.CanResistArrest)
                {
                    return true;
                }

                if (this.Flags.HasFlag(EPedFlags.HasBeenFrisked))
                {
                    return true;
                }

                // There's a chance based on the compliance value
                return this.WillStop;
            }
        }

        // Variables/Fields used by some functions, but that are not so common

        /// <summary>
        /// Gets or sets a value indicating whether the ped has already investigated a crime scene. Used by <see cref="LCPD_First_Response.Engine.Scripting.Scenarios.ScenarioCopsInvestigateCrimeScene"/>.
        /// </summary>
        public bool AlreadyInvestigated { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the ped was asked to leave the vehicle by player.
        /// </summary>
        public bool AskedToLeaveVehicle { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether empty police vehicles (so no driver) are not allowed for the suspect as a transporter, 
        /// so either a new one has to be dispatched or a close one with a driver has to be used.
        /// </summary>
        public bool DontAllowEmptyVehiclesAsTransporter { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the ped has been tased.
        /// </summary>
        public bool HasBeenTased { get; set; }


        /// <summary>
        /// Gets the ped.
        /// </summary>
        protected CPed Ped
        {
            get
            {
                return this.ped;
            }
        }

        /// <summary>
        /// Replaces the current persona data with <paramref name="newData"/>.
        /// </summary>
        /// <param name="newData">The new data.</param>
        public void ReplacePersonaData(Persona newData)
        {
            this.Persona = newData;
        }
    }
}