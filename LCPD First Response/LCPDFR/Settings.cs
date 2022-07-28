namespace LCPD_First_Response.LCPDFR
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows.Forms;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Input;
    using LCPD_First_Response.Engine.Scripting.Entities;

    using SlimDX.XInput;

    using SettingsFile = LCPD_First_Response.Engine.IO.SettingsFile;

    /// <summary>
    /// The LCPDFR settings class containing all settings lcpdfr can read from an ini file.
    /// </summary>
    internal static class Settings
    {
        /// <summary>
        /// The default path of the settings file.
        /// </summary>
        public const string SettingsFilePath = "lcpdfr\\LCPDFR.ini";

        /// <summary>
        /// The settings file.
        /// </summary>
        private static SettingsFile settingsFile;

        /// <summary>
        /// Gets the additional cop car models.
        /// </summary>
        public static CModelInfo[] AdditionalCopCarModels { get; private set; }

        /// <summary>
        /// Gets a value indicating whether roadblocks are allowed.
        /// </summary>
        public static bool AllowRoadblocks { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the all text mode is enabled that will dub almost all audio.
        /// </summary>
        public static bool AllTextModeEnabled { get; private set; }

        /// <summary>
        /// Gets a value indicating whether no controller input is allowed.
        /// </summary>
        public static bool AlwaysForceKeyboardInput { get; private set; }

        /// <summary>
        /// Gets the maximum number of ambient scenarios running at the same time.
        /// </summary>
        public static int AmbientScenariosMaximum { get; private set; }

        /// <summary>
        /// Gets the multiplier for ambient scenarios. The higher the more likely a new scenario is to start.
        /// </summary>
        public static int AmbientScenariosMultiplier { get; private set; }

        /// <summary>
        /// Gets whether autosaving is enabled.
        /// </summary>
        public static bool AutoSaveEnabled { get; private set; }

        /// <summary>
        /// Gets the maximum time in seconds before a new callout is created.
        /// </summary>
        public static int CalloutMaximumSecondsBeforeNewCallout { get; private set; }

        /// <summary>
        /// Gets the minimum time in seconds before a new callout can be created.
        /// </summary>
        public static int CalloutMinimumSecondsBeforeNewCallout { get; private set; }

        /// <summary>
        /// Gets a value indicating whether callouts are enabled or not.
        /// </summary>
        public static bool CalloutsEnabled { get; private set; }

        /// <summary>
        /// Gets the multiplier for callouts. 0 = no callouts, 1000 = instant
        /// </summary>
        public static int CalloutsMultiplier { get; private set; }

        /// <summary>
        /// Gets the name of the police department (e.g. for police computer) default is obviously Liberty City Police Department
        /// </summary>
        public static string DepartmentName { get; private set; }

        /// <summary>
        /// Gets the short name of the police department (e.g. for police computer) default is obviously LCPD
        /// </summary>
        public static string DepartmentShortName { get; private set; }

        /// <summary>
        /// Gets a value indicating whether boat callouts are disabled.
        /// </summary>
        public static bool DisableBoatCallouts { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the camera should not focus on world events.
        /// </summary>
        public static bool DisableCameraFocusOnWorldEvents { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the suspect transport cutscene should not be played.
        /// </summary>
        public static bool DisableSuspectTransportCutscene { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the random police chatter should be disabled.
        /// </summary>
        public static bool DisableRandomPoliceChatter { get; private set; }

        /// <summary>
        /// Gets a value indicating whether annihilator helicopters should always spawn.
        /// </summary>
        public static bool ForceAnnihilatorHelicopter { get; private set; }

        /// <summary>
        /// Gets a value indicating whether ambient on foot chases created by IV should not be used as ambient events.
        /// </summary>
        public static bool ForceDisableUseFootChasesAsEvent { get; private set; }

        /// <summary>
        /// Gets a value indicating whether boats  should only use pre-gathered static positions for spawning.
        /// </summary>
        public static bool ForceStaticPositionForBoats { get; private set; }

        /// <summary>
        /// Gets a value indicating whether boat backup spawning should only use pre-gathered static positions.
        /// </summary>
        public static bool ForceStaticPositionForBoatBackup { get; private set; }

        /// <summary>
        /// Gets a value indicating whether hardcore is enabled or not
        /// </summary>
        public static bool HardcoreEnabled { get; private set; }

        /// <summary>
        /// Gets the language code.
        /// </summary>
        public static string Language { get; private set; }

        /// <summary>
        /// Gets the maximum number of cop cars allowed in a pursuit.
        /// </summary>
        public static int MaximumCopCarsInPursuit { get; private set; }

        /// <summary>
        /// Gets the maximum number of cops allowed in a pursuit.
        /// </summary>
        public static int MaximumCopsInPursuit { get; private set; }

        /// <summary>
        /// Gets the port to use when joining games.
        /// </summary>
        public static int MultiplayerClientPort { get; private set; }

        /// <summary>
        /// Gets the port to use when setting up a multiplayer game.
        /// </summary>
        public static int MultiplayerHostPort { get; private set; }

        /// <summary>
        /// Gets the API key of the user to access the LCPDFR.
        /// </summary>
        public static string NetworkAPIKey { get; private set; }

        /// <summary>
        /// Gets a value indicating whether all models used should be preloaded.
        /// </summary>
        public static bool PreloadAllModels { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the holstered taser should be shown.
        /// </summary>
        public static bool ShowHolsteredTaser { get; private set; }

        /// <summary>
        /// Gets all models that can be used as suspect transporters.
        /// </summary>
        public static CModelInfo[] SuspectTransporterModels { get; private set; }

        /// <summary>
        /// Gets the weapons that should be placed in a vehicle trunk.
        /// </summary>
        public static Weapon[] VehicleTrunkWeapons { get; private set; }

        /// <summary>
        /// Gets a value indicating whether web services such as updating the user data are enabled.
        /// </summary>
        public static bool WebServicesEnabled { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not you are using the vDH police station mod.
        /// </summary>
        public static bool UsingPoliceStationMod { get; private set; }

        public static bool SirenStrobeLights { get; private set; }

        public static string WebServicesConfiguration { get; private set; }

        /// <summary>
        /// Initializes static members of the <see cref="Settings"/> class.
        /// </summary>
        public static void Initialize()
        {
            Log.Debug("Reading...", "Settings");
            string path = Path.Combine(Application.StartupPath, SettingsFilePath);
            settingsFile = new SettingsFile(path);

            ReadSettings();
            Log.Debug("Done", "Settings");
        }

        /// <summary>
        /// Gets the controller key for <paramref name="name"/>.
        /// </summary>
        /// <param name="name">Name of the key.</param>
        /// <returns>The key.</returns>
        public static EGameKey GetControllerKey(string name)
        {
            return settingsFile.GetValue("KeybindingsController", name, EGameKey.None);
        }

        /// <summary>
        /// Gets the controller key for <paramref name="name"/>.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="defaultKey">
        /// The default key.
        /// </param>
        /// <returns>
        /// The key.
        /// </returns>
        public static GamepadButtonFlags GetControllerKey(string name, GamepadButtonFlags defaultKey)
        {
            return settingsFile.GetValue("KeybindingsController", name, defaultKey);
        }

        /// <summary>
        /// Gets the key for <paramref name="name"/>.
        /// </summary>
        /// <param name="name">Name of the key.</param>
        /// <returns>The key.</returns>
        public static Keys GetKey(string name)
        {
            return settingsFile.GetValue("Keybindings", name, Keys.None);
        }

        /// <summary>
        /// Gets the key for <paramref name="name"/>,
        /// </summary>
        /// <param name="name">Name of the key.</param>
        /// <param name="defaultKey">The default key.</param>
        /// <returns>The key.</returns>
        public static Keys GetKey(string name, Keys defaultKey)
        {
            return settingsFile.GetValue("Keybindings", name, defaultKey);
        }

        /// <summary>
        /// Gets the value in <paramref name="sectionName"/> called <paramref name="valueName"/> and returns it as <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to return the value in.</typeparam>
        /// <param name="sectionName">The section the value is in.</param>
        /// <param name="valueName">The name of the value.</param>
        /// <param name="defaultValue">Optional: Default value</param>
        /// <returns>The value with the given name.</returns>
        public static T GetValue<T>(string sectionName, string valueName, T defaultValue = default(T))
        {
            return settingsFile.GetValue(sectionName, valueName, defaultValue);
        }

        /// <summary>
        /// Writes <paramref name="value"/> in <paramref name="sectionName"/>. Adds if non-existent.
        /// </summary>
        /// <param name="sectionName">The section the value is in.</param>
        /// <param name="valueName">The name of the value.</param>
        /// <param name="value">The value.</param>
        /// <returns>True if value has been written successfully.</returns>
        public static bool WriteValue(string sectionName, string valueName, string value)
        {
            return settingsFile.WriteValue(sectionName, valueName, value);
        }

        /// <summary>
        /// Writes the default settings to the file.
        /// </summary>
        public static void WriteDefaultIniSettings()
        {
            if (settingsFile.Exists())
            {
                File.Delete(SettingsFilePath);
            }

            Log.Info("WriteDefaultIniSettings: Creating configuration file", "LCPDFRSettings");
            StreamWriter streamWriter = File.CreateText(SettingsFilePath);
            streamWriter.Close();
            streamWriter.Dispose();

            if (!settingsFile.Exists())
            {
                Log.Error("WriteDefaultIniSettings: Failed to create configuration file. Missing permissions?", "LCPDFRSettings");
                return;
            }

            // Use fake settings file to obtain default values
            SettingsFile realSettingsFile = settingsFile;
            SettingsFile tempSettingsFile = new SettingsFile("lcpdfr\\temp_settings.ini");
            settingsFile = tempSettingsFile;

            // Read default values
            ReadSettings();

            string models = AdditionalCopCarModels.Aggregate(string.Empty, (current, modelInfo) => current + (modelInfo.Name + ";"));
            realSettingsFile.WriteValue("Main", "AdditionalCopCarModels", models);
            models = SuspectTransporterModels.Aggregate(string.Empty, (current, modelInfo) => current + (modelInfo.Name + ";"));
            realSettingsFile.WriteValue("Main", "SuspectTransporterModels", models);
            models = VehicleTrunkWeapons.Aggregate(string.Empty, (current, weapon) => current + (weapon.ToString() + ";"));
            realSettingsFile.WriteValue("Main", "VehicleTrunkWeapons", models);
            realSettingsFile.WriteValue("Main", "AllTextMode", AllTextModeEnabled.ToString());
            realSettingsFile.WriteValue("Main", "AlwaysForceKeyboardInput", AlwaysForceKeyboardInput.ToString());
            realSettingsFile.WriteValue("Main", "AmbientScenariosMaximum", AmbientScenariosMaximum.ToString());
            realSettingsFile.WriteValue("Main", "AmbientScenariosMultiplier", AmbientScenariosMultiplier.ToString());
            realSettingsFile.WriteValue("Main", "AutoSaveEnabled", AutoSaveEnabled.ToString());
            realSettingsFile.WriteValue("Main", "DepartmentName", DepartmentName);
            realSettingsFile.WriteValue("Main", "DepartmentShortName", DepartmentShortName);
            realSettingsFile.WriteValue("Main", "DisableCameraFocusOnWorldEvents", DisableCameraFocusOnWorldEvents.ToString());
            realSettingsFile.WriteValue("Main", "DisableSuspectTransportCutscene", DisableSuspectTransportCutscene.ToString());
            realSettingsFile.WriteValue("Main", "DisableRandomPoliceChatter", DisableRandomPoliceChatter.ToString());
            realSettingsFile.WriteValue("Main", "Language", Language);
            realSettingsFile.WriteValue("Main", "PreloadAllModels", PreloadAllModels.ToString());
            realSettingsFile.WriteValue("Main", "UsingPoliceStationMod", UsingPoliceStationMod.ToString());
            realSettingsFile.WriteValue("Main", "IsFirstStart", "True");
            realSettingsFile.WriteValue("Callouts", "DisableBoatCallouts", DisableBoatCallouts.ToString());
            realSettingsFile.WriteValue("Callouts", "ForceStaticPositionForBoats", ForceStaticPositionForBoats.ToString());
            realSettingsFile.WriteValue("Callouts", "MaximumSecondsBeforeNewCallout", CalloutMaximumSecondsBeforeNewCallout.ToString());
            realSettingsFile.WriteValue("Callouts", "MinimumSecondsBeforeNewCallout", CalloutMinimumSecondsBeforeNewCallout.ToString());
            realSettingsFile.WriteValue("Callouts", "Enabled", CalloutsEnabled.ToString());
            realSettingsFile.WriteValue("Callouts", "Multiplier", CalloutsMultiplier.ToString());
            realSettingsFile.WriteValue("Pursuits", "AllowRoadblocks", AllowRoadblocks.ToString());
            realSettingsFile.WriteValue("Pursuits", "ForceAnnihilatorHelicopter", ForceAnnihilatorHelicopter.ToString());
            realSettingsFile.WriteValue("Pursuits", "MaximumCopCarsInPursuit", MaximumCopCarsInPursuit.ToString());
            realSettingsFile.WriteValue("Pursuits", "MaximumCopsInPursuit", MaximumCopsInPursuit.ToString());
            realSettingsFile.WriteValue("Pursuits", "ForceDisableUseFootChasesAsEvent", ForceDisableUseFootChasesAsEvent.ToString());
            realSettingsFile.WriteValue("Pursuits", "ForceStaticPositionForBoatBackup", ForceStaticPositionForBoatBackup.ToString());
            realSettingsFile.WriteValue("Pursuits", "SirenStrobeLights", SirenStrobeLights.ToString());
            realSettingsFile.WriteValue("Hardcore", "Enabled", HardcoreEnabled.ToString());
            realSettingsFile.WriteValue("Networking", "ClientPort", MultiplayerClientPort.ToString());
            realSettingsFile.WriteValue("Networking", "HostPort", MultiplayerHostPort.ToString());
            realSettingsFile.WriteValue("Networking", "APIKey", NetworkAPIKey.ToString());
            realSettingsFile.WriteValue("Taser", "ShowHolsteredTaserOnPlayer", ShowHolsteredTaser.ToString());
            realSettingsFile.WriteValue("Networking", "ServicesEnabled", WebServicesEnabled.ToString());
            realSettingsFile.WriteValue("Networking", "ServicesConfiguration", WebServicesConfiguration.ToString());

            settingsFile = realSettingsFile;
            Log.Info("WriteDefaultIniSettings: Set to default configuration", "LCPDFRSettings");
        }

        /// <summary>
        /// Reads all settings.
        /// </summary>
        private static void ReadSettings()
        {
            if (!settingsFile.Exists())
            {
                Log.Warning("ReadSettings: No settings file found: " + settingsFile.Path + ". Using default settings.", "LCPDFRSettings");
            }
            else
            {
                settingsFile.Read();
            }

            // Read all settings. Note: When providing a default value the compiler knows the return type, thus no <type> is needed in front of the call

            // Read additional cop car models LCPDFR should use. Stored in format: MODEL;MODEL;MODEL
            string models = settingsFile.GetValue("Main", "AdditionalCopCarModels", string.Empty);
            Regex regex = new Regex(";");
            string[] vehicleModels = regex.Split(models);
            List<CModelInfo> modelInfos = new List<CModelInfo>(); 
            foreach (string vehicleModel in vehicleModels)
            {
                Model model = new Model(vehicleModel);
                if (model.Hash != 0 && model.isVehicle)
                {
                    CModelInfo modelInfo = new CModelInfo(vehicleModel, (uint)model.Hash, EModelFlags.IsVehicle | EModelFlags.IsCopCar | EModelFlags.IsPolice | EModelFlags.IsEmergencyServicesVehicle);
                    Log.Info("ReadSettings: Adding " + modelInfo.Name, "Settings");
                    Engine.Main.ModelManager.AddModelInfoData(modelInfo);
                    modelInfos.Add(modelInfo);
                }
                else
                {
                    Log.Warning("ReadSettings: Invalid entry " + vehicleModel, "Settings");
                }
            }

            AdditionalCopCarModels = modelInfos.ToArray();

            // Read suspect transporter models
            models = settingsFile.GetValue("Main", "SuspectTransporterModels", "POLICE;POLICE2;PSTOCKADE");
            regex = new Regex(";");
            vehicleModels = regex.Split(models);
            modelInfos = new List<CModelInfo>();
            foreach (string vehicleModel in vehicleModels)
            {
                CModelInfo modelInfo = Engine.Main.ModelManager.GetModelInfoByName(vehicleModel);
                if (modelInfo != null)
                {
                    // Prevent bitches from using weird models
                    if (modelInfo.ModelFlags.HasFlag(EModelFlags.IsCopCar) && modelInfo.ModelFlags.HasFlag(EModelFlags.IsVehicle)
                        && (modelInfo.ModelFlags.HasFlag(EModelFlags.IsNormalUnit) || modelInfo.ModelFlags.HasFlag(EModelFlags.IsNoose) || modelInfo.Name == "PSTOCKADE"))
                    {
                        modelInfo.AddFlags(EModelFlags.IsSuspectTransporter);
                        Log.Info("ReadSettings: Suspect transporter " + modelInfo.Name, "Settings");
                        modelInfos.Add(modelInfo);
                    }
                    else
                    {
                        Log.Warning("ReadSettings: Invalid option: " + vehicleModel, "Settings");
                    }
                }
                else
                {
                    Log.Warning("ReadSettings: Invalid entry " + vehicleModel, "Settings");
                }
            }

            SuspectTransporterModels = modelInfos.ToArray();

            // Read weapons for trunk
            string weaponsString = settingsFile.GetValue("Main", "VehicleTrunkWeapons", "Shotgun_Baretta");
            string[] weapons = regex.Split(weaponsString);
            List<Weapon> weaponsList = new List<Weapon>();
            foreach (string weapon in weapons)
            {
                Weapon w = Weapon.Unarmed;
                if (Enum.TryParse(weapon, out w))
                {
                    weaponsList.Add(w);
                }
            }

            VehicleTrunkWeapons = weaponsList.ToArray();
            DepartmentName = settingsFile.GetValue("Main", "DepartmentName", "Liberty City Police Department");
            DepartmentShortName = settingsFile.GetValue("Main", "DepartmentShortName", "LCPD");
            AllowRoadblocks = settingsFile.GetValue("Pursuits", "AllowRoadblocks", true);
            AllTextModeEnabled = settingsFile.GetValue("Main", "AllTextMode", false);
            AlwaysForceKeyboardInput = settingsFile.GetValue("Main", "AlwaysForceKeyboardInput", false);
            AmbientScenariosMaximum = settingsFile.GetValue("Main", "AmbientScenariosMaximum", 2);
            AmbientScenariosMultiplier = settingsFile.GetValue("Main", "AmbientScenariosMultiplier", 250);
            AutoSaveEnabled = settingsFile.GetValue("Main", "AutoSaveEnabled", false);
            DisableCameraFocusOnWorldEvents = settingsFile.GetValue("Main", "DisableCameraFocusOnWorldEvents", false);
            DisableSuspectTransportCutscene = settingsFile.GetValue("Main", "DisableSuspectTransportCutscene", false);
            DisableRandomPoliceChatter = settingsFile.GetValue("Main", "DisableRandomPoliceChatter", false);
            PreloadAllModels = settingsFile.GetValue("Main", "PreloadAllModels", false);
            DisableBoatCallouts = settingsFile.GetValue("Callouts", "DisableBoatCallouts", false);
            ForceStaticPositionForBoats = settingsFile.GetValue("Callouts", "ForceStaticPositionForBoats", false);
            CalloutMaximumSecondsBeforeNewCallout = settingsFile.GetValue("Callouts", "MaximumSecondsBeforeNewCallout", 600);
            CalloutMinimumSecondsBeforeNewCallout = settingsFile.GetValue("Callouts", "MinimumSecondsBeforeNewCallout", 60);
            CalloutsEnabled = settingsFile.GetValue("Callouts", "Enabled", true);
            CalloutsMultiplier = settingsFile.GetValue("Callouts", "Multiplier", 250);
            ForceAnnihilatorHelicopter = settingsFile.GetValue("Pursuits", "ForceAnnihilatorHelicopter", false);
            ForceDisableUseFootChasesAsEvent = settingsFile.GetValue("Pursuits", "ForceDisableUseFootChasesAsEvent", false);
            ForceStaticPositionForBoatBackup = settingsFile.GetValue("Pursuits", "ForceStaticPositionForBoatBackup", false);
            HardcoreEnabled = settingsFile.GetValue("Hardcore", "Enabled", false);
            Language = settingsFile.GetValue("Main", "Language", "en-US");
            MaximumCopCarsInPursuit = settingsFile.GetValue("Pursuits", "MaximumCopCarsInPursuit", 5);
            MaximumCopsInPursuit = settingsFile.GetValue("Pursuits", "MaximumCopsInPursuit", 20);
            MultiplayerClientPort = settingsFile.GetValue("Networking", "ClientPort", 1337);
            MultiplayerHostPort = settingsFile.GetValue("Networking", "HostPort", 1337);
            NetworkAPIKey = settingsFile.GetValue<string>("Networking", "APIKey", string.Empty);
            WebServicesConfiguration = settingsFile.GetValue<string>("Networking", "ServicesConfiguration", "public");
            ShowHolsteredTaser = settingsFile.GetValue("Taser", "ShowHolsteredTaserOnPlayer", true);
            WebServicesEnabled = settingsFile.GetValue("Networking", "ServicesEnabled", true);
            UsingPoliceStationMod = settingsFile.GetValue("Main", "UsingPoliceStationMod", false);
            SirenStrobeLights = settingsFile.GetValue("Pursuits", "SirenStrobeLights", false);
        }
    }
}