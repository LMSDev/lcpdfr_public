namespace LCPD_First_Response.Engine.DevTools
{
    using System;
    using System.Collections.Generic;

    using GTA;

    using LCPD_First_Response.Engine.GUI;
    using LCPD_First_Response.Engine.GUI.Forms;
    using LCPD_First_Response.Engine.Input;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.LCPDFR.GUI;

    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Class containing several developer tools (often bound to console commands).
    /// </summary>
    internal static class DevTools
    {
        private static CPed[] peds;

        /// <summary>
        /// Test image.
        /// </summary>
        private static Image image;

        /// <summary>
        /// Roadblock helper.
        /// </summary>
        private static RoadblockHelper roadblockHelper;

        /// <summary>
        /// Initializes static members of the <see cref="DevTools"/> class.
        /// </summary>
        static DevTools()
        {
            roadblockHelper = new RoadblockHelper();
        }

        [ConsoleCommand("RoadblockHelperDelete")]
        private static void DeleteRoadBlockCars(ParameterCollection parameterCollection)
        {
            roadblockHelper.Delete();
        }

        [ConsoleCommand("RoadblockHelperStart")]
        private static void StartRoadblockHelper(ParameterCollection parameterCollection)
        {
            roadblockHelper.Start();
        }

        [ConsoleCommand("RoadblockHelperSaveCar")]
        private static void SaveRoadblockCarCoordinate(ParameterCollection parameterCollection)
        {
            roadblockHelper.SaveVehicle();
        }

        [ConsoleCommand("RoadblockHelperSpawn")]
        private static void SpawnRoadBlockCars(ParameterCollection parameterCollection)
        {
            roadblockHelper.Spawn();
        }

        [ConsoleCommand("RoadblockHelperStop")]
        private static void StopRoadblockHelper(ParameterCollection parameterCollection)
        {
            roadblockHelper.Stop();
        }

        [ConsoleCommand("ImageTest")]
        private static void ImageTest(ParameterCollection parameterCollection)
        {
            if (image != null)
            {
                image.Dispose();
                image = null;
            }

            // Default parameters
            string file = "fbi";
            int width = 48;
            int height = 48;
            int x = 50;
            int y = 50;

            string[] parameters = parameterCollection.ToArray();
            if (parameters.Length > 0)
            {
                file = parameters[0];
            }

            if (parameters.Length > 1)
            {
                width = Convert.ToInt32(parameters[1]);
            }

            if (parameters.Length > 2)
            {
                height = Convert.ToInt32(parameters[2]);
            }

            if (parameters.Length > 3)
            {
                x = Convert.ToInt32(parameters[3]);
            }

            if (parameters.Length > 4)
            {
                y = Convert.ToInt32(parameters[4]);
            }
            
            image = new Image(string.Format("LCPDFR\\{0}.png", file), width, height, x, y);
        }

        [ConsoleCommand("AddText")]
        private static void AddText(ParameterCollection parameterCollection)
        {
            string text = parameterCollection[0];
            LCPDFR.Main.TextWall.AddText(text);
        }

        [ConsoleCommand("Transform")]
        private static void TestSpawn(ParameterCollection parameterCollection)
        {
            Game.LocalPlayer.Model = "M_Y_SWAT";
            CPlayer.LocalPlayer.Ped.Weapons.AssaultRifle_AK47.Ammo = 4000;
        }

        [ConsoleCommand("War")]
        private static void War(ParameterCollection parameterCollection)
        {
            List<CPed> addedPeds = new List<CPed>();

            foreach (CPed ped in Pools.PedPool.GetAll())
            {
                if (ped.Exists())
                {
                    if (ped.IsAliveAndWell)
                    {
                        if (ped.PedGroup != EPedGroup.Cop && ped.PedGroup != EPedGroup.Player)
                        {
                            ped.BecomeMissionCharacter();
                            ped.Weapons.Glock.Ammo = 4000;
                            ped.BlockPermanentEvents = true;
                            ped.Task.AlwaysKeepTask = true;
                            ped.Task.FightAgainst(CPlayer.LocalPlayer.Ped);
                            ped.AttachBlip();
                            addedPeds.Add(ped);
                        }
                    }
                }
            }

            peds = addedPeds.ToArray();
        }

        [ConsoleCommand("Clean")]
        private static void Clean(ParameterCollection parameterCollection)
        {
            foreach (CPed ped in peds)
            {
                if (ped.Exists())
                {
                    ped.NoLongerNeeded();
                }
            }
        }

        [ConsoleCommand("ClearAll", "Clears all tasks and frees all entities.")]
        private static void ClearAll(ParameterCollection parameterCollection)
        {
            // Finish ped tasks
            foreach (CPed ped in Pools.PedPool.GetAll())    
            {
                if (ped.Exists())
                {
                    ped.Intelligence.TaskManager.ClearTasks();
                    ped.Task.ClearAll();
                }
            }

            // Finish anonymous tasks
            Main.TaskManager.ClearTasks();

            Log.Debug("Everything cleared!", "DevTools");
        }

        [ConsoleCommand("ShowID")]
        private static void ShowID(ParameterCollection parameterCollection)
        {
            CPed ped = CPlayer.LocalPlayer.Ped.Intelligence.GetClosestPed(EPedSearchCriteria.AmbientPed, 5f);
            if (ped != null && ped.Exists())
            {
                // Get persona data
                string name = ped.PedData.Persona.Forename + " " + ped.PedData.Persona.Surname;
                DateTime birthDay = ped.PedData.Persona.BirthDay;
                int citations = ped.PedData.Persona.Citations;
                Gender gender = ped.PedData.Persona.Gender;
                ELicenseState license = ped.PedData.Persona.LicenseState;
                int timesStopped = ped.PedData.Persona.TimesStopped;
                bool wanted = ped.PedData.Persona.Wanted;

                string data = name + Environment.NewLine + birthDay.ToLongDateString() + Environment.NewLine
                              + "Citations: " + citations.ToString() + Environment.NewLine + "Gender: "
                              + gender.ToString() + Environment.NewLine + "License: " + license.ToString()
                              + Environment.NewLine + "TimesStopped: " + timesStopped + Environment.NewLine + "Wanted: "
                              + wanted.ToString();
                Log.Debug(data, "DevTools");
            }
        }

        [ConsoleCommand("SpawnAttackers")]
        private static void SpawnAttackers(ParameterCollection parameterCollection)
        {
            CPed[] peds = new CPed[4];

            for (int i = 0; i < peds.Length; i++)
            {
                peds[i] = new CPed("M_Y_PJERSEY_01", CPlayer.LocalPlayer.Ped.Position, EPedGroup.Criminal);

                if (peds[i].Exists())
                {
                    peds[i].RelationshipGroup = RelationshipGroup.Gang_AfricanAmerican;
                    peds[i].ChangeRelationship(RelationshipGroup.Gang_AfricanAmerican, Relationship.Companion);
                    peds[i].EnsurePedHasWeapon();
                    peds[i].Task.FightAgainst(CPlayer.LocalPlayer.Ped);
                    peds[i].AlwaysFreeOnDeath = true;
                    peds[i].AttachBlip();
                }
            }
        }
        
        [ConsoleCommand("TestMasterserverQueue")]
        private static void TestMasterserverQueue(ParameterCollection parameterCollection)
        {
            Networking.QueueMessageHandler.WallMessage wallMessage = new Networking.QueueMessageHandler.WallMessage();
            Newtonsoft.Json.Linq.JObject rawObject = wallMessage.CreateMessage("Hello, world!");
            Main.LCPDFRServer.SendQueueItemToSession(Main.LCPDFRServer.SessionID, rawObject);
        }

        [ConsoleCommand("FindSession")]
        private static void FindSession(ParameterCollection parameterCollection)
        {
            if (parameterCollection.Count == 0)
            {
                return;
            }

            string name = parameterCollection[0];
            JArray results = Main.LCPDFRServer.FindSessionsByVariableSearch("IsNetworkSession", false.ToString());
            if (results != null)
            {
                for (int i = 0; i < results.Count; i++)
                {
                    JToken result = results[i];
                    if (result["vars"] != null)
                    {
                        if (result["vars"]["LocalName"] != null)
                        {
                            string playerName = (string)result["vars"]["LocalName"];
                            Log.Debug("PlayerName: " + playerName, "DevTools");
                        }
                    }
                }
            }
        }
    }
}
