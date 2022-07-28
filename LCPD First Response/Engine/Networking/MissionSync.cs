using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GTA;

namespace LCPD_First_Response.Engine.Networking
{
    class MissionSync
    {
        public MissionSync()
        {
            // Attach hooks
            Main.NativeHooker.Hook("START_CUTSCENE_NOW", StartCutsceneNowCallback);
            Main.NativeHooker.Hook("SET_MISSION_FLAG", SetMissionFlagCallback);
            Main.NativeHooker.Hook("ADD_BLIP_FOR_COORD", AddBlipForCoordCallback);
        }

        private void StartCutsceneNowCallback(params object[] arguments)
        {
            string name = (string)arguments[0];
            GTA.Game.Console.Print("Cutscene: " + name);
        }

        private void SetMissionFlagCallback(params object[] arguments)
        {
            bool b = (bool) arguments[0];
            GTA.Game.Console.Print("Mission flag: " + b.ToString());
        }

        private void AddBlipForCoordCallback(params object[] arguments)
        {
            float x, y, z;
            x = (float)arguments[0];
            y = (float)arguments[1];
            z = (float)arguments[2];
            Vector3 v = new Vector3(x, y, z);
            GTA.Game.Console.Print("Blip pos:" + v.ToString());
        }
    }
}
