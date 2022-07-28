namespace LCPD_First_Response.Engine.Scripting.Entities
{
    using System.Collections.Generic;
    using System.Linq;

    using GTA;
    using System.IO;
    using System.Text.RegularExpressions;

    /// <summary>
    /// The model manager.
    /// </summary>
    internal class ModelManager
    {
        /// <summary>
        /// If game is the ballad of gay tony.
        /// </summary>
        private bool isTbogt;

        /// <summary>
        /// Model info storage.
        /// </summary>
        private List<CModelInfo> modelInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelManager"/> class.
        /// </summary>
        /// <param name="isTbogt">
        /// The is tbogt.
        /// </param>
        public ModelManager(bool isTbogt)
        {
            this.modelInfo = new List<CModelInfo>();
            this.isTbogt = isTbogt;

            // Register all models, blame me for hardcoding this :)
            // Cop peds
            this.modelInfo.Add(new CModelInfo("M_Y_COP", 0xF5148AB2, EModelFlags.IsCop | EModelFlags.IsNormalUnit | EModelFlags.IsPolice | EModelFlags.IsLibertyModel | EModelFlags.HasJob, Weapon.Handgun_Glock));
            this.modelInfo.Add(new CModelInfo("M_M_FATCOP_01", 0xE9EC3678, EModelFlags.IsCop | EModelFlags.IsNormalUnit | EModelFlags.IsPolice | EModelFlags.IsLibertyModel | EModelFlags.HasJob, Weapon.Handgun_Glock));
            this.modelInfo.Add(new CModelInfo("M_Y_COP_TRAFFIC", 0xA576D885, EModelFlags.IsCop | EModelFlags.IsPolice | EModelFlags.IsTrafficCop | EModelFlags.IsLibertyModel | EModelFlags.HasJob, Weapon.Handgun_Glock));
            this.modelInfo.Add(new CModelInfo("M_Y_STROOPER", 0xFAAD5B99, EModelFlags.IsCop | EModelFlags.IsNormalUnit | EModelFlags.IsPolice | EModelFlags.IsAlderneyModel | EModelFlags.HasJob, Weapon.Handgun_Glock));
            this.modelInfo.Add(new CModelInfo("M_Y_SWAT", 0xC41C88BE, EModelFlags.IsCop | EModelFlags.IsNoose | EModelFlags.IsAlderneyModel | EModelFlags.IsLibertyModel | EModelFlags.HasJob, Weapon.Rifle_M4));
            this.modelInfo.Add(new CModelInfo("M_Y_NHELIPILOT", 0x479F2007, EModelFlags.IsCop | EModelFlags.IsHelicopter | EModelFlags.IsAlderneyModel | EModelFlags.IsLibertyModel | EModelFlags.HasJob, Weapon.Rifle_M4));
            this.modelInfo.Add(new CModelInfo("M_M_FBI", 0xC46CBC16, EModelFlags.IsCop | EModelFlags.IsFBI | EModelFlags.IsAlderneyModel | EModelFlags.IsLibertyModel | EModelFlags.HasJob, Weapon.Handgun_Glock));
            this.modelInfo.Add(new CModelInfo("M_Y_CLUBFIT", 0x2851C93C, EModelFlags.IsFBI | EModelFlags.HasJob, Weapon.Handgun_Glock)); // These peds spawn in the strip club, so best not to set them to IsCop
            this.modelInfo.Add(new CModelInfo("M_M_ARMOURED", 0x401C1901, EModelFlags.IsCop | EModelFlags.HasJob, Weapon.Handgun_Glock));
            this.modelInfo.Add(new CModelInfo("IG_FRANCIS_MC", 0x65F4D88D, EModelFlags.IsCop | EModelFlags.HasJob, Weapon.Handgun_Glock));
            if (this.isTbogt)
            {
                this.modelInfo.Add(new CModelInfo("M_Y_CIADLC_01", 0xE82B8B50, EModelFlags.IsCop | EModelFlags.HasJob, Weapon.Handgun_Glock));
                this.modelInfo.Add(new CModelInfo("M_Y_CIADLC_02", 0xFA832FFF, EModelFlags.IsCop | EModelFlags.IsFBI | EModelFlags.IsAlderneyModel | EModelFlags.IsLibertyModel | EModelFlags.HasJob, Weapon.Handgun_Glock));
            }

            // Cop vehicles
            // Note: How to retrieve model ID:               
            // IntPtr ptr = new IntPtr(((GTA.Vehicle)CPlayer.LocalPlayer.Ped.CurrentVehicle).MemoryAddress);
            // int dwModelID = *(byte*)(ptr.ToInt64() + 0x2e);
            this.modelInfo.Add(new CModelInfo("ANNIHILATOR", 0x31F0B376, EModelFlags.IsCopCar | EModelFlags.IsHelicopter | EModelFlags.IsEmergencyServicesVehicle));
            this.modelInfo.Add(new CModelInfo("FBI", 0x432EA949, EModelFlags.IsVehicle | EModelFlags.IsCopCar | EModelFlags.IsNoose | EModelFlags.IsFBI | EModelFlags.IsEmergencyServicesVehicle, 0x6F));
            this.modelInfo.Add(new CModelInfo("NOOSE", 0x08DE2A8B, EModelFlags.IsVehicle | EModelFlags.IsCopCar | EModelFlags.IsNoose | EModelFlags.IsEmergencyServicesVehicle, 0x88));
            this.modelInfo.Add(new CModelInfo("NSTOCKADE", 0x71EF6313, EModelFlags.IsVehicle | EModelFlags.IsCopCar | EModelFlags.IsNoose | EModelFlags.IsEmergencyServicesVehicle, 0x89));
            this.modelInfo.Add(new CModelInfo("POLICE", 0x79FBB0C5, EModelFlags.IsVehicle | EModelFlags.IsCopCar | EModelFlags.IsNormalUnit | EModelFlags.IsPolice | EModelFlags.IsEmergencyServicesVehicle, 0x93));
            this.modelInfo.Add(new CModelInfo("POLICE2", 0x9F05F101, EModelFlags.IsVehicle | EModelFlags.IsCopCar | EModelFlags.IsNormalUnit | EModelFlags.IsPolice | EModelFlags.IsEmergencyServicesVehicle, 0x94));
            this.modelInfo.Add(new CModelInfo("POLMAV", 0x1517D4D9, EModelFlags.IsCopCar | EModelFlags.IsNormalUnit | EModelFlags.IsHelicopter | EModelFlags.IsEmergencyServicesVehicle, 0x198));
            this.modelInfo.Add(new CModelInfo("POLPATRIOT", 0xEB221FC2, EModelFlags.IsVehicle | EModelFlags.IsCopCar | EModelFlags.IsNoose | EModelFlags.IsEmergencyServicesVehicle, 0x95));
            this.modelInfo.Add(new CModelInfo("PSTOCKADE", 0x8EB78F5A, EModelFlags.IsVehicle | EModelFlags.IsCopCar | EModelFlags.IsPolice | EModelFlags.IsEmergencyServicesVehicle, 0x9A));
            this.modelInfo.Add(new CModelInfo("PREDATOR", 0xE2E7D4AB, EModelFlags.IsCopBoat | EModelFlags.IsPolice | EModelFlags.IsEmergencyServicesVehicle));
            this.modelInfo.Add(new CModelInfo("DINGHY", 0x3D961290, EModelFlags.IsCivilian | EModelFlags.IsCopBoat | EModelFlags.IsNoose));
            if (this.isTbogt)
            {
                // Note: The models do NOT work as backup, because they appear to be missing some flags in order to be used by the DispatchPoliceCar function
                this.modelInfo.Add(new CModelInfo("POLICE3", 0x71FA16EA, EModelFlags.IsVehicle | EModelFlags.IsCopCar | EModelFlags.IsPolice | EModelFlags.IsEmergencyServicesVehicle, 0xA3));
                this.modelInfo.Add(new CModelInfo("POLICE4", 0x8A63C7B9, EModelFlags.IsVehicle | EModelFlags.IsCopCar | EModelFlags.IsPolice | EModelFlags.IsEmergencyServicesVehicle, 0xA5));
                this.modelInfo.Add(new CModelInfo("POLICEB", 0xFDEFAEC3, EModelFlags.IsVehicle | EModelFlags.IsCopCar | EModelFlags.IsPolice | EModelFlags.IsEmergencyServicesVehicle, 0xA6));
            }

            // Other EMS vehicles
            this.modelInfo.Add(new CModelInfo("AMBULANCE", 0x45D56ADA, EModelFlags.IsVehicle | EModelFlags.IsEmergencyServicesVehicle));
            this.modelInfo.Add(new CModelInfo("FIRETRUK", 0x73920F8E, EModelFlags.IsVehicle | EModelFlags.IsEmergencyServicesVehicle));

            // Normal vehicles
            // Note: This list is auto-generated using http://www.gtamodding.com/index.php?title=List_of_models_hashes, with only a few changes, e.g. taxis
            this.modelInfo.Add(new CModelInfo("ADMIRAL", 0x4B5C5320, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("AIRTUG", 0x5D0AAC8F, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("BANSHEE", 0xC1E908D2, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("BENSON", 0x7A61B330, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("BIFF", 0x32B91AE8, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("BLISTA", 0xEB70965F, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("BOBCAT", 0x4020325C, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("BOXVILLE", 0x898ECCEA, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("BUCCANEER", 0xD756460C, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("BURRITO", 0xAFBB2CA4, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("BURRITO2", 0xC9E8FF76, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("BUS", 0xD577C962, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("CABBY", 0x705A3E41, EModelFlags.IsCivilian | EModelFlags.IsVehicle | EModelFlags.IsTaxi));
            this.modelInfo.Add(new CModelInfo("CAVALCADE", 0x779F23AA, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("CHAVOS", 0xFBFD5B62, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("COGNOSCENTI", 0x86FE0B60, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("COMET", 0x3F637729, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("COQUETTE", 0x67BC037, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("DF8", 0x9B56631, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("DILETTANTE", 0xBC993509, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("DUKES", 0x2B26F456, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("E109", 0x8A765902, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("EMPEROR", 0xD7278283, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("EMPEROR2", 0x8FC3AADC, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("ESPERANTO", 0xEF7ED55D, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("FACTION", 0x81A9CDDF, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("FELTZER", 0xBE9075F1, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("FEROCI", 0x3A196CEA, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("FEROCI2", 0x3D285C4A, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("FLATBED", 0x50B0215A, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("FORTUNE", 0x255FC509, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("FORKLIFT", 0x58E49664, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("FUTO", 0x7836CE2F, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("FXT", 0x28420460, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("HABANERO", 0x34B7390F, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("HAKUMAI", 0xEB9F21D3, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("HUNTLEY", 0x1D06D681, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("INFERNUS", 0x18F25AC7, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("INGOT", 0xB3206692, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("INTRUDER", 0x34DD8AA1, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("LANDSTALKER", 0x4BA4E8DC, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("LOKUS", 0xFDCAF758, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("MANANA", 0x81634188, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("MARBELLA", 0x4DC293EA, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("MERIT", 0xB4D8797E, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("MINIVAN", 0xED7EADA4, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("MOONBEAM", 0x1F52A43F, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("MRTASTY", 0x22C16A2F, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("MULE", 0x35ED670B, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("ORACLE", 0x506434F6, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("PACKER", 0x21EEE87D, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("PATRIOT", 0xCFCFEB3B, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("PERENNIAL", 0x84282613, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("PERENNIAL2", 0xA1363020, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("PEYOTE", 0x6D19CCBC, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("PHANTOM", 0x809AA4CB, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("PINNACLE", 0x7D10BDC, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("PMP600", 0x5208A519, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("PONY", 0xF8DE29A8, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("PREMIER", 0x8FB66F9B, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("PRES", 0x8B0D2BA6, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("PRIMO", 0xBB6B404F, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("RANCHER", 0x52DB01E0, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("REBLA", 0x4F48FC4, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("RIPLEY", 0xCD935EF9, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("ROMERO", 0x2560B2FC, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("ROM", 0x8CD0264C, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("RUINER", 0xF26CEFF9, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("SABRE", 0xE53C7459, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("SABRE2", 0x4B5D021E, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("SABREGT", 0x9B909C94, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("SCHAFTER", 0xECC96C3F, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("SENTINEL", 0x50732C82, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("SOLAIR", 0x50249008, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("SPEEDO", 0xCFB3870C, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("STALION", 0x72A4C31E, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("STEED", 0x63FFE6EC, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("STOCKADE", 0x6827CF72, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("STRATUM", 0x66B4FC45, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("STRETCH", 0x8B13F083, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("SULTAN", 0x39DA2754, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("SULTANRS", 0xEE6024BC, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("SUPERGT", 0x6C9962A9, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("TAXI", 0xC703DB5F, EModelFlags.IsCivilian | EModelFlags.IsVehicle | EModelFlags.IsTaxi));
            this.modelInfo.Add(new CModelInfo("TAXI2", 0x480DAF95, EModelFlags.IsCivilian | EModelFlags.IsVehicle | EModelFlags.IsTaxi));
            this.modelInfo.Add(new CModelInfo("TRASH", 0x72435A19, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("TURISMO", 0x8EF34547, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("URANUS", 0x5B73F5B7, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("VIGERO", 0xCEC6B9B7, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("VIGERO2", 0x973141FC, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("VINCENT", 0xDD3BD501, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("VIRGO", 0xE2504942, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("VOODOO", 0x779B4F2D, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("WASHINGTON", 0x69F06B57, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("WILLARD", 0x737DAEC2, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("YANKEE", 0xBE6FF06A, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("BOBBER", 0x92E56A2C, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("FAGGIO", 0x9229E4EB, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("HELLFURY", 0x22DC8E7F, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("NRG900", 0x47B9138A, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("PCJ", 0xC9CEAF06, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("SANCHEZ", 0x2EF89E46, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("ZOMBIEB", 0xDE05FB87, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("MAVERICK", 0x9D0450CA, EModelFlags.IsCivilian | EModelFlags.IsHelicopter));
            this.modelInfo.Add(new CModelInfo("TOURMAV", 0x78D70477, EModelFlags.IsCivilian | EModelFlags.IsVehicle));
            this.modelInfo.Add(new CModelInfo("JETMAX", 0x33581161, EModelFlags.IsCivilian));
            this.modelInfo.Add(new CModelInfo("MARQUIS", 0xC1CE1183, EModelFlags.IsCivilian));
            this.modelInfo.Add(new CModelInfo("REEFER", 0x68E27CB6, EModelFlags.IsCivilian));
            this.modelInfo.Add(new CModelInfo("SQUALO", 0x17DF5EC2, EModelFlags.IsCivilian));
            this.modelInfo.Add(new CModelInfo("TUGA", 0x3F724E66, EModelFlags.IsCivilian));
            this.modelInfo.Add(new CModelInfo("TROPIC", 0x1149422F, EModelFlags.IsCivilian));
            this.modelInfo.Add(new CModelInfo("CABLECAR", 0xC6C3242D, EModelFlags.IsCivilian));
            this.modelInfo.Add(new CModelInfo("SUBWAY_LO", 0x2FBC4D30, EModelFlags.IsCivilian));
            this.modelInfo.Add(new CModelInfo("SUBWAY_HI", 0x8B887FDB, EModelFlags.IsCivilian));

            // Ambient peds
            // Note: List is auto-generated using Abraxas data as a base (http://www.lcpdfr.com/topic/18044-10-for-developers/). DATA NOT CONFIRMED means the tool generating the list
            // couldn't find any data associated to the model in Abraxas records and guessed all flags
            this.modelInfo.Add(new CModelInfo("PLAYER", 0x6F0783F5, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_MULTIPLAYER", 0x879495E2, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_Y_MULTIPLAYER", 0xD9BDC03A, EModelFlags.IsPed| EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("SUPERLOD", 0xAE4B15D6, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_ANNA", 0x6E7BF45F, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_ANTHONY", 0x9DD666EE, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_BADMAN", 0x5927A320, EModelFlags.IsPed | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_BERNIE_CRANE", 0x596FB508, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_BLEDAR", 0x6734C2C8, EModelFlags.IsPed | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_BRIAN", 0x192BDD4A, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_BRUCIE", 0x98E29920, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_BULGARIN", 0xE28247F, EModelFlags.IsPed | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_CHARISE", 0x548F609, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_CHARLIEUC", 0xB0D18783, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_CLARENCE", 0x500EC110, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_DARDAN", 0x5786C78F, EModelFlags.IsPed | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_DARKO", 0x1709B920, EModelFlags.IsPed | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_DERRICK_MC", 0x45B445F9, EModelFlags.IsPed | EModelFlags.IsOld | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_DMITRI", 0xE27ECC1, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_DWAYNE", 0xDB354C19, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_EDDIELOW", 0xA09901F1, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_FAUSTIN", 0x3691799, EModelFlags.IsPed | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_FRENCH_TOM", 0x54EABEE4, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_GORDON", 0x7EED7363, EModelFlags.IsPed | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_GRACIE", 0xEAAEA78E, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_HOSSAN", 0x3A7556B2, EModelFlags.IsPed | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_ILYENA", 0xCE3779DA, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_ISAAC", 0xE369F2A6, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_IVAN", 0x458B61F3, EModelFlags.IsPed | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_JAY", 0x15BCAD23, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_JASON", 0xA2D8896, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_JEFF", 0x17446345, EModelFlags.IsPed | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_JIMMY", 0xEA28DB14, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_JOHNNYBIKER", 0xC9AB7F1C, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_KATEMC", 0xD1E17FCA, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_KENNY", 0x3B574ABA, EModelFlags.IsPed | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_LILJACOB", 0x58A1E271, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_LILJACOBW", 0xB4008E4D, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_LUCA", 0xD75A60C8, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_LUIS", 0xE2A57E5E, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_MALLORIE", 0xC1FE7952, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_MAMC", 0xECC3FBA7, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_MANNY", 0x5629F011, EModelFlags.IsPed | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_MARNIE", 0x188232D0, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_MEL", 0xCFE0FB92, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_MICHAEL", 0x2BD27039, EModelFlags.IsPed | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_MICHELLE", 0xBF9672F4, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_MICKEY", 0xDA0D3182, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_PACKIE_MC", 0x64C74D3B, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_PATHOS", 0xF6237664, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_PETROVIC", 0x8BE8B7F2, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_PHIL_BELL", 0x932272CA, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_PLAYBOY_X", 0x6AF081E8, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_RAY_BOCCINO", 0x38E02AB6, EModelFlags.IsPed | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_RICKY", 0xDCFE251C, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_ROMAN", 0x89395FC9, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_ROMANW", 0x2145C7A5, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_SARAH", 0xFEF00775, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_TUNA", 0x528AE104, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_VINNY_SPAZ", 0xC380AE97, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_VLAD", 0x356E1C42, EModelFlags.IsPed | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_ANDREI", 0x3977107D, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_ANGIE", 0xF866DC66, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_BADMAN", 0xFC012F67, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_BLEDAR", 0xA2DDDBA7, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_BULGARIN", 0x9E4F3E, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_BULGARINHENCH", 0x1F32DB93, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_CIA", 0x4B13F8D4, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_DARDAN", 0xF4386436, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_DAVETHEMATE", 0x1A5B22F0, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_DMITRI", 0x30B4624, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_EDTHEMATE", 0xC74969B0, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_FAUSTIN", 0xA776BDC7, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_FRANCIS", 0x4AA2E9EA, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_HOSSAN", 0x2B578C90, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_ILYENA", 0x2EB3F295, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_IVAN", 0x4A85C1C4, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_JAY", 0x96E9F99A, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_JIMMY_PEGORINO", 0x7055C230, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_MEL", 0x298ACEC3, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_MICHELLE", 0x70AEB9C8, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_MICKEY", 0xA1DFB431, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_OFFICIAL", 0x311DB819, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_RAY_BOCCINO", 0xD09ECB11, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_SERGEI", 0xDBAC6805, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_VLAD", 0x7F5B9540, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_WHIPPINGGIRL", 0x5A6C9C5F, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_MANNY", 0xD0F8F893, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_ANTHONY", 0x6B941ABA, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_ASHLEY", 0x26C3D079, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_ASSISTANT", 0x394C11AD, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_CAPTAIN", 0xE6829281, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_CHARLIEUC", 0xEC96EE3A, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_DARKO", 0xC4B4204C, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_DWAYNE", 0xFB9190AC, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_ELI_JESTER", 0x3D47C135, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_ELIZABETA", 0xAED416AF, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_GAYTONY", 0x4F78844, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_GERRYMC", 0x26DE3A8A, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_GORDON", 0x49D3EAD3, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_ISSAC", 0xB93A5686, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_JOHNNYTHEBIKER", 0x2E009A8D, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_JONGRAVELLI", 0xD7D47612, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_JORGE", 0x5906B7A5, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_KAT", 0x71A11E4C, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_KILLER", 0xB4D0F581, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_LUIS", 0x5E730218, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_MAGICIAN", 0x1B508682, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_MAMC", 0xA17C3253, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_MELODY", 0xEA01EFDC, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_MITCHCOP", 0xD8BA6C47, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_MORI", 0x9B333E73, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_PBXGIRL2", 0xE9C3C332, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_PHILB", 0x5BEB1A2D, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_PLAYBOYX", 0xE9F368C6, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_PRIEST", 0x4D6DE57E, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_RICKY", 0x88F35A20, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_TOMMY", 0x626C3F77, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_TRAMP", 0x553CBE07, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_BRIAN", 0x2AF6831D, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_CHARISE", 0x7AE0A064, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_CLARENCE", 0xE7AC8418, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_EDDIELOW", 0x6463855D, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_GRACIE", 0x999B9B33, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_JEFF", 0x17C32FB4, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_MARNIE", 0x574DE134, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_MARSHAL", 0x8B0322AF, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_PATHOS", 0xD77D71DF, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_SARAH", 0xEFF3F84D, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_ROMAN_D", 0x42F6375E, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_ROMAN_T", 0x6368F847, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_ROMAN_W", 0xE37B786A, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_BRUCIE_B", 0xE37C613, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_BRUCIE_T", 0xE1B45E6, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_BRUCIE_W", 0x765C9667, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_BERNIE_CRANEC", 0x7183C75F, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_BERNIE_CRANET", 0x4231E7AC, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_BERNIE_CRANEW", 0x1B4899DE, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_LILJACOB_B", 0xB0B4BC37, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_LILJACOB_J", 0x7EF858B3, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_MALLORIE_D", 0x5DF63F45, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_MALLORIE_J", 0xCC381BCB, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_MALLORIE_W", 0x45768E2E, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_DERRICKMC_B", 0x8469C377, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_DERRICKMC_D", 0x2FBC9A1E, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_MICHAEL_B", 0x7D0BADD3, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_MICHAEL_D", 0xCF5FD27A, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_PACKIEMC_B", 0x4DFB1B0C, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_PACKIEMC_D", 0x68EED0F3, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_KATEMC_D", 0xAF3F2AC0, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("CS_KATEMC_W", 0x4ABDE1C7, EModelFlags.IsPed | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GAFR_LO_01", 0xEE0BB2A4, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GAFR_LO_02", 0xBBD14E30, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GAFR_HI_01", 0x33D38899, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceBlack));
            this.modelInfo.Add(new CModelInfo("M_Y_GAFR_HI_02", 0x25B4EC5C, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceBlack));
            this.modelInfo.Add(new CModelInfo("M_Y_GALB_LO_01", 0xE1F6A366, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GALB_LO_02", 0xF1F54363, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GALB_LO_03", 0xC61783B, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_GALB_LO_04", 0x1EA71CCE, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_M_GBIK_LO_03", 0x29035B4, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_GBIK_HI_01", 0x5044865F, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_GBIK_HI_02", 0x9C071DE3, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GBIK02_LO_02", 0xA8E69DBF, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GBIK_LO_01", 0x5DDE4F9B, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_GBIK_LO_02", 0x8B932B00, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GIRI_LO_01", 0x10B7B44B, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_GIRI_LO_02", 0xFEDA1090, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GIRI_LO_03", 0x6DF3EEC6, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_M_GJAM_HI_01", 0x5FF2E9AF, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceBlack));
            this.modelInfo.Add(new CModelInfo("M_M_GJAM_HI_02", 0xEC4D0269, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_GJAM_HI_03", 0x4295AEF5, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceBlack));
            this.modelInfo.Add(new CModelInfo("M_Y_GJAM_LO_01", 0xA691BED3, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GJAM_LO_02", 0xCB77889E, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GKOR_LO_01", 0x5BD063B5, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceAsian));
            this.modelInfo.Add(new CModelInfo("M_Y_GKOR_LO_02", 0x2D8D8730, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceAsian));
            this.modelInfo.Add(new CModelInfo("M_Y_GLAT_LO_01", 0x1D55921C, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceHispanic));
            this.modelInfo.Add(new CModelInfo("M_Y_GLAT_LO_02", 0x8D32F1D9, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GLAT_HI_01", 0x45A43081, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceHispanic));
            this.modelInfo.Add(new CModelInfo("M_Y_GLAT_HI_02", 0x97E25504, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GMAF_HI_01", 0xEDFA50E3, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GMAF_HI_02", 0x9FA03430, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GMAF_LO_01", 0x3DBB737, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_GMAF_LO_02", 0x1E6BEC57, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_O_GRUS_HI_01", 0x9290C4A3, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsOld | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GRUS_LO_01", 0x83892528, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GRUS_LO_02", 0x75CF09B4, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_GRUS_HI_02", 0x5BFE7C54, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_M_GRU2_HI_01", 0x6F31C4B4, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_M_GRU2_HI_02", 0x19BB19C8, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_M_GRU2_LO_02", 0x66CB1E64, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_GRU2_LO_01", 0xB9A05501, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_GTRI_HI_01", 0x33EEB47F, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceAsian));
            this.modelInfo.Add(new CModelInfo("M_M_GTRI_HI_02", 0x28C09E23, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceAsian));
            this.modelInfo.Add(new CModelInfo("M_Y_GTRI_LO_01", 0xBF635A9F, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GTRI_LO_02", 0xF62B4836, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_O_MAID_01", 0xD33B8FE9, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsOld | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_O_BINCO", 0xF97D04E6, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsOld | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_Y_BANK_01", 0x516F7106, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthMidClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob));
            this.modelInfo.Add(new CModelInfo("F_Y_DOCTOR_01", 0x14A4B50F, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthUpperClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob));
            this.modelInfo.Add(new CModelInfo("F_Y_GYMGAL_01", 0x507AAC5B, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthMidClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("F_Y_FF_BURGER_R", 0x37214098, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite | EModelFlags.HasJob));
            this.modelInfo.Add(new CModelInfo("F_Y_FF_CLUCK_R", 0xEB5AB08B, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_Y_FF_RSCAFE", 0x8292BFB5, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_Y_FF_TWCAFE", 0xCB09BED, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite | EModelFlags.HasJob));
            this.modelInfo.Add(new CModelInfo("F_Y_FF_WSPIZZA_R", 0xEEB5DE91, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_Y_HOOKER_01", 0x20EF1FEB, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthPoor | EModelFlags.IsRaceBlack));
            this.modelInfo.Add(new CModelInfo("F_Y_HOOKER_03", 0x3B61D4D0, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("F_Y_NURSE", 0xB8D8632B, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_Y_STRIPPERC01", 0x42615D12, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite | EModelFlags.HasJob));
            this.modelInfo.Add(new CModelInfo("F_Y_STRIPPERC02", 0x50AFF9AF, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthPoor | EModelFlags.IsRaceBlack | EModelFlags.HasJob));
            this.modelInfo.Add(new CModelInfo("F_Y_WAITRESS_01", 0x171C5D1, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthMidClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob));
            this.modelInfo.Add(new CModelInfo("M_M_ALCOHOLIC", 0x97093869, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_BUSDRIVER", 0x7FDDC3F, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthPoor | EModelFlags.IsRaceBlack | EModelFlags.HasJob));
            this.modelInfo.Add(new CModelInfo("M_M_CHINATOWN_01", 0x2D243DEF, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceAsian));
            this.modelInfo.Add(new CModelInfo("M_M_CRACKHEAD", 0x9313C198, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_DOC_SCRUBS_01", 0xD13AEF5, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite | EModelFlags.HasJob)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_DOCTOR_01", 0xB940B896, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_DODGYDOC", 0x16653776, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthUpperClass | EModelFlags.IsRaceBlack));
            this.modelInfo.Add(new CModelInfo("M_M_EECOOK", 0x7D77FE8D, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_M_ENFORCER", 0xF410AB9B, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_FACTORY_01", 0x2FB107C1, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthPoor | EModelFlags.IsRaceBlack));
            this.modelInfo.Add(new CModelInfo("M_M_FEDCO", 0x89275CA8, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_FIRECHIEF", 0x24696C93, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthUpperClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob));
            this.modelInfo.Add(new CModelInfo("M_M_GUNNUT_01", 0x1CFC648F, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_M_HELIPILOT_01", 0xD19BD6D0, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_HPORTER_01", 0x2536480C, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob));
            this.modelInfo.Add(new CModelInfo("M_M_KOREACOOK_01", 0x959D9B8A, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_LAWYER_01", 0x918DD1CF, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthUpperClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_LAWYER_02", 0xBC5DA76E, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthUpperClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_LOONYBLACK", 0x1699B3B8, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthPoor | EModelFlags.IsRaceBlack));
            this.modelInfo.Add(new CModelInfo("M_M_PILOT", 0x8C0F140E, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthMidClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_PINDUS_01", 0x301D7295, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_M_POSTAL_01", 0xEF0CF791, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_SAXPLAYER_01", 0xB92CCD03, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_SECURITYMAN", 0x907AF88D, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_SELLER_01", 0x1916A97C, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthPoor | EModelFlags.IsRaceHispanic | EModelFlags.HasJob));
            this.modelInfo.Add(new CModelInfo("M_M_SHORTORDER", 0x6FF14E0F, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthPoor | EModelFlags.IsRaceHispanic));
            this.modelInfo.Add(new CModelInfo("M_M_STREETFOOD_01", 0x881E67C, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite | EModelFlags.HasJob));
            this.modelInfo.Add(new CModelInfo("M_M_SWEEPER", 0xD6D5085C, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_TAXIDRIVER", 0x85DCEE, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite | EModelFlags.HasJob)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_TELEPHONE", 0x46B50EAA, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob));
            this.modelInfo.Add(new CModelInfo("M_M_TENNIS", 0xE96555E2, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_TRAIN_01", 0x452086C4, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthPoor | EModelFlags.IsRaceBlack | EModelFlags.HasJob));
            this.modelInfo.Add(new CModelInfo("M_M_TRAMPBLACK", 0xF7835A1A, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_TRUCKER_01", 0xFD3979FD, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_O_JANITOR", 0xB376FD38, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsOld | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_O_HOTEL_FOOT", 0x15E1A07, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsOld | EModelFlags.IsWealthMidClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob));
            this.modelInfo.Add(new CModelInfo("M_O_MPMOBBOSS", 0x463E4B5D, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_AIRWORKER", 0xA8B24166, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_BARMAN_01", 0x80807842, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_BOUNCER_01", 0x95DCB0F5, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_BOUNCER_02", 0xE79AD470, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_BOWL_01", 0xD05CB843, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_BOWL_02", 0xE61EE3C7, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_CHINVEND_01", 0x2DCD7F4C, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthPoor | EModelFlags.IsRaceAsian));
            this.modelInfo.Add(new CModelInfo("M_Y_CONSTRUCT_01", 0xD4F6DA2A, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_CONSTRUCT_02", 0xC371B720, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_CONSTRUCT_03", 0xD56DDB14, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_COURIER", 0xAE46285D, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_COWBOY_01", 0xDDCCAF85, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_DEALER", 0xB380C536, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_DRUG_01", 0x565A4099, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthPoor | EModelFlags.IsRaceHispanic));
            this.modelInfo.Add(new CModelInfo("M_Y_FF_BURGER_R", 0xF192D, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthPoor | EModelFlags.IsRaceBlack | EModelFlags.HasJob));
            this.modelInfo.Add(new CModelInfo("M_Y_FF_CLUCK_R", 0xC3B54549, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_FF_RSCAFE", 0x75FDB605, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite | EModelFlags.HasJob));
            this.modelInfo.Add(new CModelInfo("M_Y_FF_TWCAFE", 0xD11FBA8B, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_FF_WSPIZZA_R", 0xC55ACF1, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthPoor | EModelFlags.IsRaceBlack | EModelFlags.HasJob));
            this.modelInfo.Add(new CModelInfo("M_Y_FIREMAN", 0xDBA0B619, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GARBAGE", 0x43BD9C04, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite | EModelFlags.HasJob)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GOON_01", 0x358464B5, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthMidClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_GYMGUY_01", 0x8E96352C, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_MECHANIC_02", 0xEABA11B9, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_MODO", 0xC10A9D57, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_PERSEUS", 0xF6FFEBB2, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_PINDUS_01", 0x1DDEBBCF, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_PINDUS_02", 0xB1F9651, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthPoor | EModelFlags.IsRaceHispanic));
            this.modelInfo.Add(new CModelInfo("M_Y_PINDUS_03", 0xF958F2C4, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_PMEDIC", 0xB9F5BEA0, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_PRISON", 0x9C0BF5CC, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_PRISONAOM", 0xCD38A07, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsOld | EModelFlags.IsWealthBum | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_ROMANCAB", 0x5C907185, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_RUNNER", 0xA7ABA2BA, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_SHOPASST_01", 0x15556BF3, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob));
            this.modelInfo.Add(new CModelInfo("M_Y_SWORDSWALLOW", 0xFC2BE1B8, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_THIEF", 0xB2F9C1A1, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_VALET", 0x102B77F0, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite | EModelFlags.HasJob));
            this.modelInfo.Add(new CModelInfo("M_Y_VENDOR", 0xF4E8205B, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite | EModelFlags.HasJob)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_FRENCHTOM", 0x87DB1287, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_JIM_FITZ", 0x75E29A7D, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_O_PEASTEURO_01", 0xF3D9C032, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsOld | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_O_PEASTEURO_02", 0xB50EF20, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsOld | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("F_O_PHARBRON_01", 0xEB320486, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsOld | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_O_PJERSEY_01", 0xF92630A4, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsOld | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_O_PORIENT_01", 0x9AD4BE64, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsOld | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_O_RICH_01", 0x600A909, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsOld | EModelFlags.IsWealthUpperClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("F_M_BUSINESS_01", 0x93E163C, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthUpperClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("F_M_BUSINESS_02", 0x1780B2C1, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthUpperClass | EModelFlags.IsRaceBlack));
            this.modelInfo.Add(new CModelInfo("F_M_CHINATOWN", 0x51FFF4A5, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceAsian));
            this.modelInfo.Add(new CModelInfo("F_M_PBUSINESS", 0xEF0F006B, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_M_PEASTEURO_01", 0x2864B0DC, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("F_M_PHARBRON_01", 0xB92CE9DD, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_M_PJERSEY_01", 0x844EA438, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_M_PJERSEY_02", 0xAF1EF9D8, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_M_PLATIN_01", 0x3067DA63, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthPoor | EModelFlags.IsRaceHispanic));
            this.modelInfo.Add(new CModelInfo("F_M_PLATIN_02", 0xF84BEA2C, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_M_PMANHAT_01", 0x32CEF1D1, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthUpperClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("F_M_PMANHAT_02", 0x4901554, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthUpperClass | EModelFlags.IsRaceBlack));
            this.modelInfo.Add(new CModelInfo("F_M_PORIENT_01", 0x81BA39A8, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_M_PRICH_01", 0x605DF31F, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsOld | EModelFlags.IsWealthUpperClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("F_Y_BUSINESS_01", 0x1B0DCC86, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthUpperClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("F_Y_CDRESS_01", 0x3120FC7F, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthUpperClass | EModelFlags.IsRaceAsian));
            this.modelInfo.Add(new CModelInfo("F_Y_PBRONX_01", 0xAECAC8C7, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_Y_PCOOL_01", 0x9568444C, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_Y_PCOOL_02", 0xA52AE3D1, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_Y_PEASTEURO_01", 0xC760585B, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_Y_PHARBRON_01", 0x8D2AC355, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_Y_PHARLEM_01", 0xA047A8F, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceBlack));
            this.modelInfo.Add(new CModelInfo("F_Y_PJERSEY_02", 0x6BC78, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("F_Y_PLATIN_01", 0x339B6D8, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthPoor | EModelFlags.IsRaceHispanic));
            this.modelInfo.Add(new CModelInfo("F_Y_PLATIN_02", 0xEE8D8D80, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_Y_PLATIN_03", 0x67F08048, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceHispanic));
            this.modelInfo.Add(new CModelInfo("F_Y_PMANHAT_01", 0x6392D986, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthMidClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("F_Y_PMANHAT_02", 0x50B8B3D2, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthUpperClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("F_Y_PMANHAT_03", 0x3EFE105D, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthUpperClass | EModelFlags.IsRaceBlack));
            this.modelInfo.Add(new CModelInfo("F_Y_PORIENT_01", 0xB8DA98D7, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_Y_PQUEENS_01", 0x2A8A0FF0, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthPoor | EModelFlags.IsRaceBlack));
            this.modelInfo.Add(new CModelInfo("F_Y_PRICH_01", 0x95E177F9, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_Y_PVILLBO_02", 0xC73ECED1, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_Y_SHOP_03", 0x5E8CD2B8, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthUpperClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("F_Y_SHOP_04", 0x6E2671EB, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthMidClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("F_Y_SHOPPER_05", 0x9A8CFCFD, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_Y_SOCIALITE", 0x4680C12E, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthMidClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("F_Y_STREET_02", 0xCA5194CB, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_Y_STREET_05", 0x110C2243, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("F_Y_STREET_09", 0x57D62FD6, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthMidClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("F_Y_STREET_12", 0x91AFE421, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_Y_STREET_30", 0x4CEF5CF5, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("F_Y_STREET_34", 0x6F96222E, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceBlack));
            this.modelInfo.Add(new CModelInfo("F_Y_TOURIST_01", 0x6892A334, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthMidClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("F_Y_VILLBO_01", 0x2D6795BA, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthMidClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_M_BUSINESS_02", 0xDA0E92D1, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_BUSINESS_03", 0x976C0D95, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_EE_HEAVY_01", 0xA59C6FD2, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_EE_HEAVY_02", 0x9371CB7D, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_FATMOB_01", 0x74636532, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_M_GAYMID", 0x894A8CB2, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_GENBUM_01", 0xBF963CE7, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_LOONYWHITE", 0x1D88B92A, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthBum | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_M_MIDTOWN_01", 0x89BC811F, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_PBUSINESS_01", 0x3F688D84, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthUpperClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_M_PEASTEURO_01", 0xC717BCE, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_M_PHARBRON_01", 0xC3306A8C, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_PINDUS_02", 0x6A3B66CC, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthPoor | EModelFlags.IsRaceBlack));
            this.modelInfo.Add(new CModelInfo("M_M_PITALIAN_01", 0xAC686EC9, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_PITALIAN_02", 0x9EF053D9, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_PLATIN_01", 0x450E5DBF, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceHispanic));
            this.modelInfo.Add(new CModelInfo("M_M_PLATIN_02", 0x75633E74, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceHispanic));
            this.modelInfo.Add(new CModelInfo("M_M_PLATIN_03", 0x60AD1508, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceHispanic));
            this.modelInfo.Add(new CModelInfo("M_M_PMANHAT_01", 0xD8CF835D, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_PMANHAT_02", 0xB217B5E2, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_PORIENT_01", 0x2BC50FD3, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthPoor | EModelFlags.IsRaceAsian));
            this.modelInfo.Add(new CModelInfo("M_M_PRICH_01", 0x6F2AE4DB, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthUpperClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_O_EASTEURO_01", 0xE6372469, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsOld | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_O_HASID_01", 0x9E495AD7, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsOld | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_O_MOBSTER", 0x62B5E24B, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsOld | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_O_PEASTEURO_02", 0x793F36B1, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsOld | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_O_PHARBRON_01", 0x4E76BDF6, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsOld | EModelFlags.IsWealthMidClass | EModelFlags.IsRaceBlack));
            this.modelInfo.Add(new CModelInfo("M_O_PJERSEY_01", 0x3A78BA45, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsOld | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_O_STREET_01", 0xB29788AB, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsOld | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_O_SUITED", 0xE86251C, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsOld | EModelFlags.IsWealthUpperClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_BOHO_01", 0x7C54115F, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthMidClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_BOHOGUY_01", 0xD2FF2BF, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthMidClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_BRONX_01", 0x31EE9E3, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceBlack));
            this.modelInfo.Add(new CModelInfo("M_Y_BUSINESS_01", 0x5B404032, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthUpperClass | EModelFlags.IsRaceBlack));
            this.modelInfo.Add(new CModelInfo("M_Y_BUSINESS_02", 0x2924DBD8, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthUpperClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_CHINATOWN_03", 0xBB784DE6, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_CHOPSHOP_01", 0xED4319C3, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_CHOPSHOP_02", 0xDF0C7D56, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_DODGY_01", 0xBE9A3CD6, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_DORK_02", 0x962996E4, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_DOWNTOWN_01", 0x47F77FC9, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_DOWNTOWN_02", 0x5971A2B9, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthMidClass | EModelFlags.IsRaceAsian));
            this.modelInfo.Add(new CModelInfo("M_Y_DOWNTOWN_03", 0x236BB6B2, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_GAYYOUNG", 0xD36D1B5D, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GENSTREET_11", 0xD7A357ED, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GENSTREET_16", 0x9BF260A8, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GENSTREET_20", 0x3AF39D6C, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_GENSTREET_34", 0x4658B34E, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceBlack));
            this.modelInfo.Add(new CModelInfo("M_Y_HARDMAN_01", 0xAB537AD4, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_HARLEM_01", 0xB71B0F29, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_HARLEM_02", 0x97EBD0CB, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_HARLEM_04", 0x7D701BD4, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceHispanic));
            this.modelInfo.Add(new CModelInfo("M_Y_HASID_01", 0x90442A67, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_LEASTSIDE_01", 0xC1181556, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_PBRONX_01", 0x22522444, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceBlack));
            this.modelInfo.Add(new CModelInfo("M_Y_PCOOL_01", 0xFBB5AA01, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_PCOOL_02", 0xF45E1B4E, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_PEASTEURO_01", 0x298F268A, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_PHARBRON_01", 0x27F5967B, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceBlack));
            this.modelInfo.Add(new CModelInfo("M_Y_PHARLEM_01", 0x1961E02, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceBlack));
            this.modelInfo.Add(new CModelInfo("M_Y_PJERSEY_01", 0x5BF734C6, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_PLATIN_01", 0x944D1A30, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_PLATIN_02", 0xC30777A4, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_PLATIN_03", 0xB0F0D377, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_PMANHAT_01", 0x243BD606, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthMidClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_PMANHAT_02", 0x7554785A, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthMidClass | EModelFlags.IsRaceBlack));
            this.modelInfo.Add(new CModelInfo("M_Y_PORIENT_01", 0xEB7CE59F, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_PQUEENS_01", 0x21673B90, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceBlack));
            this.modelInfo.Add(new CModelInfo("M_Y_PRICH_01", 0x509627D1, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthUpperClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_PVILLBO_01", 0xD55CAAC, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthMidClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_PVILLBO_02", 0xB5559AAD, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_PVILLBO_03", 0xA2E575D9, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_QUEENSBRIDGE", 0x48E8EE31, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceBlack));
            this.modelInfo.Add(new CModelInfo("M_Y_SHADY_02", 0xB73D062F, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_SKATEBIKE_01", 0x68A019EE, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_SOHO_01", 0x170C6DAE, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthMidClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_STREET_01", 0x3B99DE1, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_STREET_03", 0x1F3854DE, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_STREET_04", 0x3082F773, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceHispanic));
            this.modelInfo.Add(new CModelInfo("M_Y_STREETBLK_02", 0xA37B1794, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_STREETBLK_03", 0xD939030F, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_STREETPUNK_02", 0xD3E34ABA, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_STREETPUNK_04", 0x8D1CBD36, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_STREETPUNK_05", 0x51E946D0, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_TOUGH_05", 0xBC0DDE62, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_TOURIST_02", 0x303963D0, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthMidClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("F_Y_BIKESTRIPPER_01", 0x86BF8536, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_Y_BUSIASIAN", 0xE4CADE41, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_Y_EMIDTOWN_01", 0x1DE2861D, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthMidClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("F_Y_GANGELS_01", 0xF7055110, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_Y_GANGELS_02", 0x292B355B, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("F_Y_GANGELS_03", 0xE1F526F0, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_Y_GLOST_01", 0xB3AE9B8, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("F_Y_GLOST_02", 0x5453FBF5, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_Y_GLOST_03", 0x25911E70, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_Y_GLOST_04", 0x6677A03C, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("F_Y_GRYDERS_01", 0xB3E305FD, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_Y_UPTOWN_01", 0x4E5D55F, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceBlack));
            this.modelInfo.Add(new CModelInfo("F_Y_UPTOWN_CS", 0x8ED1E138, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_ASHLEYA", 0xD49C2B16, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_BILLY", 0xE5135137, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_BILLYPRISON", 0xCCC15E4E, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_BRIANJ", 0x14DA2838, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_CLAY", 0x6CCFE08A, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_DAVE_GROSSMAN", 0xB634B03C, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_DESEAN", 0xFB9A0BD0, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_EVAN", 0xD07B6195, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_JASON_M", 0xC77060C1, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_JIM_FITZ", 0x33E8C374, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_LOSTGIRL", 0xCF8E5838, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_MALC", 0xF1BCA919, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_MARTA", 0xA0367380, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_MATTHEWS", 0xF60A3CF3, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_MCCORNISH", 0x1609B707, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_NIKO", 0x6032264F, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_PGIRL_01", 0xA47978B5, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_PGIRL_02", 0x4BC8C755, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_ROMAN_E1", 0xD31529F3, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_STROOPER", 0x95D15467, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_TERRY", 0x67000B94, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("LOSTBUDDY_01", 0x721B6514, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("LOSTBUDDY_02", 0x808A01F1, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("LOSTBUDDY_03", 0x487511C8, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("LOSTBUDDY_04", 0x65BE4C5A, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("LOSTBUDDY_05", 0x2AC45667, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("LOSTBUDDY_06", 0x3985F3EA, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("LOSTBUDDY_07", 0x295D53B5, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("LOSTBUDDY_08", 0x1B1AB730, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("LOSTBUDDY_09", 0x540128FC, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("LOSTBUDDY_10", 0x2DBE5DAB, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("LOSTBUDDY_11", 0x64894B40, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("LOSTBUDDY_12", 0x725066CE, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("LOSTBUDDY_13", 0x192F348D, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_SMARTBLACK", 0x9607A6C2, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_SPRETZER", 0x81F47D63, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_UPEAST_01", 0x1A25B7E, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthUpperClass | EModelFlags.IsRaceBlack));
            this.modelInfo.Add(new CModelInfo("M_M_UPTOWN_01", 0x38D04A7D, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthMidClass | EModelFlags.IsRaceBlack));
            this.modelInfo.Add(new CModelInfo("M_O_HISPANIC_01", 0xC2700A81, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsOld | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_BIKEMECH", 0xFB504807, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_BUSIASIAN", 0xF2200C7B, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_BUSIMIDEAST", 0x836DCFB6, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_DOORMAN_01", 0xFAF80EF6, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GANGELS_02", 0xBAE8AD11, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GANGELS_03", 0x1C997071, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GANGELS_04", 0xD7ED23C, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_GANGELS_05", 0xF3B926, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GANGELS_06", 0x82613BFF, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GAYGANG_01", 0x636CDA80, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_GLOST_01", 0x55CEC30B, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_GLOST_02", 0x678B6684, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthPoor | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_GLOST_03", 0xE776E65D, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GLOST_04", 0x81041975, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GLOST_05", 0x92C5BCF8, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GLOST_06", 0x9C70D04E, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GRYDERS_01", 0xE1BA167, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceBlack));
            this.modelInfo.Add(new CModelInfo("M_Y_GRYDERS_02", 0x5EC9C2C2, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthPoor | EModelFlags.IsRaceBlack));
            this.modelInfo.Add(new CModelInfo("M_Y_GTRI_02", 0x9EF03294, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GTRIAD_HI_01", 0x4B0BC9FA, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceAsian));
            this.modelInfo.Add(new CModelInfo("M_Y_HIP_02", 0xE58A26AC, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_HIPMALE_01", 0xD1A697ED, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_HISPANIC_01", 0x5A99A8C0, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceHispanic));
            this.modelInfo.Add(new CModelInfo("M_Y_PRISONBLACK", 0xA97ED37B, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_PRISONDLC_01", 0xCEDC662A, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_PRISONGUARD", 0x8DC7AE18, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_Y_ASIANCLUB_01", 0x66C81C17, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthUpperClass | EModelFlags.IsRaceAsian));
            this.modelInfo.Add(new CModelInfo("F_Y_ASIANCLUB_02", 0x7511B8AA, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthUpperClass | EModelFlags.IsRaceAsian));
            this.modelInfo.Add(new CModelInfo("F_Y_CLOEPARKER", 0xA7114B68, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_Y_CLUBEURO_01", 0x37771AD5, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthMidClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("F_Y_DANCER_01", 0xCFC9B096, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_Y_DOMGIRL_01", 0x520CBA78, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceHispanic));
            this.modelInfo.Add(new CModelInfo("F_Y_EMIDTOWN_02", 0xC08ACB5F, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_Y_HOSTESS", 0x7A1ECAD7, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthMidClass | EModelFlags.IsRaceAsian));
            this.modelInfo.Add(new CModelInfo("F_Y_HOTCHICK_01", 0xEE4335C2, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_Y_HOTCHICK_02", 0x93D400CD, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_Y_HOTCHICK_03", 0x12BEFEB9, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthMidClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("F_Y_JONI", 0xCB6CD993, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_Y_PGIRL_01", 0xCDAE3E7C, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_Y_PGIRL_02", 0x9B9F5A57, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_Y_SMID_01", 0xA01941EC, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("F_Y_TRENDY_01", 0x2D874100, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthMidClass | EModelFlags.IsRaceWhite));
            //this.modelInfo.Add(new CModelInfo("IG_AHMAD", 0xE2F65127, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_ARMANDO", 0x51AD1CE3, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_ARMSDEALER", 0x47471B9B, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_ARNAUD", 0x2A96AA6B, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_BANKER", 0x1BBAF430, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_BLUEBROS", 0xA91DABD1, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_BRUCIE2", 0xE80E9160, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_BULGARIN2", 0xE860DFB, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_DAISY", 0x26F2283E, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_DEEJAY", 0xA94AF89C, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_DESSIE", 0xA9C24CEF, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_GRACIE2", 0x780C8ADA, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_HENRIQUE", 0x7193DD41, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_ISSAC2", 0xA7356B14, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_JACKSON", 0xC1379A94, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_JOHNNY2", 0x7D372B, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_LUIS2", 0x75CCCC60, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_MARGOT", 0x6B34A006, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_MORI_K", 0x63138CCC, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_MR_SANTOS", 0x26582854, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_NAPOLI", 0xCE2077E6, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_OYVEY", 0x7C89F307, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_ROCCO", 0xC9869CCA, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_ROYAL", 0xDBF72AD6, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_SPADE", 0x671E6D91, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_TAHIR", 0xE7BCA666, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_TIMUR", 0x8BCF3DEB, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_TONY", 0xEFA2695D, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_TRAMP2", 0xC5F4F8A5, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_TRIAD", 0x249488, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_TROY", 0x6317546B, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_VIC", 0xF6A7A434, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_VICGIRL", 0xE4C07993, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_VINCE", 0x5285B57B, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            //this.modelInfo.Add(new CModelInfo("IG_YUSEF", 0xE5497381, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_E2MAF_01", 0xD7FC02CB, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_M_E2MAF_02", 0x30F234C2, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_M_MAFUNION", 0x27369312, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_AMIRGUARD_01", 0x273BE7AE, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthMidClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_BARMAISON", 0x9AE100DF, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_BATHROOM", 0x5537808C, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthPoor | EModelFlags.IsRaceBlack));
            this.modelInfo.Add(new CModelInfo("M_Y_CELEBBLOG", 0x94CBBAF8, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_CLUBBLACK_01", 0xD37434B0, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_CLUBEURO_01", 0x10F4BD43, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthMidClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_CLUBEURO_02", 0xFF5219FE, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_CLUBEURO_03", 0x2562661E, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthMidClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_CLUBWHITE_01", 0x29A3192E, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthMidClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_DOMDRUG_01", 0xEC2D21A, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceHispanic));
            this.modelInfo.Add(new CModelInfo("M_Y_DOMGUY_01", 0x2BFEE7EE, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceHispanic));
            this.modelInfo.Add(new CModelInfo("M_Y_DOMGUY_02", 0x61BFD373, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceHispanic));
            this.modelInfo.Add(new CModelInfo("M_Y_DOORMAN_02", 0x68B66A71, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_E2RUSSIAN_01", 0xB12754CD, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_E2RUSSIAN_02", 0xDEDA3016, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_E2RUSSIAN_03", 0xD5241CAA, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_EXSPORTS", 0x1E10313A, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_FIGHTCLUB_01", 0xB3D17A7, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_FIGHTCLUB_02", 0xA7F4D114, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_FIGHTCLUB_03", 0xBA4775B9, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_FIGHTCLUB_04", 0xCEA49E73, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_FIGHTCLUB_05", 0xDE623DEE, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_FIGHTCLUB_06", 0x531D2766, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceBlack)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_FIGHTCLUB_07", 0x64D34AD2, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_FIGHTCLUB_08", 0x7586EC39, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceAsian)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GAYBLACK_01", 0x218F4947, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthMidClass | EModelFlags.IsRaceBlack));
            this.modelInfo.Add(new CModelInfo("M_Y_GAYDANCER", 0xAA47E132, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GAYGENERAL_01", 0xE0AAAB26, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GAYWHITE_01", 0xD6511833, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_GUIDO_01", 0x3A895123, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthMidClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_GUIDO_02", 0x68D7ADC7, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthMidClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_MIDEAST_01", 0x2D654515, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_MOBPARTY", 0x432DABA9, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsAdult | EModelFlags.IsWealthUpperClass | EModelFlags.IsRaceWhite));
            this.modelInfo.Add(new CModelInfo("M_Y_PAPARAZZI_01", 0xABC3DCD5, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED
            this.modelInfo.Add(new CModelInfo("M_Y_UPTOWN_01", 0xCD8C3F20, EModelFlags.IsPed | EModelFlags.IsCivilian | EModelFlags.IsYoung | EModelFlags.IsWealthLowerClass | EModelFlags.IsRaceWhite)); // DATA NOT CONFIRMED

            // Properly read ped animation data:

            string pedsFile = GTA.Game.InstallFolder + "\\common\\data\\peds.ide";
            List<string> pedLines = new List<string>();

            foreach (string line in File.ReadLines(pedsFile))
            {
                if (line.Length > 50) pedLines.Add(line);
            }

            foreach (string pedLine in pedLines)
            {
                //Regex.Replace(copLine, @"\s+", "");
                string trimmed = Regex.Replace(pedLine, @"\t|\n|\r", "");
                trimmed = trimmed.Trim();
                string[] splitPedLine = trimmed.Split(',');
                if (splitPedLine.Length > 3)
                {
                    //0th Element is the model
                    //3rd Element is the anim group
                    foreach (CModelInfo modelInfo in this.modelInfo)
                    {
                        if (modelInfo.Name.ToLower() == splitPedLine[0].ToLower())
                        {
                            //Log.Debug("Mapping " + modelInfo.Name + " to " + splitPedLine[3], this);
                            modelInfo.SetAnimGroup(splitPedLine[3]);
                        }
                    }
                }
            }

            

            Log.Debug("ModelManager: Loaded " + this.modelInfo.Count + " models", "ModelManager");
        }

        /// <summary>
        /// Adds <paramref name="modelInfo"/> to the model manager.
        /// </summary>
        /// <param name="modelInfo">The model info.</param>
        public void AddModelInfoData(CModelInfo modelInfo)
        {
            if (this.modelInfo.Any(model => model.Name == modelInfo.Name))
            {
                Log.Debug("AddModelInfoData: " + modelInfo.Name + " already in list", this);
                return;
            }

            this.modelInfo.Add(modelInfo);
        }

        /// <summary>
        /// Gets the model info by hash.
        /// </summary>
        /// <param name="hash">The hash.</param>
        /// <returns>The model info.</returns>
        public CModelInfo GetModelInfoByHash(uint hash)
        {
            //Log.Debug("Looking up hash " + hash.ToString("X"), "ModelManager");
            foreach (CModelInfo cModelInfo in modelInfo)
            {
                if (cModelInfo.Hash == hash)
                {
                    //Log.Debug("Data found", "ModelManager");
                    return cModelInfo;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the model info by name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The model info.</returns>
        public CModelInfo GetModelInfoByName(string name)
        {
            //Log.Debug("Looking up name " + name, "ModelManager");
            foreach (CModelInfo cModelInfo in modelInfo)
            {
                if (cModelInfo.Name == name)
                {
                    return cModelInfo;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets all models that have the flags set.
        /// </summary>
        /// <param name="modelFlags">The flags.</param>
        /// <returns>Model array.</returns>
        public CModelInfo[] GetModelInfosByFlags(EModelFlags modelFlags)
        {
            var tempInfos = new List<CModelInfo>();
            foreach (CModelInfo cModelInfo in this.modelInfo)
            {
                if (cModelInfo.ModelFlags.HasFlag(modelFlags))
                {
                    tempInfos.Add(cModelInfo);
                }
            }

            return tempInfos.ToArray();
        }

        /// <summary>
        /// Gets all models without the flags set.
        /// </summary>
        /// <param name="modelFlags">The flags.</param>
        /// <returns>Model array.</returns>
        public CModelInfo[] GetModelInfosByFlagsExclude(EModelFlags modelFlags)
        {
            var tempInfos = new List<CModelInfo>();
            foreach (CModelInfo cModelInfo in this.modelInfo)
            {
                if ((cModelInfo.ModelFlags & modelFlags) == 0)
                {
                    tempInfos.Add(cModelInfo);
                }
            }

            return tempInfos.ToArray();
        }
    }
}