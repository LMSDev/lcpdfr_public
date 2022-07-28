namespace LCPD_First_Response.LCPDFR.API
{
    using System;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.LCPDFR.Scripts;

    /// <summary>
    /// The LCPDFRPed, which provides access to LCPDFR specific functions.
    /// </summary>
    public class LPed : LPedBase
    {
        /// <summary>
        /// Defines the ped group.
        /// </summary>
        public enum EPedGroup
        {
            /// <summary>
            /// A cop.
            /// </summary>
            Cop,

            /// <summary>
            /// A criminal.
            /// </summary>
            Criminal,

            /// <summary>
            /// A default mission ped.
            /// </summary>
            MissionPed,
        }

        /// <summary>
        /// Defines an item a ped can carry (like evidence).
        /// </summary>
        [Flags]
        public enum EPedItem
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
        /// Initializes a new instance of the <see cref="LPed"/> class.
        /// </summary>
        /// <param name="position">
        /// The position.
        /// </param>
        /// <param name="model">
        /// The model.
        /// </param>
        public LPed(GTA.Vector3 position, string model)
        {
            this.Ped = new CPed(model, position, Engine.Scripting.Entities.EPedGroup.MissionPed);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LPed"/> class.
        /// </summary>
        /// <param name="position">
        /// The position.
        /// </param>
        /// <param name="model">
        /// The model.
        /// </param>
        /// <param name="pedGroup">
        /// The ped group.
        /// </param>
        public LPed(GTA.Vector3 position, string model, EPedGroup pedGroup)
        {
            Engine.Scripting.Entities.EPedGroup group = Engine.Scripting.Entities.EPedGroup.MissionPed;
            if (pedGroup == EPedGroup.Cop)
            {
                group = Engine.Scripting.Entities.EPedGroup.Cop;
            }

            if (pedGroup == EPedGroup.Criminal)
            {
                group = Engine.Scripting.Entities.EPedGroup.Criminal;
            }

            this.Ped = new CPed(model, position, group);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LPed"/> class.
        /// </summary>
        /// <param name="ped">
        /// The ped.
        /// </param>
        internal LPed(CPed ped)
        {
            this.Ped = ped;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the ped will always surrender when being stopped.
        /// </summary>
        public bool AlwaysSurrender
        {
            get
            {
                return this.Ped.PedData.AlwaysSurrender;
            }

            set
            {
                this.Ped.PedData.AlwaysSurrender = value;
            }
        }

        /// <summary>
        /// Gets the blip assigned to the ped.
        /// </summary>
        public GTA.Blip Blip
        {
            get
            {
                return this.Ped.Blip;
            }
        }

        /// <summary>
        /// Gets the <see cref="GTA.Ped"/> representation.
        /// </summary>
        public GTA.Ped GPed
        {
            get
            {
                return (GTA.Ped)this.Ped;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the ped can be arrested by player.
        /// </summary>
        public bool CanBeArrestedByPlayer
        {
            get
            {
                return this.Ped.PedData.CanBeArrestedByPlayer;
            }

            set
            {
                this.Ped.PedData.CanBeArrestedByPlayer = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the ped can resist arrest.
        /// </summary>
        public bool CanResistArrest
        {
            get
            {
                return this.Ped.PedData.CanResistArrest;
            }

            set
            {
                this.Ped.PedData.CanResistArrest = value;
            }
        }

        /// <summary>
        /// Gets or sets the compliance chance for the ped (1-100). Based on this number, the chance a ped will resist arrest is calculated. The higher the value is, the less likely a ped will resist.
        /// </summary>
        public int ComplianceChance
        {
            get
            {
                return this.Ped.PedData.ComplianceChance;
            }

            set
            {
                this.Ped.PedData.ComplianceChance = value;
            }
        }

        /// <summary>
        /// Gets or sets the default weapon which will be used by <see cref="EquipWeapon"/>.
        /// </summary>
        public GTA.Weapon DefaultWeapon
        {
            get
            {
                return this.Ped.PedData.DefaultWeapon;
            }

            set
            {
                this.Ped.PedData.DefaultWeapon = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the AI for this ped is disabled in its current pursuit. Setting this to true makes the ped do nothing.
        /// </summary>
        public bool DisablePursuitAI
        {
            get
            {
                return this.Ped.PedData.DisableChaseAI;
            }

            set
            {
                this.Ped.PedData.DisableChaseAI = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the ped has been arrested.
        /// </summary>
        public bool HasBeenArrested
        {
            get
            {
                return this.Ped.Wanted.HasBeenArrested;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the ped is currently playing an ambient speech.
        /// </summary>
        public bool IsAmbientSpeechPlaying
        {
            get
            {
                return this.Ped.IsAmbientSpeechPlaying;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the ped is currently in a building respectively under a roof.
        /// </summary>
        /// <returns>True if on building, false otherwise.</returns>
        public bool IsInBuilding
        {
            get
            {
                return this.Ped.IsInBuilding();
            }
        }

        /// <summary>
        /// Gets a value indicating whether the ped is on a street. Not very reliable.
        /// </summary>
        public bool IsOnStreet
        {
            get
            {
                return this.Ped.IsOnStreet();
            }
        }

        /// <summary>
        /// Gets a value indicating whether the ped is a player.
        /// </summary>
        public bool IsPlayer
        {
            get
            {
                return this.Ped.IsPlayer;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the ped is being arrested.
        /// </summary>
        public bool IsBeingArrested
        {
            get
            {
                return this.Ped.Wanted.IsBeingArrested || this.Ped.Wanted.IsBeingArrestedByPlayer;
            }
        }

        /// <summary>
        /// Gets or sets the items carried by the ped. These items can be found when frisking the ped for instance. Note that the value uses <see cref="System.FlagsAttribute"/>.
        /// </summary>
        public EPedItem ItemsCarried
        {
            get
            {
                EPedItem items = EPedItem.Nothing;
                PedData.EPedLuggage luggage = this.Ped.PedData.Luggage;
                foreach (PedData.EPedLuggage value in Enum.GetValues(luggage.GetType()))
                {
                    if (luggage.HasFlag(value))
                    {
                        if (items.HasFlag(EPedItem.Nothing))
                        {
                            items = (EPedItem)(int)value;
                        }
                        else
                        {
                            items |= (EPedItem)(int)value;
                        }
                    }
                }

                return items;
            }

            set
            {
                PedData.EPedLuggage items = PedData.EPedLuggage.Nothing;
                foreach (EPedItem v in Enum.GetValues(this.ItemsCarried.GetType()))
                {
                    if (value.HasFlag(v))
                    {
                        if (items.HasFlag(PedData.EPedLuggage.Nothing))
                        {
                            items = (PedData.EPedLuggage)(int)v;
                        }
                        else
                        {
                            items |= (PedData.EPedLuggage)(int)v;
                        }
                    }
                }

                this.Ped.PedData.Luggage = items;
            }
        }

        /// <summary>
        /// Gets the persona data of the ped.
        /// </summary>
        public PersonaData PersonaData
        {
            get
            {
                return new PersonaData(this.Ped.PedData.Persona.BirthDay, this.Ped.PedData.Persona.Citations, this.Ped.PedData.Persona.Forename, this.Ped.PedData.Persona.Surname, this.Ped.PedData.Persona.LicenseState == ELicenseState.Valid, this.Ped.PedData.Persona.TimesStopped, this.Ped.PedData.Persona.Wanted);
            }

            set
            {
                ELicenseState state = ELicenseState.Valid;
                if (!value.HasValidLicense)
                {
                    state = ELicenseState.Expired;
                }

                Persona internalData = new Persona(this.Ped, this.Ped.Gender, value.BirthDay, value.Citations, value.Forename, value.Surname, state, value.TimesStopped, value.Wanted);
                this.Ped.PedData.ReplacePersonaData(internalData);
            }
        }

        /// <summary>
        /// Gets or sets the range in a pursuit where this ped can detect enemies.
        /// </summary>
        public float RangeToDetectEnemies
        {
            get
            {
                return this.Ped.PedData.SenseRange;
            }

            set
            {
                this.Ped.PedData.SenseRange = value;
            }
        }

        /// <summary>
        /// Gets the <see cref="CPed"/> representation.
        /// </summary>
        internal CPed CPed
        {
            get
            {
                return this.Ped;
            }
        }

        /// <summary>
        /// Performs a very basic check whether <paramref name="ped"/> can be seen. Doesn't include possible objects in the line of sight.
        /// </summary>
        /// <param name="ped">The ped.</param>
        /// <returns>Whether the ped can be seen.</returns>
        public bool CanSeePed(LPed ped)
        {
            return this.Ped.Intelligence.CanSeePed(ped.Ped);
        }

        /// <summary>
        /// Deletes the blip of the ped, if any.
        /// </summary>
        public void DeleteBlip()
        {
            this.Ped.DeleteBlip();
        }

        /// <summary>
        /// Draws <paramref name="text"/>  above the peds head for <paramref name="duration"/> milliseconds.
        /// </summary>
        /// <param name="text">The ped.</param>
        /// <param name="duration">The time</param>
        public void DrawTextAboveHead(string text, int duration)
        {
            this.Ped.Intelligence.SayText(text, duration);
        }

        /// <summary>
        /// Ensures the ped is not in a building by teleporting around <paramref name="position"/> if necessary.
        /// </summary>
        /// <param name="position">
        /// The position used as center.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool EnsurePedIsNotInBuilding(GTA.Vector3 position)
        {
            return this.Ped.EnsurePedIsNotInBuilding(position);
        }

        /// <summary>
        /// Equips a default weapon.
        /// </summary>
        public void EquipWeapon()
        {
            this.Ped.EnsurePedHasWeapon();
        }

        /// <summary>
        /// Returns whether <paramref name="ped"/> has been spotted by this ped. Optionally only works when ped is in front of this ped.
        /// </summary>
        /// <param name="ped">The ped.</param>
        /// <param name="inFront">Whether ped has to be spotted in front.</param>
        /// <returns>True if spotted, otherwise false.</returns>
        public bool HasSpottedPed(LPed ped, bool inFront)
        {
            if (inFront)
            {
                return this.Ped.HasSpottedChar(ped.Ped);
            }

            return this.Ped.HasSpottedCharInFront(ped.Ped);
        }

        /// <summary>
        /// Returns whether the ped is facing <paramref name="ped"/>.
        /// </summary>
        /// <param name="ped">The ped.</param>
        /// <returns></returns>
        public bool IsFacingChar(LPed ped)
        {
            return this.Ped.IsFacingChar(ped.Ped);
        }

        /// <summary>
        /// Makes the camera focus the ped for <paramref name="focusTime"/> milliseconds if player is close, on foot or driving slow and idle.
        /// Returns whether camera is focusing taking into account the requirements.
        /// </summary>
        /// <param name="focusTime">The time.</param>
        /// <returns>True if focusing, false if not.</returns>
        public bool MakeCameraFocus(int focusTime)
        {
            return CameraHelper.PerformEventFocus(this.Ped, true, 1000, focusTime, false, false, true);
        }

        /// <summary>
        /// Sets the ped's ID as recently checked by player, so its name will be displayed in the police computer.
        /// </summary>
        public void SetAsRecentlyChecked()
        {
            CPlayer.LocalPlayer.LastPedPulledOver = new CPed[] { this.Ped };
        }

        /// <summary>
        /// Makes the ped surrender and disables all actions in pursuit.
        /// </summary>
        public void Surrender()
        {
            this.Ped.Intelligence.Surrender();
        }

        /// <summary>
        /// Plays the walkie talkie animation with <paramref name="speech"/>.
        /// </summary>
        /// <param name="speech">The speech to say.</param>
        public void PlayWalkieTalkieAnimation(string speech)
        {
            TaskWalkieTalkie taskWalkieTalkie = new TaskWalkieTalkie(speech);
            taskWalkieTalkie.AssignTo(this.Ped, ETaskPriority.MainTask);
        }

        /// <summary>
        /// Creates a new instance of <see cref="LPed"/> from <paramref name="ped"/>.
        /// </summary>
        /// <param name="ped">The ped.</param>
        /// <returns>Converted ped instance.</returns>
        public static LPed FromGTAPed(GTA.Ped ped)
        {
            CPed customPed = Pools.PedPool.GetPedFromPool(ped);
            return new LPed(customPed);
        }

        /// <summary>
        /// Attaches a blip to the ped.
        /// </summary>
        /// <returns>The blip.</returns>
        //public new LBlip AttachBlip()
        //{
        //    this.Ped.AttachBlip();

        //    return new LBlip(this.Ped.Blip);
        //}
    }
}