namespace LCPD_First_Response.Engine.Scripting.Entities
{
    using System;

    /// <summary>
    /// Model flags describing the model.
    /// </summary>
    [Flags]
    public enum EModelFlags
    {
        /// <summary>
        ///  No flags.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// A model of a normal police unit, that is not a special unit.
        /// </summary>
        IsNormalUnit = 0x1,

        /// <summary>
        /// A cop car in general (so either noose, fbi or police).
        /// </summary>
        IsCopCar = 0x2,

        /// <summary>
        /// A cop in general.
        /// </summary>
        IsCop = 0x4,

        /// <summary>
        /// A helicopter.
        /// </summary>
        IsHelicopter = 0x8,

        /// <summary>
        /// A noose model.
        /// </summary>
        IsNoose = 0x10,

        /// <summary>
        /// A police model.
        /// </summary>
        IsPolice = 0x20,

        /// <summary>
        /// A traffic cop model.
        /// </summary>
        IsTrafficCop = 0x40,

        /// <summary>
        /// A model of a young ped (age 18-28).
        /// </summary>
        IsYoung = 0x80,

        /// <summary>
        /// A model of an adult (age 29-50).
        /// </summary>
        IsAdult = 0x100,

        /// <summary>
        /// A model of a pensioner (age 51-70).
        /// </summary>
        IsOld = 0x200,

        /// <summary>
        /// A model of a taxi.
        /// </summary>
        IsTaxi = 0x400,

        /// <summary>
        /// A model of either a police car, an ambulance or a firetruck.
        /// </summary>
        IsEmergencyServicesVehicle = 0x800,

        /// <summary>
        /// A model of a police vehicle capable of transporting suspects.
        /// </summary>
        IsSuspectTransporter = 0x1000,

        /// <summary>
        /// A ped model considered to be a bum.
        /// </summary>
        IsWealthBum = 0x2000,

        /// <summary>
        /// A ped model considered to be poor.
        /// </summary>
        IsWealthPoor = 0x4000,

        /// <summary>
        /// A ped model considered to belong to the lower class.
        /// </summary>
        IsWealthLowerClass = 0x8000,

        /// <summary>
        /// A ped model considered to belong to the middle class.
        /// </summary>
        IsWealthMidClass = 0x10000,

        /// <summary>
        /// A ped model considered to belong to the upper class.
        /// </summary>
        IsWealthUpperClass = 0x20000,

        /// <summary>
        /// A white ped model.
        /// </summary>
        IsRaceWhite = 0x40000,

        /// <summary>
        /// A black ped model.
        /// </summary>
        IsRaceBlack = 0x80000,

        /// <summary>
        /// An asian ped model.
        /// </summary>
        IsRaceAsian = 0x100000,

        /// <summary>
        /// A hispanic ped model.
        /// </summary>
        IsRaceHispanic = 0x200000,

        /// <summary>
        /// A FBI model.
        /// </summary>
        IsFBI = 0x400000,

        /// <summary>
        /// A model which can be used in Alderney
        /// </summary>
        IsAlderneyModel = 0x800000,

        /// <summary>
        /// A model which can be used in Broker, Dukes, Bohan and Algonquin
        /// </summary>
        IsLibertyModel = 0x1000000,

        /// <summary>
        /// A civilian, so no cop model.
        /// </summary>
        IsCivilian = 0x2000000,

        /// <summary>
        /// A vehicle in general.
        /// </summary>
        IsVehicle = 0x4000000,

        /// <summary>
        /// A ped in general.
        /// </summary>
        IsPed = 0x8000000,

        /// <summary>
        /// A ped with a job (e.g. medic, fireman, security, nurse)
        /// </summary>
        HasJob = 0x10000000,

        /// <summary>
        /// A cop boat.
        /// </summary>
        IsCopBoat = 0x20000000,
    }

    /// <summary>
    /// This class contains variables that specify what the model is and what it can be used for.
    /// </summary>
    public class CModelInfo
    {
        /// <summary>
        /// No model.
        /// </summary>
        public static readonly CModelInfo None = new CModelInfo("NONE", 0x0, EModelFlags.None);

        /// <summary>
        /// Initializes a new instance of the <see cref="CModelInfo"/> class.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="hash">
        /// The hash.
        /// </param>
        /// <param name="modelFlags">
        /// The model flags.
        /// </param>
        public CModelInfo(string name, uint hash, EModelFlags modelFlags)
        {
            this.Name = name;
            this.Hash = hash;
            this.ModelFlags = modelFlags;
            this.DefaultWeapon = GTA.Weapon.None;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CModelInfo"/> class.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="hash">
        /// The hash.
        /// </param>
        /// <param name="modelFlags">
        /// The model flags.
        /// </param>
        /// <param name="defaultWeapon">
        /// The default weapon.
        /// </param>
        public CModelInfo(string name, uint hash, EModelFlags modelFlags, GTA.Weapon defaultWeapon)
        {
            this.Name = name;
            this.Hash = hash;
            this.ModelFlags = modelFlags;
            this.DefaultWeapon = defaultWeapon;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CModelInfo"/> class.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="hash">
        /// The hash.
        /// </param>
        /// <param name="modelFlags">
        /// The model flags.
        /// </param>
        /// <param name="modelID">
        /// The model id.
        /// </param>
        public CModelInfo(string name, uint hash, EModelFlags modelFlags, int modelID)
        {
            this.Name = name;
            this.Hash = hash;
            this.ModelFlags = modelFlags;
            this.DefaultWeapon = GTA.Weapon.None;
            this.ModelID = modelID;
        }

        /// <summary>
        /// Gets the weapon CPed.EnsurePedHasWeapon will equip. Used for peds only.
        /// </summary>
        public GTA.Weapon DefaultWeapon { get; private set; }

        /// <summary>
        /// Gets the number of glasses models available for the model.
        /// </summary>
        /// <returns>The number of models.</returns>
        public int GetNumberOfGlasses()
        {
            // Random hacks for some models
            if (CPlayer.LocalPlayer.Model == "M_Y_COP")
            {
                return 1;
            }
            else if (CPlayer.LocalPlayer.Model == "M_M_FATCOP_01")
            {
                return 1;
            }
            else if (CPlayer.LocalPlayer.Model == "M_Y_COP_TRAFFIC")
            {
                return 0;
            }
            else if (CPlayer.LocalPlayer.Model == "M_Y_STROOPER")
            {
                return 1;
            }
            else if (CPlayer.LocalPlayer.Model == "M_Y_SWAT")
            {
                return 0;
            }
            else if (CPlayer.LocalPlayer.Model == "M_Y_NHELIPILOT")
            {
                return 0;
            }
            else if (CPlayer.LocalPlayer.Model == "M_M_FBI")
            {
                return 1;
            }
            else if (CPlayer.LocalPlayer.Model == "M_Y_CLUBFIT")
            {
                return 1;
            }
            else if (CPlayer.LocalPlayer.Model == "M_M_ARMOURED")
            {
                return 0;
            }
            else if (CPlayer.LocalPlayer.Model == "IG_FRANCIS_MC")
            {
                return 0;
            }
            else if (CPlayer.LocalPlayer.Model == "M_Y_CIADLC_01")
            {
                return 2;
            }
            else if (CPlayer.LocalPlayer.Model == "M_Y_CIADLC_02")
            {
                return 2;
            }

            return 0;
        }

        /// <summary>
        /// Gets the number of hat models available for the model.
        /// </summary>
        /// <returns>The number of models.</returns>
        public int GetNumberOfHats()
        {
            // Random hacks for some models
            if (CPlayer.LocalPlayer.Model == "M_Y_COP")
            {
                return 2;
            }
            else if (CPlayer.LocalPlayer.Model == "M_M_FATCOP_01")
            {
                return 2;
            }
            else if (CPlayer.LocalPlayer.Model == "M_Y_COP_TRAFFIC")
            {
                return 1;
            }
            else if (CPlayer.LocalPlayer.Model == "M_Y_STROOPER")
            {
                return 1;
            }
            else if (CPlayer.LocalPlayer.Model == "M_Y_SWAT")
            {
                return 3;
            }
            else if (CPlayer.LocalPlayer.Model == "M_Y_NHELIPILOT")
            {
                return 1;
            }
            else if (CPlayer.LocalPlayer.Model == "M_M_FBI")
            {
                return 1;
            }
            else if (CPlayer.LocalPlayer.Model == "M_Y_CLUBFIT")
            {
                return 0;
            }
            else if (CPlayer.LocalPlayer.Model == "M_M_ARMOURED")
            {
                return 1;
            }
            else if (CPlayer.LocalPlayer.Model == "IG_FRANCIS_MC")
            {
                return 0;
            }
            else if (CPlayer.LocalPlayer.Model == "M_Y_CIADLC_01")
            {
                return 0;
            }
            else if (CPlayer.LocalPlayer.Model == "M_Y_CIADLC_02")
            {
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// Adds <paramref name="flags"/> to the model.
        /// </summary>
        /// <param name="flags">The flags.</param>
        public void AddFlags(EModelFlags flags)
        {
            this.ModelFlags |= flags;
        }

        /// <summary>
        /// Sets the ped model's anim group to <paramref name="group"/>.
        /// </summary>
        /// <param name="flags">The flags.</param>
        public void SetAnimGroup(string group)
        {
            // Make sure no spaces
            string trimmedGroup = group.Trim();
            this.AnimGroup = trimmedGroup;
        }

        /// <summary>
        /// Gets the model hash.
        /// </summary>
        public uint Hash { get; private set; }

        /// <summary>
        /// Gets the model flags.
        /// </summary>
        public EModelFlags ModelFlags { get; private set; }

        /// <summary>
        /// Gets the internal model ID used by rockstar (used when spawning vehicles via advanced hook).
        /// </summary>
        public int ModelID { get; private set; }

        /// <summary>
        /// Gets the model name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the model's default animation group (PEDS ONLY!)
        /// </summary>
        public string AnimGroup { get; private set; }
    }
}