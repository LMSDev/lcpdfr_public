namespace LCPD_First_Response.LCPDFR.Scripts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Web.UI.HtmlControls;
    using System.Xml.Serialization;

    using GTA;
    using GTA.value;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.GUI;

    using Globals = LCPD_First_Response.LCPDFR.Globals;
    using Main = LCPD_First_Response.LCPDFR.Main;

    /// <summary>
    /// Provides functions to saves all important data of the current session into a file to or restore from it.
    /// </summary>
    public class Savegame
    {
        [Serializable]
        public class SavedPlayerData : SavedPedData
        {
            public SavedWeaponData CurrentWeaponData;
            public bool OnDutyState;
            public SavedWeaponData[] WeaponData;

            public struct SavedWeaponData
            {
                public int Ammo;
                public GTA.Weapon Type;

                public SavedWeaponData(int ammo, GTA.Weapon type)
                {
                    this.Ammo = ammo;
                    this.Type = type;
                }
            }

            public SavedPlayerData()
            {
            }

            public SavedPlayerData(SavedWeaponData currentWeaponData, SavedWeaponData[] savedWeaponData, bool onDutyState, SavedPedData savedPedData)
                : base(savedPedData.position, savedPedData.heading, savedPedData.modelName, 
                savedPedData.vehicleHandle, savedPedData.VehicleSeat, savedPedData.alive, savedPedData.health, savedPedData.skinData)
            {
                this.CurrentWeaponData = currentWeaponData;
                this.WeaponData = savedWeaponData;
                this.OnDutyState = onDutyState;
            }

            public SavedPlayerData(SerializationInfo info, StreamingContext ctxt) : base(info, ctxt)
            {
                this.CurrentWeaponData = (SavedWeaponData)info.GetValue("PlayerDataCurrentWeaponData", typeof(SavedWeaponData));
                this.WeaponData = (SavedWeaponData[])info.GetValue("PlayerDataWeaponData", typeof(SavedWeaponData));
                this.OnDutyState = (bool)info.GetValue("PlayerDataOnDutyState", typeof(bool));
            }

            public new void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                base.GetObjectData(info, context);
                info.AddValue("PlayerDataCurrentWeaponData", this.CurrentWeaponData);
                info.AddValue("PlayerDataWeaponData", this.WeaponData);
                info.AddValue("PlayerDataOnDutyState", this.OnDutyState);
            }
        }

        [Serializable]
        public class SavedPedData : ISerializable
        {
            public bool alive;

            public Vector3 position;

            public float heading;

            public string modelName;

            public int vehicleHandle;

            public VehicleSeat VehicleSeat;

            public int health;

            public SavedSkinData[] skinData;

            public struct SavedSkinData
            {
                public int ModelIndex;

                public int TextureIndex;

                public SavedSkinData(int modelIndex, int textureIndex)
                {
                    this.ModelIndex = modelIndex;
                    this.TextureIndex = textureIndex;
                }
            }

            public SavedPedData()
            {
                
            }

            public SavedPedData(Vector3 position, float heading, string modelName, int vehicleHandle, VehicleSeat vehicleSeat, bool alive, int health, SavedSkinData[] skinData)
            {
                this.position = position;
                this.heading = heading;
                this.modelName = modelName;
                this.vehicleHandle = vehicleHandle;
                this.VehicleSeat = vehicleSeat;
                this.alive = alive;
                this.health = health;
                this.skinData = skinData;
            }

            public SavedPedData(SerializationInfo info, StreamingContext ctxt)
            {
                this.position = (Vector3)info.GetValue("PedDataPosition", typeof(Vector3));
                this.heading = (float)info.GetValue("PedDataHeading", typeof(float));
                this.modelName = (string)info.GetValue("PedDataModelName", typeof(string));
                this.vehicleHandle = (int)info.GetValue("PedDataHandling", typeof(int));
                this.VehicleSeat = (VehicleSeat)info.GetValue("PedDataVehicleSeat", typeof(VehicleSeat));
                this.alive = (bool)info.GetValue("PedDataAlive", typeof(bool));
                this.health = (int)info.GetValue("PedDataHealth", typeof(int));
                this.skinData = (SavedSkinData[])info.GetValue("PedDataSkinData", typeof(SavedSkinData));
            }

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("PedDataPosition", this.position);
                info.AddValue("PedDataHeading", this.heading);
                info.AddValue("PedDataModelName", this.modelName);
                info.AddValue("PedDataHandling", this.vehicleHandle);
                info.AddValue("PedDataVehicleSeat", this.VehicleSeat);
                info.AddValue("PedDataAlive", this.alive);
                info.AddValue("PedDataHealth", this.health);
                info.AddValue("PedDataSkinData", this.skinData);
            }
        }

        [Serializable]
        public class SavedVehicleData : ISerializable
        {
            public Vector3 position;

            public int[] colors;

            public float heading;

            public string modelName;

            public int handle;

            public float health;

            public SavedVehicleData()
            {
                
            }

            public SavedVehicleData(Vector3 position, float heading, string modelName, int handle, int[] colors, float health)
            {
                this.position = position;
                this.heading = heading;
                this.modelName = modelName;
                this.handle = handle;
                this.colors = colors;
                this.health = health;
            }

            public SavedVehicleData(SerializationInfo info, StreamingContext ctxt)
            {
                this.position = (Vector3)info.GetValue("VehicleDataPosition", typeof(Vector3));
                this.heading = (float)info.GetValue("VehicleDataHeading", typeof(float));
                this.modelName = (string)info.GetValue("VehicleDataModelName", typeof(string));
                this.handle = (int)info.GetValue("VehicleDataHandle", typeof(int));
                this.colors = (int[])info.GetValue("VehicleDataColors", typeof(int[]));
                this.health = (float)info.GetValue("VehicleDataHealth", typeof(float));
            }

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("VehicleDataPosition", this.position);
                info.AddValue("VehicleDataHeading", this.heading);
                info.AddValue("VehicleDataModelName", this.modelName);
                info.AddValue("VehicleDataHandle", this.handle);
                info.AddValue("VehicleDataColors", this.colors);
                info.AddValue("VehicleDataHealth", this.health);
            }
        }

        [Serializable]
        public class SavedCameraData : ISerializable
        {
            public Vector3 direction;

            public SavedCameraData()
            {
                
            }

            public SavedCameraData(Vector3 direction)
            {
                this.direction = direction;
            }

            public SavedCameraData(SerializationInfo info, StreamingContext ctxt)
            {
                this.direction = (Vector3)info.GetValue("CameraDataDirection", typeof(Vector3));
            }

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("CameraDataDirection", this.direction);
            }
        }

        [Serializable]
        public class SavedGameData : ISerializable
        {
            public SavedCameraData CameraData;

            public SavedPlayerData PlayerData;

            public List<SavedPedData> PedData;

            public List<SavedVehicleData> VehicleData; 

            public double CurrentTime;

            public Weather CurrentWeather;

            public SavedGameData()
            {
                
            }

            public SavedGameData(SavedPlayerData playerData, SavedCameraData cameraData, List<SavedPedData> pedData, List<SavedVehicleData> vehicleData, double currentTime, Weather currentWeather)
            {
                this.PlayerData = playerData;
                this.CameraData = cameraData;
                this.PedData = pedData;
                this.VehicleData = vehicleData;
                this.CurrentTime = currentTime;
                this.CurrentWeather = currentWeather;
            }

            public SavedGameData(SerializationInfo info, StreamingContext ctxt)
            {
                this.PlayerData = (SavedPlayerData)info.GetValue("PlayerData", typeof(SavedPlayerData));
                this.CameraData = (SavedCameraData)info.GetValue("CameraData", typeof(SavedCameraData));
                this.PedData = (List<SavedPedData>)info.GetValue("PedData", typeof(List<SavedPedData>));
                this.VehicleData = (List<SavedVehicleData>)info.GetValue("VehicleData", typeof(List<SavedVehicleData>));
                this.CurrentTime = (double)info.GetValue("CurrentTime", typeof(double));
                this.CurrentWeather = (Weather)info.GetValue("CurrentWeather", typeof(Weather));
            }

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("PlayerData", this.PlayerData);
                info.AddValue("CameraData", this.CameraData);
                info.AddValue("PedData", this.PedData);
                info.AddValue("VehicleData", this.VehicleData);
                info.AddValue("CurrentTime", this.CurrentTime);
                info.AddValue("CurrentWeather", this.CurrentWeather);
            }
        }

        /// <summary>
        /// Gets the time when the last save occured.
        /// </summary>
        public static DateTime? LastSave { get; private set; }

        public static void SaveCurrentGameToFile()
        {
            try
            {
                SaveCurrentGameToFile("Savegame.fr");
                LastSave = DateTime.Now;
            }
            catch (Exception ex)
            {
                Log.Warning("Error while trying to save current game session", "Savegame");
                ExceptionHandler.LogCriticalException(ex);
            }
        }

        public static void SaveCurrentGameToFile(string fileName)
        {
            SavedPlayerData playerData;
            List<SavedPedData> pedData = new List<SavedPedData>();
            List<SavedVehicleData> vehicleData = new List<SavedVehicleData>();

            // Save player data
            playerData = SavePlayer(CPlayer.LocalPlayer);

            // Save camera settings.
            SavedCameraData cameraData = new SavedCameraData(Game.CurrentCamera.Direction);

            // Count peds and vehicles.
            Engine.Log.Debug("Engine.Pools.PedPool.GetAll().Length: " + Engine.Pools.PedPool.GetAll().Length, "Savegame");
            Engine.Log.Debug("Engine.Pools.VehiclePool.GetAll().Length: " + Engine.Pools.VehiclePool.GetAll().Length, "Savegame");
            //Engine.Log.Info("Invalid handles below can be ignored", "Savegame");

            // Save all vehicles.
            foreach (CVehicle vehicle in Engine.Pools.VehiclePool.GetAll())
            {
                if (vehicle == null || !vehicle.Exists())
                {
                    continue;
                }

                SavedVehicleData savedVehicleData = SaveVehicle(vehicle, pedData);
                vehicleData.Add(savedVehicleData);
            }

            // Save all peds
            foreach (CPed ped in Engine.Pools.PedPool.GetAll())
            {
                if (ped == null || !ped.Exists() || ped.Handle == Game.LocalPlayer.Character.pHandle || ped.IsInVehicle)
                {
                    continue;
                }

                SavedPedData savedPedData = SavePed(ped);
                pedData.Add(savedPedData);
            }

            // Save time and weather
            double currentTime = World.CurrentDayTime.TotalDays;
            Weather weather = World.Weather;

            SavedGameData savedGameData = new SavedGameData(playerData, cameraData, pedData, vehicleData, currentTime, weather);

            // Serialize data
            Stream stream = File.Open(fileName, FileMode.Create);
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(SavedGameData));
            xmlSerializer.Serialize(stream, savedGameData);
            stream.Close();
        }

        public static bool LoadGameFromSaveFile()
        {
            return LoadGameFromSaveFile("Savegame.fr");
        }

        public static bool LoadGameFromSaveFile(string fileName)
        {
            SavedGameData savedGameData = null;

            // Read file
            Stream stream = File.Open(fileName, FileMode.Open);

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(SavedGameData));
            savedGameData = (SavedGameData)xmlSerializer.Deserialize(stream);
            stream.Close();

            // Fade screen out in order to prevent the game from doing any spawn related stuff.
            Game.FadeScreenOut(2500, true);

            // Restore settings
            World.CurrentDayTime = TimeSpan.FromDays(savedGameData.CurrentTime);
            World.Weather = savedGameData.CurrentWeather;

            // Restore player settings
            CPlayer.LocalPlayer.Ped.Position = savedGameData.PlayerData.position;
            CPlayer.LocalPlayer.Ped.Heading = savedGameData.PlayerData.heading;
            CPlayer.LocalPlayer.Model = "PLAYER";
            CPlayer.LocalPlayer.Model = savedGameData.PlayerData.modelName;

            // No respawn yet.
            PopulationManager.BlockAllSpawning();

            // Wipe pools.
            foreach (CPed ped in Engine.Pools.PedPool.GetAll())
            {
                if (ped != null && ped.Exists() && ped.Handle != Game.LocalPlayer.Character.pHandle)
                {
                    ped.Delete();
                }
            }

            foreach (GTA.Ped ped in World.GetAllPeds())
            {
                if (ped != null && ped.Exists() && ped.pHandle != Game.LocalPlayer.Character.pHandle)
                {
                    ped.Delete();
                }
            }

            foreach (CVehicle vehicle in Engine.Pools.VehiclePool.GetAll())
            {
                if (vehicle != null && vehicle.Exists())
                {
                    vehicle.Delete();
                }
            }

            foreach (GTA.Vehicle vehicle in World.GetAllVehicles())
            {
                if (vehicle != null && vehicle.Exists() && vehicle.pHandle != Game.LocalPlayer.Character.pHandle)
                {
                    vehicle.Delete();
                }
            }

            // Clear everything
            AreaHelper.ClearArea(Vector3.Zero, 4000f, true, true);

            // Count peds and vehicles.
            Engine.Log.Debug("Engine.Pools.PedPool.GetAll().Length: " + Engine.Pools.PedPool.GetAll().Length, "Savegame");
            Engine.Log.Debug("Engine.Pools.VehiclePool.GetAll().Length: " + Engine.Pools.VehiclePool.GetAll().Length, "Savegame");

            int playerVehicleHandle = savedGameData.PlayerData.vehicleHandle;
            CVehicle playerVehicle = null;
            List<CPed> permamentPeds = new List<CPed>();

            // Restore vehicles first so we can later warp their drivers
            foreach (SavedVehicleData savedVehicleData in savedGameData.VehicleData)
            {
                CVehicle vehicle = new CVehicle(savedVehicleData.modelName, savedVehicleData.position, EVehicleGroup.Normal);
                if (vehicle.Exists())
                {
                    vehicle.Heading = savedVehicleData.heading;
                    vehicle.Color = savedVehicleData.colors[0];
                    vehicle.FeatureColor1 = savedVehicleData.colors[1];
                    vehicle.FeatureColor2 = savedVehicleData.colors[2];
                    //vehicle.SpecularColor = savedVehicleData.colors[3];
                    vehicle.EngineHealth = savedVehicleData.health;

                    if (savedVehicleData.handle == playerVehicleHandle)
                    {
                        playerVehicle = vehicle;
                    }

                    // Check if vehicle is in use by any driver
                    foreach (SavedPedData savedPedData in savedGameData.PedData)
                    {
                        if (savedPedData.vehicleHandle == savedVehicleData.handle)
                        {
                            // Peds spawned in vehicles are not marked as no longer needed (otherwise they are sometimes converted to non-driving dummies), so we need to do that later.
                            CPed ped = LoadPedInVehicle(savedPedData, vehicle);
                            if (ped != null && ped.Exists())
                            {
                                permamentPeds.Add(ped);
                            }
                        }
                    }

                    vehicle.NoLongerNeeded();
                }
            }

            // Restore peds
            foreach (SavedPedData savedPedData in savedGameData.PedData)
            {
                if (savedPedData.vehicleHandle != -1)
                {
                    continue;
                }

                LoadPed(savedPedData);
            }

            // Count peds and vehicles.
            Engine.Log.Debug("Engine.Pools.PedPool.GetAll().Length: " + Engine.Pools.PedPool.GetAll().Length, "Savegame");
            Engine.Log.Debug("Engine.Pools.VehiclePool.GetAll().Length: " + Engine.Pools.VehiclePool.GetAll().Length, "Savegame");

            // Restore LCPDFR specific settings before player settings as we might overwrite them.
            if (savedGameData.PlayerData.OnDutyState)
            {
                if (!Globals.IsOnDuty)
                {
                    Main.Instance.PerformInitialLCPDFRStartUp();
                    Main.GoOnDutyScript.ForceOnDuty();
                    TextHelper.ClearHelpbox();

                    Log.Info("On duty restored", "Savegame");
                }
            }

            // Change model again to be sure.
            CPlayer.LocalPlayer.Model = "PLAYER";
            CPlayer.LocalPlayer.Model = savedGameData.PlayerData.modelName;
            CPlayer.LocalPlayer.Ped.PlaceCamBehind();

            // Restore weapons.
            CPlayer.LocalPlayer.Ped.Weapons.RemoveAll();
            foreach (SavedPlayerData.SavedWeaponData weaponData in savedGameData.PlayerData.WeaponData)
            {
                CPlayer.LocalPlayer.Ped.Weapons[weaponData.Type].Ammo = weaponData.Ammo;
            }

            // Set weapon back.
            CPlayer.LocalPlayer.Ped.SetWeapon(savedGameData.PlayerData.CurrentWeaponData.Type);

            // Restore player skin.
            int i = 0;
            foreach (GTA.PedComponent pedComponent in Enum.GetValues(typeof(GTA.PedComponent)))
            {
                CPlayer.LocalPlayer.Ped.Skin.Component[pedComponent].ChangeIfValid(savedGameData.PlayerData.skinData[i].ModelIndex, savedGameData.PlayerData.skinData[i].TextureIndex);
                i++;
            }

            // Warp in vehicle, if any.
            if (playerVehicle != null && playerVehicle.Exists())
            {
                CPlayer.LocalPlayer.Ped.WarpIntoVehicle(playerVehicle, savedGameData.PlayerData.VehicleSeat);
            }

            // Fade in back again.
            Game.FadeScreenIn(2500, true);

            // Make peds no longer needed, which will also trigger their ambient driving task.
            foreach (CPed ped in permamentPeds)
            {
                ped.NoLongerNeeded();
            }

            // Allow spawning again.
            PopulationManager.AllowAllSpawning();

            return true;
        }

        private static SavedPlayerData SavePlayer(CPlayer player)
        {
            List<SavedPlayerData.SavedWeaponData> weaponData = new List<SavedPlayerData.SavedWeaponData>();
            foreach (WeaponSlot weaponSlot in Enum.GetValues(typeof(WeaponSlot)))
            {
                if (weaponSlot == WeaponSlot.DetonatorUnknown) continue;

                GTA.Weapon type = player.Ped.Weapons.inSlot(weaponSlot).Type;
                if (type != GTA.Weapon.None)
                { 
                    weaponData.Add(new SavedPlayerData.SavedWeaponData(player.Ped.Weapons.inSlot(weaponSlot).Ammo, type));
                }
            }

            SavedPlayerData.SavedWeaponData currentWeaponData = new SavedPlayerData.SavedWeaponData(player.Ped.Weapons.Current.Ammo, player.Ped.Weapons.Current.Type);

            SavedPlayerData playerData = new SavedPlayerData(currentWeaponData, weaponData.ToArray(), Globals.IsOnDuty, SavePed(CPlayer.LocalPlayer.Ped));
            return playerData;
        }

        private static SavedPedData SavePed(CPed ped)
        {
            Vector3 position = ped.Position;
            CModel model = ped.Model;

            float heading = ped.Heading;
            int vehicleHandle = -1;
            VehicleSeat vehicleSeat = VehicleSeat.None;

            if (ped.IsInVehicle)
            {
                vehicleHandle = ped.CurrentVehicle.Handle;
                vehicleSeat = ped.GetSeatInVehicle();
            }

            // Save skin.
            List<SavedPedData.SavedSkinData> skinComponentData = new List<SavedPedData.SavedSkinData>();
            foreach (GTA.PedComponent pedComponent in Enum.GetValues(typeof(GTA.PedComponent)))
            {
                int modelIndex = ped.Skin.Component[pedComponent].ModelIndex;
                int textureIndex = ped.Skin.Component[pedComponent].TextureIndex;
                skinComponentData.Add(new SavedPedData.SavedSkinData(modelIndex, textureIndex));
            }

            return new SavedPedData(position, heading, model.ModelInfo.Name, vehicleHandle, vehicleSeat, ped.IsAliveAndWell, ped.Health, skinComponentData.ToArray());
        }

        private static SavedVehicleData SaveVehicle(CVehicle vehicle, List<SavedPedData> pedData)
        {
            Vector3 position = vehicle.Position;
            CModel model = vehicle.Model;

            float heading = vehicle.Heading;
            foreach (CPed ped in vehicle.GetAllPedsInVehicle())
            {
                if (ped != null && ped.Exists() && ped.Handle != Game.LocalPlayer.Character.pHandle)
                {
                    SavedPedData data = SavePed(ped);
                    pedData.Add(data);
                }
            }

            return new SavedVehicleData(position, heading, model.ModelInfo.Name, vehicle.Handle, new int[] { vehicle.Color.Index, vehicle.FeatureColor1.Index, vehicle.FeatureColor2.Index, vehicle.SpecularColor.Index }, vehicle.EngineHealth );
        }

        private static CPed LoadPed(SavedPedData pedData)
        {
            CPed ped = LoadPedBase(pedData);
            if (ped.Exists())
            {
                ped.NoLongerNeeded();
            }

            return ped;
        }

        private static CPed LoadPedInVehicle(SavedPedData pedData, CVehicle vehicle)
        {
            CPed ped = LoadPedBase(pedData);
            if (ped.Exists())
            {
                ped.WarpIntoVehicle(vehicle, pedData.VehicleSeat);

                // Prevent passengers from leaving the vehicle.
                if (pedData.VehicleSeat != VehicleSeat.Driver)
                {
                    ped.Task.AlwaysKeepTask = true;
                    ped.Task.Wait(Int32.MaxValue);
                }
            }

            return ped;
        }

        private static CPed LoadPedBase(SavedPedData pedData)
        {
            CPed ped = new CPed(pedData.modelName, pedData.position, EPedGroup.Unknown);
            if (ped.Exists())
            {
                // Apply skin.
                int i = 0;
                foreach (GTA.PedComponent pedComponent in Enum.GetValues(typeof(GTA.PedComponent)))
                {
                    ped.Skin.Component[pedComponent].ChangeIfValid(pedData.skinData[i].ModelIndex, pedData.skinData[i].TextureIndex);
                    i++;
                }

                ped.Heading = pedData.heading;
                ped.Health = pedData.health;
                if (!pedData.alive)
                {
                    ped.Die();
                }
            }

            return ped;
        }
    }
}