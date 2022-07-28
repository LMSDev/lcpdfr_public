namespace LCPD_First_Response.LCPDFR
{
    using System.Text;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Events;
    using LCPD_First_Response.Engine.Scripting.Native;
    using LCPD_First_Response.Engine.Scripting.Scenarios;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;
    using System.IO;
    using System.Collections;
    using System.Collections.Generic;

    using LCPD_First_Response.LCPDFR.GUI;

    /// <summary>
    /// Responsible for voice messages such as Visual lost, Criminal fleeing, Officer down etc.
    /// </summary>
    internal static class PoliceScanner
    {
        /// <summary>
        /// Sets a value indicating whether the cop blips are active.
        /// </summary>
        public static bool CopBlipsActive
        {
            set
            {
                AdvancedHookManaged.AVehicle.SetCopBlipsActive(value);
            }
        }

        /// <summary>
        /// Sets a value indicating whether the in-game police scanner is enabled, which is the voice heard when the player commits a crime. Needed to play some police sounds.
        /// </summary>
        public static bool PoliceScannerEnabled
        {
            set
            {
                if (value)
                {
                    Natives.EnablePoliceScanner();
                }
                else
                {
                    Natives.DisablePoliceScanner();
                }
            }
        }

        /// <summary>
        /// Initializes static members of the <see cref="PoliceScanner"/> class.
        /// </summary>
        public static void Initialize()
        {
            // EventOfficerDown.EventRaised += new EventOfficerDown.EventRaisedEventHandler(EventOfficerDown_EventRaised);
            // EventPedDead.EventRaised += new EventPedDead.EventRaisedEventHandler(EventPedDead_EventRaised);
            EventVisualLost.EventRaised += new EventVisualLost.EventRaisedEventHandler(EventVisualLost_EventRaised);
            EventCriminalEnteredVehicle.EventRaised += new EventCriminalEnteredVehicle.EventRaisedEventHandler(EventCriminalEnteredVehicle_EventRaised);
            EventCriminalLeftVehicle.EventRaised += new EventCriminalLeftVehicle.EventRaisedEventHandler(EventCriminalLeftVehicle_EventRaised);
            EventCriminalEscaped.EventRaised += new EventCriminalEscaped.EventRaisedEventHandler(EventCriminalEscaped_EventRaised);
            EventCriminalSpotted.EventRaised += EventCriminalSpotted_EventRaised;
            EventCriminalSpeeding.EventRaised += EventCriminalSpeeding_EventRaised;
            EventHelicopterDown.EventRaised += EventHelicopterDown_EventRaised;
            EventOfficerAttacked.EventRaised += EventOfficerAttacked_EventRaised;
        }


        private static List<string> colorNames;
        private static string workingString = "";

        #region carColSpeak

        public static string CarCol(int index)
        {
            return CarColSpeak(index, false);
        }
        public static string CarColSpeak(int index)
        {
            return CarColSpeak(index, true);
        }
        private static string CarColSpeak(int index, bool speak)
        {
            string carCol = null;

            if (colorNames == null)
            {
                colorNames = new List<string>();

                string file = @"common\data\carcols.dat";

                ArrayList al = new ArrayList();

                using (StreamReader sr = new StreamReader(file))
                {
                    string line;

                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Length >= 3 && line.Substring(0, 3) == "end") break;
                        if (line.Length < 10 || line == "col" || line.Substring(0, 1) == "#") continue;

                        al.Add(line);

                    }
                    sr.Close();
                }

                foreach (string s in al)
                {
                    string[] splitter = s.Split('#');
                    if (splitter.Length == 0 || splitter[0] == null) continue;
                    string ss = splitter[0].Trim();
                    splitter = ss.Split(',');
                    if (splitter.Length < 4) continue;
                    if (splitter[3] != "-") ss = splitter[3] + " " + splitter[4];
                    else ss = splitter[4];
                    colorNames.Add(ss);
                }

            }

            if (index < 0 || index >= colorNames.Count)
            {
                Log.Warning("CarColSpeak: Invalid color index " + index, "PoliceScanner");
            }

            carCol = colorNames[index];

            if (!speak)
            {
                return carCol;
            }
            else
            {
                if (carCol != null)
                {
                    workingString = "";

                    if (carCol.Split(' ').Length > 1)
                    {
                        if (carCol.Split(' ')[0] == "bright") addSound("none","BRIGHT"); //AudioHelper.PlayActionSound("BRIGHT", true);
                        else if (carCol.Split(' ')[0] == "light") addSound("none", "LIGHT");//AudioHelper.PlayActionSound("LIGHT", true);
                        else if (carCol.Split(' ')[0] == "dark") addSound("none", "DARK"); //AudioHelper.PlayActionSound("DARK", true);
                        // addSound("POLICE_SCANNER/COLOUR", carCol.Split(' ')[1].ToUpper());
                        // AudioHelper.PlayActionSound(carCol.Split(' ')[1].ToUpper(), true);
                        // return carCol.Split(' ')[1].ToUpper();
                        addSound("none", carCol.Split(' ')[1].ToUpper());
                    }
                    else
                    {
                        //addSound("POLICE_SCANNER/COLOUR", carCol.ToUpper());
                        // AudioHelper.PlayActionSound(carCol.ToUpper(), true);
                        addSound("none", carCol.ToUpper());
                    }
                }
                return workingString;
            }
        }

        #endregion

        # region carNameSpeak

        private static void addSound(string unused, string sound)
        {
            //AudioHelper.PlayActionSound(sound, true);
            workingString += " " + sound;
        }

        public static string CarName(CVehicle veh)
        {
            return CarNameSpeak(veh, 0, false, false);
        }
        public static string CarName(int hash)
        {
            return CarNameSpeak(null, hash, false, false);
        }
        public static string CarType(CVehicle veh)
        {
            return CarNameSpeak(veh, 0, false, true);
        }
        public static string CarTypeSpeak(CVehicle veh)
        {
            return CarNameSpeak(veh, 0, true, true);
        }
        public static string CarNameSpeak(CVehicle veh)
        {
            return CarNameSpeak(veh, 0, true, false);
        }

        private static string CarNameSpeak(CVehicle veh, int hash, bool speak, bool catonly)
        {
            workingString = "";

            string result;
            string carM = null;
            string carMt = null;
            string carMpre = null;
            string carMpost = null;
            string carCat = null;
            string carSpec1 = null;
            int hash2 = 0;
            if (veh != null && veh.Exists()) hash2 = (int)veh.Model.ModelInfo.Hash;
            else hash2 = hash;

            if (hash2 == 0) return "Error";

            switch (hash2)
            {
                case 1264341792:
                    carM = "Dundreary Admiral";
                    carCat = "SEDAN";
                    break;
                case 1560980623:
                    carM = "AirTug";
                    break;
                case 1171614426:
                    carM = "Ambulance";
                    break;
                case -1041692462:
                    carM = "Bravado Banshee";
                    carCat = "SPORTS_CAR";
                    break;
                case 2053223216:
                    carM = "Vapid Benson";
                    carCat = "TRUCK";
                    break;
                case 850991848:
                    carM = "HVY Biff";
                    carCat = "TRUCK";
                    break;
                case -344943009:
                    carM = "Dinka Blista";
                    carMt = "Dinka Blista Compact";
                    carCat = "HATCHBACK";
                    break;
                case 1075851868:
                    carM = "Vapid Bobcat";
                    carCat = "PICK_UP";
                    break;
                case -1987130134:
                    carM = "Brute Boxville";
                    carCat = "VAN";
                    break;
                case -682211828:
                    carM = "Albany Buccaneer";
                    carCat = "COUPE";
                    break;
                case -1346687836:
                case -907477130:
                case 254976389: // check if it's correct
                    carM = "Declasse Burrito";
                    carCat = "VAN";
                    break;
                case -713569950:
                    carM = "Bus";
                    break;
                case 1884962369:
                    carM = "Taxi";
                    break;
                case 2006918058:
                    carM = "Albany Cavalcade";
                    carCat = "SUV";
                    break;
                case -67282078:
                    carM = "Dinka Chavos";
                    carCat = "SEDAN";
                    break;
                case -2030171296:
                    carM = "Enus Cognoscenti";
                    carCat = "SEDAN";
                    break;
                case 1063483177:
                    carM = "Pfister Comet";
                    carCat = "SPORTS_CAR";
                    break;
                case 108773431:
                    carM = "Invetero Coquette";
                    carCat = "SPORTS_CAR";
                    break;
                case 162883121:
                    carM = "Imponte DF8";
                    carMt = "Imponte DF8-90";
                    carCat = "FOUR_DOOR";
                    break;
                case -1130810103:
                    carM = "Karin Dilletante";
                    carCat = "HATCHBACK";
                    break;
                case 723973206:
                    carM = "Imponte Dukes";
                    carCat = "COUPE";
                    break;
                case -1971955454:
                    carM = "Vapid Contender";
                    carCat = "PICK_UP";
                    break;
                case -685276541:
                case -1883002148:
                    carM = "Albany Emperor";
                    carCat = "SEDAN";
                    break;
                case -276900515:
                case -1932515764:
                    carM = "Albany Esperanto";
                    carCat = "SEDAN";
                    break;
                case -2119578145:
                    carM = "Willard Faction";
                    carCat = "COUPE";
                    break;
                case 1127131465:
                    carM = "FIB Buffalo";
                    carCat = "SEDAN";
                    break;
                case -1097828879:
                    carM = "Benefactor Feltzer";
                    if (veh.Exists()) carCat = veh.Extras(3).Enabled ? "SPORTS_CAR" : "CONVERTIBLE";
                    else carCat = "SPORTS_CAR";
                    break;
                case 974744810:
                case 1026055242:
                    carM = "Bravado Feroci";
                    carCat = "SEDAN";
                    break;
                case 1938952078:
                    carM = "Firetruck";
                    break;
                case 1353720154:
                    carM = "MTL Flatbed";
                    carCat = "TRUCK";
                    break;
                case 627033353:
                    carM = "Vapid Fortune";
                    carCat = "COUPE";
                    break;
                case 1491375716:
                    carM = "Forklift";
                    break;
                case 2016857647:
                    carM = "Karin Futo";
                    carCat = "COUPE";
                    break;
                case 675415136:
                    carM = "Albany FXT";
                    carCat = "SUV";
                    break;
                case 884422927:
                    carM = "Emperor Habanero";
                    carCat = "VAN";
                    break;
                case -341892653:
                    carM = "Dinka Hakumai";
                    carCat = "SEDAN";
                    break;
                case 486987393:
                    carM = "Vapid Huntley";
                    carCat = "SUV";
                    break;
                case 418536135:
                    carM = "Pegasi Infernus";
                    carCat = "SPORTS_CAR";
                    break;
                case -1289722222:
                    carM = "Vulcar Ingot";
                    carCat = "STATION_WAGON";
                    break;
                case 886934177:
                    carM = "Karin Intruder";
                    carCat = "SEDAN";
                    break;
                case 1269098716:
                    carM = "Dundreary Landstalker";
                    carCat = "SUV";
                    break;
                case -37030056:
                    carM = "Emperor Lokus";
                    carCat = "SEDAN";
                    break;
                case -2124201592:
                    carM = "Albany Manana";
                    carCat = "TWO_DOOR";
                    break;
                case 1304597482:
                    carM = "Willard Marbelle";
                    carCat = "SEDAN";
                    break;
                case -1260881538:
                    carM = "Declasse Merit";
                    carCat = "SEDAN";
                    break;
                case -310465116:
                    carM = "Vapid Minivan";
                    carCat = "VAN";
                    break;
                case 525509695:
                    carM = "Declasse Moonbeam";
                    carCat = "VAN";
                    break;
                case 583100975:
                    carM = "ice_cream_truck";
                    carMt = "Mr. Tasty";
                    break;
                case 904750859:
                    carM = "Maibatsu Mule";
                    carCat = "TRUCK";
                    break;
                case 148777611:
                    carM = "NOOSE";
                    carMt = "NOOSE Cruiser";
                    break;
                case 1911513875:
                    carMpre = "MODEL/MOD_SWAT";
                    carM = "ENFORCER";
                    carMt = "SWAT Enforcer";
                    break;
                case 1348744438:
                    carM = "Ubermacht Oracle";
                    carCat = "SEDAN";
                    break;
                case 569305213:
                    carM = "MTL Packer";
                    carCat = "TRUCK";
                    break;
                case -808457413:
                    carM = "Mammoth Patriot";
                    carCat = "SUV";
                    break;
                case -2077743597:
                    carM = "Dinka Perennial";
                    carCat = "VAN";
                    break;
                case -1590284256:
                    carM = "Dinka Perennial";
                    carMt = "FlyUS Perennial";
                    carCat = "VAN";
                    break;
                case 1830407356:
                    carM = "Vapid Peyote";
                    if (veh.Exists()) carCat = veh.Extras(2).Enabled ? "TWO_DOOR" : "CONVERTIBLE";
                    else carCat = "TWO_DOOR";
                    break;
                case -2137348917:
                    carM = "Joebuilt Phantom";
                    carCat = "TRUCK";
                    break;
                case 131140572:
                    carM = "Annis Pinnacle";
                    carCat = "SEDAN";
                    break;
                case 1376298265:
                    carM = "Schyster pmp_600";
                    carMt = "Schyster PMP 600";
                    carCat = "SEDAN";
                    break;
                case 2046537925:
                    carM = "Police_car";
                    carMt = "Police Cruiser";
                    break;
                case -1627000575:
                    carM = "Police_car";
                    carMt = "Police Patrol";
                    break;
                case -350085182:
                    carMpre = "MODEL/MOD_NOOSE";
                    carM = "Patriot";
                    carMt = "NOOSE Patriot";
                    break;
                case -119658072:
                    carM = "Brute Pony";
                    carCat = "VAN";
                    break;
                case -1883869285:
                    carM = "Declasse Premier";
                    carCat = "SEDAN";
                    break;
                case -1962071130:
                    carMt = "Albany Presidente";
                    carM = "Albany Pres";
                    carCat = "SEDAN";
                    break;
                case -1150599089:
                    carM = "Albany Primo";
                    carCat = "SEDAN";
                    break;
                case -1900572838:
                    carM = "Police_Stockade";
                    carMt = "Police Stockade";
                    break;
                case 1390084576:
                    carM = "Declasse Rancher";
                    if (veh.Exists()) carCat = veh.Extras(2).Enabled ? "SUV" : "PICK_UP";
                    else carCat = "SUV";
                    break;
                case 83136452:
                    carM = "Ubermacht Rebla";
                    carCat = "SUV";
                    break;
                case -845979911:
                    carM = "Ripley";
                    carCat = "TRUCK";
                    break;
                case 627094268:
                    carMpre = "VEHICLE_CATEGORY/HEARSE";
                    carMt = "Hearse";
                    carCat = "HEARSE";
                    break;
                case -227741703:
                    carM = "Imponte Ruiner";
                    carCat = "COUPE";
                    break;
                case -449022887:
                case 1264386590:
                    carM = "Declasse Sabre";
                    carCat = "COUPE";
                    break;
                case -1685021548:
                    carM = "Declasse Sabre_GT";
                    carMt = "Declasse Sabre GT";
                    carCat = "COUPE";
                    break;
                case -322343873:
                    carM = "Benefactor Schafter";
                    carCat = "SEDAN";
                    break;
                case 1349725314:
                    carM = "Ubermacht Sentinel";
                    carCat = "COUPE";
                    break;
                case 1344573448:
                    carM = "Willard Solair";
                    carCat = "STATION_WAGON";
                    break;
                case -810318068:
                    carM = "Vapid Speedo";
                    carCat = "VAN";
                    break;
                case 1923400478:
                    carM = "Classique Stallion";
                    if (veh.Exists()) carCat = veh.Extras(3).Enabled ? "COUPE" : "CONVERTIBLE";
                    else carCat = "COUPE";
                    break;
                case 1677715180:
                    carM = "Vapid Steed";
                    carCat = "VAN";
                    break;
                case 1747439474:
                    carM = "Securicar";
                    break;
                case 1723137093:
                    carM = "Zireonium Stratum";
                    carCat = "STATION_WAGON";
                    break;
                case -1961627517:
                    carM = "Stretch";
                    carMpost = "VEHICLE_CATEGORY/LIMO";
                    carM = "Stretch Limo";
                    carCat = "LIMO";
                    break;
                case 970598228:
                    carM = "Karin Sultan";
                    carCat = "SEDAN";
                    break;
                case -295689028:
                    carM = "Karin Sultan_RS";
                    carMt = "Karin Sultan RS";
                    carCat = "SPORTS_CAR";
                    break;
                case 1821991593:
                    carM = "Dewbauchee SuperGT";
                    carCat = "SPORTS_CAR";
                    break;
                case -956048545:
                case 1208856469:
                    carM = "Taxi";
                    break;
                case 1917016601:
                    carM = "Trash";
                    carMt = "Trashmaster";
                    carCat = "TRUCK";
                    break;
                case -1896659641:
                    carM = "Grotti Turismo";
                    carCat = "SPORTS_CAR";
                    break;
                case 1534326199:
                    carM = "Vapid Uranus";
                    carCat = "TWO_DOOR";
                    break;
                case -825837129:
                    carM = "Declasse Vigero";
                    carCat = "COUPE";
                    break;
                case -1758379524:
                    carM = "Declasse Vigero";
                    carCat = "JALOPY";
                    break;
                case -583281407:
                    carM = "Maibatsu Vincent";
                    carCat = "SEDAN";
                    break;
                case -498054846:
                    carM = "Dundreary Virgo";
                    carCat = "TWO_DOOR";
                    break;
                case 2006667053:
                    carM = "Voodoo";
                    carCat = "TWO_DOOR";
                    break;
                case 1777363799:
                    carM = "Albany Washington";
                    carCat = "SEDAN";
                    break;
                case 1937616578:
                    carM = "Willard";
                    carCat = "SEDAN";
                    break;
                case -1099960214:
                    carM = "Vapid Yankee";
                    carCat = "TRUCK";
                    break;
                // BIKES --------------------------------------
                case -1842748181:
                    carMpre = "VEHICLE_CATEGORY/SCOOTER";
                    carMt = "Scooter";
                    carCat = "SCOOTER";
                    break;
                case 55628203:
                    carMpre = "VEHICLE_CATEGORY/MOPED";
                    carMt = "Moped";
                    carCat = "MOPED";
                    break;
                case 584879743:
                    carM = "Hellfury";
                    carCat = "MOTORCYCLE";
                    break;
                case 1203311498:
                    carM = "Shitzu NRG900";
                    carCat = "MOTORCYCLE";
                    break;
                case -909201658:
                    carM = "Shitzu PCJ600";
                    carCat = "MOTORCYCLE";
                    break;
                case 788045382:
                    carM = "Sanchez";
                    carCat = "MOTORCYCLE";
                    break;
                case -570033273:
                    carM = "Zombie";
                    carCat = "MOTORCYCLE";
                    break;
                // Helikoptery -----------------------
                case 837858166:
                    carM = "POLICE_HELICOPTER";
                    carMt = "Police Annihilator";
                    break;
                case -1660661558:
                case 2027357303:
                    carM = "MAVERICK_HELICOPTER";
                    carMt = "Maverick";
                    break;
                case 353883353:
                    carM = "POLICE_MAVERICK_HELICOPTER";
                    carMt = "Police Maverick";
                    break;
                // LODZIE -----------------------------
                case 1033245328:
                    carM = "Dinghy";
                    carCat = "SPEEDBOAT";
                    break;
                case 861409633:
                    carM = "Jetmax";
                    carCat = "SPEEDBOAT";
                    break;
                case -1043459709:
                    carM = "Marquee";
                    carMt = "Marquis";
                    carCat = "YACHT";
                    break;
                case -488123221:
                    carM = "Predator";
                    carCat = "SPEEDBOAT";
                    break;
                case 1759673526:
                    carM = "Reefer";
                    carCat = "YACHT";
                    break;
                case 400514754:
                    carM = "Squalo";
                    carCat = "SPEEDBOAT";
                    break;
                case 290013743:
                    carM = "Tropic";
                    carCat = "SPEEDBOAT";
                    break;
                case 1064455782:
                    carM = "Tug_Boat";
                    carMt = "Tug Boat";
                    carCat = "TRAWLER";
                    break;

                // Auta EFLC
                case 562680400:
                    carM = "Police_car";
                    carMt = "APC";
                    break;
                case -304802106:
                    carM = "Bravado Buffalo";
                    carCat = "SEDAN";
                    break;
                case 259882128: // popraw
                    carMpre = "MANUFACTURER/MAN_VAPID";
                    carMpost = "VEHICLE_CATEGORY/SPORTS_CAR";
                    carM = "Vapid Bullet GT";
                    carCat = "SPORTS_CAR";
                    break;
                case 1147287684:
                    carSpec1 = "EP2_SFX/E2_SCANNER_MODELS|MOD_CADDY";
                    carMt = "Caddy";
                    break;
                case -591610296:
                    carMpre = "VEHICLE_CATEGORY/COUPE";
                    carMpost = "VEHICLE_CATEGORY/SPORTS_CAR";
                    carM = "Ocelot F620";
                    carCat = "SPORTS_CAR";
                    break;
                case -114627507:
                    carMpost = "VEHICLE_CATEGORY/LIMO";
                    carM = "Schafter";
                    carMt = "Schafter Limo";
                    carCat = "LIMO";
                    break;
                case 1337041428:
                case 1051281622:
                    carMpre = "MANUFACTURER/MAN_BENEFACTOR";
                    carMpost = "VEHICLE_CATEGORY/SUV";
                    carCat = "SUV";
                    break;
                case 303951489: // to correct
                case 280944375: // to correct
                    carM = "Benefactor Schafter";
                    carCat = "SEDAN";
                    break;
                case 729783779:
                    carMpre = "MANUFACTURER/MAN_VAPID";
                    carSpec1 = "EP2_SFX/E2_SCANNER_MODELS|SLAMVAN";
                    carMt = "Vapid Slamvan";
                    carCat = "VAN";
                    break;
                case 1123216662:
                    carMpre = "MANUFACTURER/MAN_ENUS";
                    carMpost = "VEHICLE_CATEGORY/FOUR_DOOR";
                    carMt = "Enus Super Diamond";
                    carCat = "SEDAN";
                    break;
                case 1638119866:
                    carMpre = "MANUFACTURER/MAN_ENUS";
                    carMpost = "VEHICLE_CATEGORY/CONVERTIBLE";
                    carMt = "Enus Super Drop Diamond";
                    carCat = "CONVERTIBLE";
                    break;
                case -1323100960:
                    carMpre = "MANUFACTURER/MAN_VAPID";
                    carMpost = "VEHICLE_CATEGORY/TRUCK";
                    carM = "Vapid Towtruck";
                    carCat = "TRUCK";
                    break;
                case 1912215274:
                    carMpre = "MODEL/MOD_BUFFALO";
                    carM = "Police_car";
                    carMt = "Police Buffalo";
                    break;
                case -1973172295:
                    carM = "Police_car";
                    carMt = "Police Stinger";
                    break;
                // BIKES EFLC --------------------------------
                case 1672195559:
                    carMpre = "MANUFACTURER/MAN_DINKA";
                    carMpost = "VEHICLE_CATEGORY/MOTORCYCLE";
                    carMt = "Dinka Akuma";
                    carCat = "MOTORCYCLE";
                    break;
                case -891462355:
                    carMpre = "MANUFACTURER/MAN_PEGASI";
                    carMpost = "VEHICLE_CATEGORY/MOTORCYCLE";
                    carMt = "Pegasi Bati";
                    carCat = "MOTORCYCLE";
                    break;
                case -1830458836:
                    carM = "Freeway";
                    carCat = "MOTORCYCLE";
                    break;
                case -1670998136:
                    carMpre = "MANUFACTURER/MAN_DINKA";
                    carMpost = "VEHICLE_CATEGORY/MOTORCYCLE";
                    carMt = "Dinka Double T";
                    carCat = "MOTORCYCLE";
                    break;
                case 1265391242:
                    carMpre = "MANUFACTURER/MAN_SHITZU";
                    carMpost = "VEHICLE_CATEGORY/MOTORCYCLE";
                    carMt = "Shitzu Hakuchou";
                    carCat = "MOTORCYCLE";
                    break;
                case 301427732:
                    carMpre = "VEHICLE_CATEGORY/MOTORCYCLE";
                    carMt = "Hexer";
                    carCat = "MOTORCYCLE";
                    break;
                case -34623805:
                    carMpre = "VEHICLE_CATEGORY/MOTORCYCLE";
                    carMt = "Police Bike";
                    carCat = "MOTORCYCLE";
                    break;
                case -140902153:
                    carMpre = "MANUFACTURER/MAN_SHITZU";
                    carMpost = "VEHICLE_CATEGORY/MOTORCYCLE";
                    carMt = "Shitzu Vader";
                    carCat = "MOTORCYCLE";
                    break;
                // HELI EFLC ------------------------------
                case 788747387:
                    carMpre = "VEHICLE_CATEGORY/HELICOPTER";
                    carMt = "Buzzard";
                    carCat = "HELICOPTER";
                    break;
                case 1044954915:
                    carMpre = "MANUFACTURER/MAN_HVY";
                    carMpost = "VEHICLE_CATEGORY/HELICOPTER";
                    carMt = "HVY Skylift";
                    carCat = "HELICOPTER";
                    break;
                case -339587598:
                    carMpre = "VEHICLE_CATEGORY/HELICOPTER";
                    carMt = "Swift";
                    carCat = "HELICOPTER";
                    break;
                // BOATS EFLC -----------------------------
                case -1205801634:
                    carMpre = "VEHICLE_CATEGORY/SPEEDBOAT";
                    carMt = "Blade";
                    carCat = "SPEEDBOAT";
                    break;
                case -1731432653:
                    carMpre = "VEHICLE_CATEGORY/SPEEDBOAT";
                    carMt = "Floater";
                    carCat = "SPEEDBOAT";
                    break;
                case 944930284:
                    carMpre = "VEHICLE_CATEGORY/SPEEDBOAT";
                    carMt = "Smuggler";
                    carCat = "SPEEDBOAT";
                    break;
            }


            if (!speak)
            {
                if (carMt == null) return carM;
                else return carMt;
            }

            if (catonly)
            {
                if (carCat != null) addSound("POLICE_SCANNER/VEHICLE_CATEGORY", carCat);
                else
                {
                    if (carM != null)
                    {
                        addSound("POLICE_SCANNER/MODEL", "MOD_" + carM.ToUpper());
                    }
                    else
                    {
                        addSound(carSpec1.Split('|')[0].ToUpper(), carSpec1.Split('|')[1].ToUpper());
                    }
                }
            }
            else
            {

                if (carMpre != null)
                {
                    addSound("POLICE_SCANNER/" + carMpre.Split('/')[0].ToUpper(), carMpre.Split('/')[1].ToUpper());
                }
                if (carM != null)
                {
                    if (carM.Split(' ').Length > 1)
                    {
                        addSound("POLICE_SCANNER/MANUFACTURER", "MAN_" + carM.Split(' ')[0].ToUpper());
                        addSound("POLICE_SCANNER/MODEL", "MOD_" + carM.Split(' ')[1].ToUpper());
                    }
                    else
                    {
                        addSound("POLICE_SCANNER/MODEL", "MOD_" + carM.ToUpper());
                    }
                }

                if (carSpec1 != null)
                {
                    addSound(carSpec1.Split('|')[0].ToUpper(), carSpec1.Split('|')[1].ToUpper());
                }

                if (carMpost != null)
                {
                    addSound("POLICE_SCANNER/" + carMpost.Split('/')[0].ToUpper(), carMpost.Split('/')[1].ToUpper());
                }
            }

            //result = "AAA";
            return workingString;
        }
        # endregion


        /// <summary>
        /// Reports <paramref name="ped"/> as being attacked by a suspect.
        /// </summary>
        /// <param name="ped">The cop.</param>
        public static void ReportOfficerAttacked(CPed ped)
        {
            DelayedCaller.Call(delegate {
                if (ped!= null && ped.Exists())
                {
                    AudioHelper.PlayDispatchAcknowledgeOfficerAttacked(ped.Position);
                }
            }, 5000);
        }

        /// <summary>
        /// Reports <paramref name="ped"/> as officer down.
        /// </summary>
        /// <param name="ped">The cop.</param>
        /// <param name="dontInvestigate">Whether or not a close unit should investigate the position.</param>
        public static void ReportOfficerDown(CPed ped, bool dontInvestigate)
        {
            // Only works for cops on foot
            if (!dontInvestigate && !ped.IsInVehicle)
            {
                Log.Debug("ReportOfficerDown: Looking for close units to investigate", "PoliceScanner");

                // Make close cop investigate the scene
                CPed[] closeCops = Engine.Main.CopManager.RequestUnitInVehicle(ped.Position, 80);
                if (closeCops == null)
                {
                    Log.Debug("ReportOfficerDown: No units in vehicle found", "PoliceScanner");

                    // No vehicle unit found, look for on foot units
                    CPed closeCop = Engine.Main.CopManager.RequestUnit(ped.Position, 40);
                    if (closeCop != null)
                    {
                        closeCops = new CPed[] { closeCop };
                    }
                    else
                    {
                        Log.Debug("ReportOfficerDown: No on foot unit found", "PoliceScanner");
                    }
                }

                DelayedCaller.Call(ReportOfficerDownDelayedReportDeadOfficer, 3000, ped);

                // Create scenario
                if (closeCops != null)
                {
                    DelayedCaller.Call(ReportOfficerDownDelayedCreateScenario, 7000, closeCops, ped);
                }
            }
        }

        /// <summary>
        /// Called three seconds after an officer was killed and reports the death.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        private static void ReportOfficerDownDelayedReportDeadOfficer(params object[] parameter)
        {
            CPed ped = (CPed)parameter[0];

            if (ped.Exists())
            {
                Main.TextWall.AddText("[" + CultureHelper.GetText("POLICE_SCANNER_CONTROL") + "]" + string.Format(CultureHelper.GetText("POLICE_SCANNER_OFFICER_DOWN"), AreaHelper.GetAreaNameMeaningful(ped.Position)));
            }
        }

        /// <summary>
        /// Called seven seconds after an officer was killed and nearby officers were found. Starts a scenario to investigate.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        private static void ReportOfficerDownDelayedCreateScenario(params object[] parameter)
        {
            CPed[] closeCops = (CPed[])parameter[0];
            CPed ped = (CPed)parameter[1];

            if (!ped.Exists())
            {
                return;
            }

            // If the array of cops has no cops, break.
            if (closeCops.Length == 0)
            {
                System.Diagnostics.Debugger.Break();
            }

            // Ensure the cops are still valid
            foreach (CPed closeCop in closeCops)
            {
                if (!closeCop.Exists())
                {
                    Log.Debug("ReportOfficerDown: Scenario cops disposed before creating scenario.", "PoliceScanner");
                    return;
                }
            }

            Log.Debug("ReportOfficerDown: Creating scenario", "PoliceScanner");
            var scenario = new ScenarioCopsInvestigateCrimeScene(closeCops, ped.Position, true);
            var taskScenario = new TaskScenario(scenario);

            string name = closeCops[0].PedData.Persona.Forename + " " + closeCops[0].PedData.Persona.Surname;
            Main.TextWall.AddText("[" + CultureHelper.GetText("POLICE_SCANNER_OFFICER") + " " + name + "] " + CultureHelper.GetText("POLICE_SCANNER_ROGER_THAT_INVESTIGATE"));
        }


        /// <summary>
        /// Called when visual lost event is raised.
        /// </summary>
        /// <param name="event">The event.</param>
        private static void EventVisualLost_EventRaised(EventVisualLost @event)
        {
            StringBuilder stringBuilder = new StringBuilder();

            Wanted w = @event.Ped.Wanted;
            if (w.LastKnownInVehicle)
            {
                if (w.LastKnownVehicle != null && w.LastKnownVehicle.Exists())
                { 
                    stringBuilder.Append("Last known in vehicle of model ").Append(w.LastKnownVehicle.Model.ModelInfo.Name)
                        .Append(" and color ").Append(w.LastKnownVehicle.Color.ToString());
                }
            }
            else
            {
                stringBuilder.Append("Last known on foot");
            }

            stringBuilder.Append(" at ").Append(w.LastKnownPosition.ToString());

            Log.Debug("EventVisualLost_EventRaised: " + stringBuilder.ToString(), "PoliceScanner");

            if (@event.Ped.PedData.CurrentChase != null && @event.Ped.PedData.CurrentChase.IsPlayersChase)
            {
                if (w.LastKnownInVehicle && @event.Ped.CurrentVehicle != null && @event.Ped.CurrentVehicle.Exists())
                {
                    AudioHelper.PlayActionInScannerUsingPosition("THIS_IS_CONTROL SUSPECT_LAST_SEEN_IN A_ERR" + CarColSpeak(@event.Ped.CurrentVehicle.Color.Index) + CarNameSpeak(@event.Ped.CurrentVehicle) + " IN_OR_ON_POSITION", w.LastKnownPosition);
                }
                else
                {
                    AudioHelper.PlayActionInScannerUsingPosition("ATTENTION_ALL_UNITS SUSPECT_LAST_SEEN IN_OR_ON_POSITION ON_FOOT", w.LastKnownPosition);
                }

                Main.TextWall.AddText("[" + CultureHelper.GetText("POLICE_SCANNER_CONTROL") + "] " + CultureHelper.GetText("POLICE_SCANNER_VISUAL_LOST"));
            }
        }

        /// <summary>
        /// Called when criminal has left a vehicle during a chase.
        /// </summary>
        /// <param name="event">The event.</param>
        private static void EventCriminalLeftVehicle_EventRaised(EventCriminalLeftVehicle @event)
        {
            Log.Debug("EventCriminalLeftVehicle_EventRaised: Criminal is on foot now", "PoliceScanner");
            if (@event.Ped.PedData.CurrentChase.IsPlayersChase)
            {
                // Only if at least one officer has visual
                if (@event.Ped.Wanted.VisualLostSince == 0)
                {
                    // If suspect can be seen by other cops, make them report report
                    if (@event.Ped.Wanted.OfficersVisual > 0)
                    {
                        if (Common.GetRandomBool(0, 2, 1))
                        {
                            AudioHelper.PlayActionInScannerUsingPosition("SUSPECT HAS_ABANDONED_VEHICLE_AND_IS_CONTINUING_ON_FOOT", new GTA.Vector3(0f, 0f, 0f));
                        }
                        else
                        {
                            AudioHelper.PlayActionInScannerUsingPosition("SUSPECT HAS_FLED_VEHICLE_AND_IS_CONTINUING_ON_FOOT", new GTA.Vector3(0f, 0f, 0f));
                        }
                    }
                    else
                    {
                        AudioHelper.PlaySpeechInScannerNoNoiseFromPed(CPlayer.LocalPlayer.Ped, "SUSPECT_IS_ON_FOOT");
                    }

                    Main.TextWall.AddText("[" + CultureHelper.GetText("POLICE_SCANNER_CONTROL") + "] " + CultureHelper.GetText("POLICE_SCANNER_SUSPECT_ON_FOOT"));
                }
            }
        }

        /// <summary>
        /// Called when criminal has entered a vehicle during a chase.
        /// </summary>
        /// <param name="event">The event.</param>
        private static void EventCriminalEnteredVehicle_EventRaised(EventCriminalEnteredVehicle @event)
        {
            Log.Debug("EventCriminalEnteredVehicle_EventRaised: Criminal is in a vehicle now", "PoliceScanner");
            if (@event.Ped.PedData.CurrentChase.IsPlayersChase)
            {
                // Only if at least one officer has visual
                if (@event.Ped.Wanted.VisualLostSince == 0)
                {
                    // If suspect can be seen by other cops, make them report report
                    if (@event.Ped.Wanted.OfficersVisual > 0)
                    {
                        if (@event.Ped.CurrentVehicle.Model.IsBike)
                        {
                            AudioHelper.PlayActionInScanner("SUSPECT_IS_ON_BIKE");
                        }
                        else
                        {
                            AudioHelper.PlayActionInScanner("SUSPECT_IS_IN_CAR");
                        }
                    }
                    else
                    {
                        // Player reports, so no noise
                        if (@event.Ped.CurrentVehicle.Model.IsBike)
                        {
                            AudioHelper.PlaySpeechInScannerNoNoiseFromPed(CPlayer.LocalPlayer.Ped, "SUSPECT_IS_ON_BIKE");
                        }
                        else
                        {
                            AudioHelper.PlaySpeechInScannerNoNoiseFromPed(CPlayer.LocalPlayer.Ped, "SUSPECT_IS_IN_CAR");
                        }
                    }

                    if (CarNameSpeak(@event.Ped.CurrentVehicle) != "Error" && !string.IsNullOrWhiteSpace(CarNameSpeak(@event.Ped.CurrentVehicle)))
                    {
                        CVehicle vehicle = @event.Ped.CurrentVehicle;
                        DelayedCaller.Call(delegate { AudioHelper.PlayActionInScannerUsingPosition("SUSPECT IS_NOW_DRIVING_A_ERR" + CarColSpeak(vehicle.Color.Index) + CarNameSpeak(vehicle), new GTA.Vector3(0f, 0f, 0f)); }, 4000);
                    }

                    // Get model and color
                    string model = @event.Ped.CurrentVehicle.Model.ModelInfo.Name;
                    string color = CarColSpeak(@event.Ped.CurrentVehicle.Color.Index);

                    Main.TextWall.AddText("[" + CultureHelper.GetText("POLICE_SCANNER_CONTROL") + "] " + string.Format(CultureHelper.GetText("POLICE_SCANNER_SUSPECT_IN_VEHICLE"), color, model));
                }
            }
        }

        /// <summary>
        /// Called when a criminal has escaped.
        /// </summary>
        /// <param name="event"></param>
        private static void EventCriminalEscaped_EventRaised(EventCriminalEscaped @event)
        {
            Log.Debug("EventCriminalEscaped_EventRaised: Criminal has escaped", "PoliceScanner");
            if (@event.Ped.PedData.CurrentChase.IsPlayersChase)
            {
                Main.TextWall.AddText("[" + CultureHelper.GetText("POLICE_SCANNER_CONTROL") + "] " + CultureHelper.GetText("POLICE_SCANNER_SUSPECT_ESCAPED"));
            }
        }

        /// <summary>
        /// Called when an officer has regained visual on a suspect.
        /// </summary>
        /// <param name="event">The event.</param>
        private static void EventCriminalSpotted_EventRaised(EventCriminalSpotted @event)
        {
            Log.Debug("EventCriminalSpotted_EventRaised: Visual regained", "PoliceScanner");
            if (@event.Ped.PedData.CurrentChase.IsPlayersChase)
            {
                float heading = @event.Ped.Heading;
                GTA.Vector3 position = @event.Ped.Position;
                DelayedCaller.Call(delegate { AudioHelper.PlayDispatchChaseUpdateOnSuspect(@event.Ped); }, 2000);

                string area = AreaHelper.GetAreaNameMeaningful(position);
                TextHelper.AddTextToTextWall("[" + CultureHelper.GetText("POLICE_SCANNER_CONTROL") + "] " + string.Format(CultureHelper.GetText("POLICE_SCANNER_VISUAL_REGAINED"), area), true);
            }
        }

        /// <summary>
        /// Called when a suspect goes over 50 speed
        /// </summary>
        /// <param name="event">The event.</param>
        private static void EventCriminalSpeeding_EventRaised(EventCriminalSpeeding @event)
        {
            Log.Debug("EventCriminalSpeeding_EventRaised: Criminal is speeding", "PoliceScanner");
            if (@event.Ped.PedData.CurrentChase.IsPlayersChase)
            {
                float speed = @event.Ped.CurrentVehicle.Speed;
                GTA.Vector3 position = @event.Ped.Position;
                DelayedCaller.Call(delegate { AudioHelper.PlayDispatchIssueTrafficAlertForSuspect(speed, position); }, 5000);

                if (speed >= 37.5f)
                {
                    speed = 90;
                }
                else if (speed >= 35f)
                {
                    speed = 80;
                }
                else if (speed >= 32.5f)
                {
                    speed = 70;
                }
                else
                {
                    speed = 60;
                }

                TextHelper.AddTextToTextWall("[" + CultureHelper.GetText("POLICE_SCANNER_CONTROL") + "] " + string.Format(CultureHelper.GetText("POLICE_SCANNER_SUSPECT_SPEEDING"), speed), true);
            }
        }

        /// <summary>
        /// Called when a helicopter dies
        /// </summary>
        /// <param name="event">The helicopter.</param>
        private static void EventHelicopterDown_EventRaised(EventHelicopterDown @event)
        {
            Log.Debug("EventHelicopterDown_EventRaised: Helicopter died", "PoliceScanner");
            if (@event.Vehicle.Position.DistanceTo(CPlayer.LocalPlayer.Ped.Position) < 200)
            {
                string intro = "ALL_UNITS_ALL_UNITS";
                if (Common.GetRandomBool(0, 2, 1)) intro = "ALL_UNITS";

                string eventIntro = "INS_WE_HAVE";
                if (Common.GetRandomBool(0, 2, 1)) eventIntro = "INS_WEVE_GOT";

                string eventDetails = "CRIM_AN_AIR_UNIT_DOWN IN_OR_ON_POSITION";
                if (Common.GetRandomBool(0, 2, 1)) eventDetails = "CRIM_AN_AIR_UNIT_DOWN I_REPEAT CRIM_AN_AIR_UNIT_DOWN IN_OR_ON_POSITION";

                GTA.Vector3 position = @event.Vehicle.Position;
                DelayedCaller.Call(delegate { AudioHelper.PlayActionInScannerUsingPosition(intro + " " + eventIntro + " " + eventDetails, position); }, Common.GetRandomValue(6000, 8000));

                string area = AreaHelper.GetAreaNameMeaningful(@event.Vehicle.Position);
                TextHelper.AddTextToTextWall("[" + CultureHelper.GetText("POLICE_SCANNER_CONTROL") + "] " + string.Format(CultureHelper.GetText("POLICE_SCANNER_HELI_DOWN"), area), true);
            }
        }


        /// <summary>
        /// Called when an officer is down.
        /// </summary>
        /// <param name="event">The event.</param>
        private static void EventOfficerDown_EventRaised(EventOfficerDown @event)
        {
            Log.Debug("EventOfficerDown_EventRaised: Officer down at " + @event.Ped.Position.ToString(), "PoliceScanner");

            // Check if there is a combat in the area, if so, don't report
            // TODO: Maybe dispatch more cops to support? Or let the user rather decide?

            if (!CPed.IsPedInCombatInArea(@event.Ped.Position, 50))
            {
                ReportOfficerDown(@event.Ped, false);
            }
            else
            {
                Log.Debug("EventOfficerDown_EventRaised: Combat in area, no report.", "PoliceScanner");
            }
        }

        /// <summary>
        /// Called when an officer is attacked.
        /// </summary>
        /// <param name="event">The event.</param>
        private static void EventOfficerAttacked_EventRaised(EventOfficerAttacked @event)
        {
            Log.Debug("EventOfficerAttacked_EventRaised: Officer attacked at " + @event.Ped.Position.ToString(), "PoliceScanner");

            if (@event.ForceReport || @event.Ped.PedData.ReportBeingAttacked)
            {
                //ReportOfficerAttacked(@event.Ped);
                if (@event.Ped.HasBeenDamagedBy(GTA.Weapon.Misc_AnyMelee) || @event.Ped.HasBeenDamagedBy(GTA.Weapon.Misc_RammedByCar))
                {
                    DelayedCaller.Call(delegate { if (@event.Ped.Exists()) AudioHelper.PlayDispatchAcknowledgeReportedCrime(@event.Ped.Position, AudioHelper.EPursuitCallInReason.Assaulted); }, Common.GetRandomValue(4000, 6000));
                }
                else 
                {
                    DelayedCaller.Call(delegate { if (@event.Ped.Exists()) AudioHelper.PlayDispatchAcknowledgeReportedCrime(@event.Ped.Position, AudioHelper.EPursuitCallInReason.ShotAt); }, Common.GetRandomValue(4000, 6000));
                }
               
                @event.Ped.ClearLastDamageEntity();

                string area = AreaHelper.GetAreaNameMeaningful(@event.Ped.Position);
                TextHelper.AddTextToTextWall("[" + CultureHelper.GetText("POLICE_SCANNER_CONTROL") + "] " + string.Format(CultureHelper.GetText("POLICE_SCANNER_OFFICER_ATTACKED"), area), true);
            }
            else
            {
                Log.Debug("EventOfficerDown_EventRaised: Cop already reported attack, no report.", "PoliceScanner");
            }
        }

        /*
        /// <summary>
        /// Called when a ped is dead.
        /// </summary>
        /// <param name="event">The event.</param>
        private static void EventPedDead_EventRaised(EventPedDead @event)
        {
            // Do nothing when not on duty
            if (!Globals.IsOnDuty)
            {
                return;
            }

            if (@event.Ped.Exists())
            {
                // If ped was in player chase, report kill
                if (@event.Ped.PedData.CurrentChase != null)
                {
                    if (@event.Ped.PedData.CurrentChase.IsPlayersChase)
                    {
                        if (@event.Ped.PedGroup == EPedGroup.Criminal)
                        {
                            string[] speech = new string[] { "SUSPECT_IS_DOWN", "SUSPECT_NEUTRALIZED", "SUSPECT_TAKEN_DOWN" };
                            DelayedCaller.Call(delegate { AudioHelper.PlayActionInScanner(Common.GetRandomCollectionValue<string>(speech)); }, 1500);
                        }
                    }
                }
                else
                {
                    // If ped was killed by player, investigate why
                    if (@event.Ped.HasBeenDamagedBy(CPlayer.LocalPlayer.Ped))
                    {
                        // If not a criminal, report a civlian down by player
                        if (@event.Ped.PedGroup != EPedGroup.Criminal)
                        {
                            Log.Debug("EventPedDead_EventRaised: Civilian killed by player", "PoliceScanner");
                            Stats.UpdateStat(Stats.EStatType.AccidentalKills, 1, @event.Ped.Position);
                        }
                        else if (@event.Ped.PedGroup == EPedGroup.Cop)
                        {
                            Log.Debug("EventPedDead_EventRaised: Player killed a cop", "PoliceScanner");
                        }
                    }

                    if (@event.Ped.PedGroup == EPedGroup.Player)
                    {
                        Log.Debug("EventPedDead_EventRaised: Player has been killed", "PoliceScanner");
                        Stats.UpdateStat(Stats.EStatType.OfficersKilled, 1, @event.Ped.Position);
                        DelayedCaller.Call(delegate { AudioHelper.PlayActionInScanner("BEEN_SHOT"); }, 2500);
                    }
                }
            }
        } * */
    }
}