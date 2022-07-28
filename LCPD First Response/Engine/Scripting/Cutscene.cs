//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using GTA;
//using LCPD_First_Response.Engine.Networking;
//using LCPD_First_Response.Engine.Networking.Data;
//using Lidgren.Network;

//namespace LCPD_First_Response.Engine.Scripting
//{
//    class Cutscene : BaseComponent
//    {
//        private string name;

//        public Cutscene(string name)
//        {
//            this.name = name;

//        }

//        /// <summary>
//        /// Plays the cutscene. Note: This will ensure the model is player and also try to display localized text. This function will return when cutscene has finished
//        /// </summary>
//        public void Play(bool sync)
//        {
//            // Load text data
//            string[] textData = GetTextForCutscene();
//            if (textData != null)
//            {
//                foreach (string s in textData)
//                {
//                    //LoadTextForCutscene(s, );
//                }
//            }

//            if (sync)
//            {
//                DynamicData dynamicData = new DynamicData();
//                dynamicData.Write("YUSUF1");
//                dynamicData.Write((byte) 0);
//                Main.NetworkManager.Send(EMessageID.StartCutsceneLoadText, dynamicData, NetDeliveryMethod.ReliableUnordered, EMessageReceiver.AllClients);

//                dynamicData.Clear();
//                dynamicData.Write("E2Y1Aud");
//                dynamicData.Write((byte) 6);
//                Main.NetworkManager.Send(EMessageID.StartCutsceneLoadText, dynamicData, NetDeliveryMethod.ReliableUnordered, EMessageReceiver.AllClients);

//                dynamicData.Clear();
//                dynamicData.Write(name);
//                Main.NetworkManager.Send(EMessageID.StartCutscene, dynamicData, NetDeliveryMethod.ReliableUnordered, EMessageReceiver.AllClients);
//            }

//            Log.Debug("Start now", this);
//            GTA.Native.Function.Call("LOAD_ADDITIONAL_TEXT", "YUSUF1", 0);
//            GTA.Native.Function.Call("LOAD_ADDITIONAL_TEXT", "E2Y1Aud", 6);
//            GTA.Native.Function.Call("LOAD_ALL_OBJECTS_NOW");

//            // Ensure mode is luis
//            GTA.Model oldModel = GTA.Game.LocalPlayer.Model;
//            GTA.value.PedSkin oldSkin = GTA.Game.LocalPlayer.Character.Skin;
//            bool needModelSwitch = false;
//            if (GTA.Game.LocalPlayer.Model != "PLAYER")
//            {
//                GTA.Game.LocalPlayer.Model = "PLAYER";
//                needModelSwitch = true;
//            }

//            GTA.Native.Function.Call("START_CUTSCENE_NOW", name);
//            while (!GTA.Native.Function.Call<bool>("HAS_CUTSCENE_LOADED"))
//            {
//                Game.WaitInCurrentScript(0);
//                //Game.Console.Print("loading");
//                //WAIT(0);
//            }
//            Log.Debug("Loaded", this);
//            while (!GTA.Native.Function.Call<bool>("HAS_CUTSCENE_FINISHED"))
//            {
//                //Game.Console.Print("playing");
//                Game.WaitInCurrentScript(0);
//                //WAIT(0);
//            }
//            if (needModelSwitch)
//            {
//                GTA.Game.LocalPlayer.Model = oldModel;
//                GTA.Game.LocalPlayer.Skin.Template = oldSkin;
//            }

//            Log.Debug("Finished", this);
//            if (sync)
//            {
//                DynamicData dynamicData = new DynamicData();
//                dynamicData.Write(name);
//                Main.NetworkManager.Send(EMessageID.ClearCutscene, dynamicData, NetDeliveryMethod.ReliableUnordered, EMessageReceiver.AllClients);
//            }

//            GTA.Native.Function.Call("CLEAR_NAMED_CUTSCENE", name);
//            Game.FadeScreenIn(4000);
//            Log.Debug("Cleaned", this);
//        }

//        private string[] GetTextForCutscene()
//        {
//            return null;
//        }

//        private void LoadTextForCutscene(string textName, int id, bool sync)
//        {
            
//        }

//        public override string ComponentName
//        {
//            get { return "Cutscene"; }
//        }
//    }
//}
