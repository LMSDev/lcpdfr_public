namespace LCPD_First_Response.Engine.Scripting
{
    using System.Text.RegularExpressions;

    using GTA;

    /// <summary>
    /// Contains helper functions for areas in GTA, like getting an area's name.
    /// </summary>
    internal static class AreaHelper
    {
        /// <summary>
        /// Clears the area around <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="peds">Whether peds should be deleted.</param>
        /// <param name="vehicles">Whether vehicles should be deleted.</param>
        public static void ClearArea(Vector3 position, float radius, bool peds, bool vehicles)
        {
            if (peds)
            {
                Native.Natives.ClearAreaOfChars(position, radius);
            }

            if (vehicles)
            {
                Native.Natives.ClearAreaOfCars(position, radius);
            }
        }

        /// <summary>
        /// Gets the name of the area at <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <returns>The area name.</returns>
        public static string GetAreaName(Vector3 position)
        {
            return GetAreaName(World.GetZoneName(position));
        }

        /// <summary>
        /// Gets the name of the area of the zone name.
        /// </summary>
        /// <param name="zoneName">The zone name.</param>
        /// <returns>The area name.</returns>
        public static string GetAreaName(string zoneName)
        {
            if (zoneName == "LIBERTY") return "AT_SEA";

            string str3 = Regex.Replace(zoneName.Substring(1), "[^.A-Z]", string.Empty);

            switch (str3)
            {
                case "PENN":
                    return "ACTER_INDUSTRIAL_PARK";

                case "ACTI":
                    return "ACTER_INDUSTRIAL_PARK";

                case "PORT":
                    return "PORT_TUDOR";

                case "ACT":
                    return "ACTER";

                case "TUDO":
                    return "TUDOR";

                case "NORM":
                    return "NORMANDY";

                case "BERC":
                    return "BERCHEM";

                case "ALD":
                    return "ALDERNEY_CITY";

                case "LEFT":
                    return "LEFTWOOD";

                // Actually it's Westdyke, but the audio is named WESTDYK for some reason. Maybe rather fix the typo in the audio than the string here later
                case "WEST":
                    return "WESTDYK";
            }

            if (zoneName == "ZBRI8")
            {
                return "VARSITY_HEIGHTS";
            }
            else
            {
                switch (str3)
                {
                    case "NORT":
                        return "NORTHWOOD";

                    case "EHOL":
                        return "EAST_HOLLAND";

                    case "NHOL":
                        return "NORTH_HOLLAND";

                    case "VARH":
                        return "VARSITY_HEIGHTS";

                    case "MIDPA":
                        return "MIDDLE_PARK";

                    case "LANC":
                        return "LANCASTER";

                    case "MIDE":
                        return "MIDDLE_PARK_EAST";

                    case "MDW":
                        return "MIDDLE_PARK_WEST";

                    case "PURG":
                        return "PURGATORY";

                    case "STAR":
                        return "STAR_JUNCTION";

                    case "HAT":
                        return "HATTON_GARDENS";

                    case "LANCE":
                        return "LANCET";
                }

                if (zoneName == "ZBRI2")
                {
                    return "ALGONQUIN_BRIDGE";
                }
                else if (str3 == "WESTM")
                {
                    return "WESTMINSTER";
                }
                else if (zoneName.Contains("ZBOO"))
                {
                    return "ASAHARA_RD";
                }
                else
                {
                    switch (str3)
                    {
                        case "MEAT":
                            return "THE_MEAT_QUARTER";

                        case "TRI":
                            return "THE_TRIANGLE";

                        case "EAST":
                            return "EASTON";

                        case "PRES":
                            return "PRESIDENTS_CITY";

                        case "LEAP":
                            return "COLONY_ISLAND";

                        case "COIS":
                            return "COLONY_ISLAND";

                        case "FISN":
                            return "FISH_MARKET_NORTH";

                        case "LOWE":
                            return "LOWER_EASTON";

                        case "ITAL":
                            return "LITTLE_ITALY";

                        case "SUFF":
                            return "SUFFOLK";

                        case "CGCI":
                            return "CASTLE_GARDEN_CITY";

                        case "CITY":
                            return "CITY_HALL";

                        case "CHIN":
                            return "CHINATOWN";

                        case "FISS":
                            return "FISHMARKET_SOUTH";
                    }

                    if (zoneName == "ZBRI1")
                    {
                        return "BROKER_BRDIGE";
                    }
                    else
                    {
                        switch (str3)
                        {
                            case "EXC":
                                return "THE_EXCHANGE";

                            case "CGAR":
                                return "CASTLE_GARDENS";

                            case "HAP":
                                return "HAPPINESS_ISLAND";
                        }

                        switch (zoneName)
                        {
                            case "ZBRI3":
                            case "ZBRI4":
                            case "ZBRI5":
                                return "CHARGE_ISLAND";
                        }

                        switch (str3)
                        {
                            case "CHISL":
                                return "CHARGE_ISLAND";

                            case "SOHAN":
                                return "SOUTH_BOHAN";

                            case "CHASE":
                                return "CHASE_POINT";

                            case "INDUS":
                                return "INDUSTRIAL";

                            case "FORT":
                                return "FORTSIDE";

                            case "BOULE":
                                return "BOULEVARD";
                        }

                        if (zoneName == "ZBRI7")
                        {
                            return "BOULEVARD";
                        }
                        else
                        {
                            switch (str3)
                            {
                                case "NRDNS":
                                    return "NORTHERN_GARDENS";

                                case "LBAY":
                                    return "LITTLE_BAY";
                            }

                            if (zoneName == "ZBRI6")
                            {
                                return "LITTLE_BAY";
                            }
                            else
                            {
                                switch (str3)
                                {
                                    case "STEI":
                                        return "STEIN_WAY";

                                    case "MPARK":
                                        return "MEADOWS_PARK";

                                    case "ESTCT":
                                        return "EAST_ISLAND_CITY";

                                    case "CERV":
                                        return "CERVAZA_HEIGHTS";

                                    case "MHILLS":
                                        return "MEADOW_HILLS";

                                    case "WILIS":
                                        return "WILLIS";

                                    case "AIRPT":
                                        return "FRANCIS_INTERNATIONAL";

                                    case "BECCT":
                                        return "BEECHWOOD_CITY";

                                    case "SHTLER":
                                        return "SCHOTTLER";

                                    case "DOWNT":
                                        return "DOWNTOWN";

                                    case "BOAB":
                                        return "BOABO";

                                    case "RHIL":
                                        return "ROTTERDAM_HILL";

                                    case "EHOK":
                                        return "EAST_HOOK";

                                    case "OUTLO":
                                        return "OUTLOOK";

                                    case "SLOPES":
                                        return "SOUTH_SLOPES";
                                }

                                switch (str3)
                                {
                                    case "SLOPES":
                                        return "SOUTH_SLOPES";

                                    case "FIEPR":
                                        return "FIREFLY_PROJECTS";

                                    case "HOVEB":
                                        return "HOVE_BEACH";
                                }

                                switch (str3)
                                {
                                    case "HOVEB":
                                        return "HOVE_BEACH";

                                        // Actually it's BEACHGATE, but there is no audio for it
                                    case "BEG":
                                        return "FIREFLY_ISLAND";

                                    case "FRIS":
                                        return "FIREFLY_ISLAND";
                                }

                                return "UNKNOWN";
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets a more detailed area name of <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <returns>The area name.</returns>
        public static string GetAreaNameMeaningful(Vector3 position)
        {
            return GetAreaNameMeaningful(World.GetZoneName(position));
        }

        /// <summary>
        /// Gets a more detailed area name of <paramref name="zoneName"/>.
        /// </summary>
        /// <param name="zoneName">The zone name.</param>
        /// <returns>The area name.</returns>
        public static string GetAreaNameMeaningful(string zoneName)
        {
            string str3 = Regex.Replace(zoneName.Substring(1), "[^.A-Z]", string.Empty);
            if (zoneName == "LIBERTY") return "at sea";

            switch (str3)
            {
                case "PENN":
                    return "Acter Industrial Park";

                case "ACTI":
                    return "Acter Industrial Park";

                case "PORT":
                    return "Port Tudor";

                case "ACT":
                    return "Acter";

                case "TUDO":
                    return "Tudor";

                case "NORM":
                    return "Normandy";

                case "BERC":
                    return "Berchem";

                case "ALD":
                    return "Alderney City";

                case "LEFT":
                    return "Leftwood";

                case "WEST":
                    return "Westdyke";
            }

            if (zoneName == "ZBRI8")
            {
                return "Hickey Bridge";
            }
            else
            {
                switch (str3)
                {
                    case "NORT":
                        return "Northwood";

                    case "EHOL":
                        return "East Holland";

                    case "NHOL":
                        return "North Holland";

                    case "VARH":
                        return "Varsity Heights";

                    case "MIDPA":
                        return "Middle Park";

                    case "LANC":
                        return "Lancaster";

                    case "MIDE":
                        return "Middle Park East";

                    case "MDW":
                        return "Middle Park West";

                    case "PURG":
                        return "Purgatory";

                    case "STAR":
                        return "Star Junction";

                    case "HAT":
                        return "Hatton Gardens";

                    case "LANCE":
                        return "Lancet";
                }

                if (zoneName == "ZBRI2")
                {
                    return "Algonquin Bridge";
                }
                else if (str3 == "WESTM")
                {
                    return "Westminster";
                }
                else if (zoneName.Contains("ZBOO"))
                {
                    return "Booth Tunnel";
                }
                else
                {
                    switch (str3)
                    {
                        case "MEAT":
                            return "The Meat Quarter";

                        case "TRI":
                            return "The Triangle";

                        case "EAST":
                            return "Easton";

                        case "PRES":
                            return "Presidents City";

                        case "LEAP":
                            return "Colony Island";

                        case "COIS":
                            return "Colony Island";

                        case "FISN":
                            return "Fish Market North";

                        case "LOWE":
                            return "Lower Easton";

                        case "ITAL":
                            return "Little Italy";

                        case "SUFF":
                            return "Suffolk";

                        case "CGCI":
                            return "Castle Garden City";

                        case "CITY":
                            return "City Hall";

                        case "CHIN":
                            return "Chinatown";

                        case "FISS":
                            return "Fishmarket South";
                    }

                    if (zoneName == "ZBRI1")
                    {
                        return "Broker Bridge";
                    }
                    else
                    {
                        switch (str3)
                        {
                            case "EXC":
                                return "The Exchange";

                            case "CGAR":
                                return "Castle Gardens";

                            case "HAP":
                                return "Happiness Island";
                        }

                        switch (zoneName)
                        {
                            case "ZBRI3":
                            case "ZBRI4":
                            case "ZBRI5":
                                return "Charge Island";
                        }

                        switch (str3)
                        {
                            case "CHISL":
                                return "Charge Island";

                            case "SOHAN":
                                return "South Bohan";

                            case "CHASE":
                                return "Chase Point";

                            case "INDUS":
                                return "Industrial";

                            case "FORT":
                                return "Fortside";

                            case "BOULE":
                                return "Boulevard";
                        }

                        if (zoneName == "ZBRI7")
                        {
                            return "Northwood Heights Bridge";
                        }
                        else
                        {
                            switch (str3)
                            {
                                case "NRDNS":
                                    return "Northern Gardens";

                                case "LBAY":
                                    return "Little Bay";
                            }

                            if (zoneName == "ZBRI6")
                            {
                                return "Dukes Bay Bridge";
                            }
                            else
                            {
                                switch (str3)
                                {
                                    case "STEI":
                                        return "Steinway";

                                    case "MPARK":
                                        return "Middle Park";

                                    case "ESTCT":
                                        return "East Island City";

                                    case "CERV":
                                        return "Cervaza Heights";

                                    case "MHILLS":
                                        return "Meadow Hills";

                                    case "WILIS":
                                        return "Willis";

                                    case "AIRPT":
                                        return "Francis International Airport";

                                    case "BECCT":
                                        return "Beechwood City";

                                    case "SHTLER":
                                        return "Schottler";

                                    case "DOWNT":
                                        return "Downtown";

                                    case "BOAB":
                                        return "Boabo";

                                    case "RHIL":
                                        return "Rotterdam Hill";

                                    case "EHOK":
                                        return "East Hook";

                                    case "OUTLO":
                                        return "Outlook";

                                    case "SLOPES":
                                        return "South Slopes";
                                }

                                switch (str3)
                                {
                                    case "SLOPES":
                                        return "South Slopes";

                                    case "FIEPR":
                                        return "Firefly Projects";

                                    case "HOVEB":
                                        return "Hove Beach";
                                }

                                switch (str3)
                                {
                                    case "HOVEB":
                                        return "Hove Beach";

                                    case "BEG":
                                        return "Beachgate";

                                    case "FRIS":
                                        return "Firefly Island";
                                }

                                return "UNKNOWN";
                            }
                        }
                    }
                }
            }
        }
    }
}
