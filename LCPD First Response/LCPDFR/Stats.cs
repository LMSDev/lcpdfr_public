namespace LCPD_First_Response.LCPDFR
{
    using System;
    using System.IO;
    using System.Management;
    using System.Threading;

    using GTA;

    using LCPD_First_Response.Engine;

    /// <summary>
    /// The class holding all statistics about LCPDFR and the player.
    /// </summary>
    internal static class Stats
    {
        /// <summary>
        /// Describes the different stat types.
        /// </summary>
        internal enum EStatType
        {
            /// <summary>
            /// The number of accidental kills.
            /// </summary>
            AccidentalKills,

            /// <summary>
            /// The number of arrests made.
            /// </summary>
            Arrests,

            /// <summary>
            /// The number of backup units called.
            /// </summary>
            BackupCalled,

            /// <summary>
            /// The number of chases started.
            /// </summary>
            Chases,

            /// <summary>
            /// The number of citations issued.
            /// </summary>
            Citations,

            /// <summary>
            /// The number of helicopter backup units called.
            /// </summary>
            HelicopterBackupCalled,

            /// <summary>
            /// The number of noose backup units called.
            /// </summary>
            NooseBackupCalled,

            /// <summary>
            /// The number of officers killed.
            /// </summary>
            OfficersKilled,

            // The following are usage statistics:
            PulloversStarted,

            Frisks,

            ArrestTakenToStation,

            ArrestCalledTransporter,

            ANPREnabled,

            ANPRDisabled,

            PDEntered,

            TutorialPlayed,

            QuickActionMenuOpened,

            PoliceComputerOpened,

            VehicleTrunkOpened,

            DoorOpened,

            SuspectGrabbed,

            CalloutAccepted,

            CalloutDenied,

            TaserEquipped,

            TaserFired,

            PartnerRecruited,

            PartnerRecruitedPD,

            PartnerOrderedGoTo,

            PartnerOrderedRegroup,

            PartnerOrderedKeepPosition,

            PartnerOrderedArrest,

            AmbulanceCalled,

            FirefighterCalled,

            AreaTrafficBlocked,

            FastWalkUsed,

            PedsStopped,

            PedsSetAsWanted,

            PedsReleased,

            PedDataLookedUp,

            LethalForceAllowed,

            LethalForceDenied,

            UnitsSwitchedToActive,

            UnitsSwitchedToPassive,

            AirUnitsSwitchedToActive,

            AirUnitsSwitchedToPassive,
        }

        /// <summary>
        /// The lock object for the usage data file.
        /// </summary>
        private static object fileLock;

        /// <summary>
        /// Gets the number of accidental kills.
        /// </summary>
        public static int AccidentalKills { get; private set; }

        /// <summary>
        /// Initializes static members of the <see cref="Stats"/> class.
        /// </summary>
        public static void Initialize()
        {
            fileLock = new object();

            Thread informationThread = new Thread(CollectSystemInformation);
            informationThread.IsBackground = true;
            informationThread.Start();
        }

        /// <summary>
        /// Updates <paramref name="statType"/> by <paramref name="value"/>.
        /// </summary>
        /// <param name="statType">The stat type.</param>
        /// <param name="value">The value.</param>
        public static void UpdateStat(EStatType statType, int value)
        {
            UpdateStat(statType, value, GTA.Vector3.Zero);
        }

        /// <summary>
        /// Updates <paramref name="statType"/> by <paramref name="value"/> and reporting the <paramref name="position"/>.
        /// </summary>
        /// <param name="statType">The stat type.</param>
        /// <param name="value">The value.</param>
        /// <param name="position">The position where the stat was updated.</param>
        public static void UpdateStat(EStatType statType, int value, GTA.Vector3 position)
        {
            switch (statType)
            {
                case EStatType.AccidentalKills:
                    AccidentalKills++;
                    break;
            }

            try
            {
                WriteToStatsFile(statType, value);
            }
            catch (Exception exception)
            {
                Log.Error("Error while updating stats file: " + exception.Message, "Stats");
            }

            if (Settings.WebServicesEnabled)
            {
                UpdateServerStat(statType, value, position);
            }
        }

        /// <summary>
        /// Updates <paramref name="statType"/> on the server.
        /// </summary>
        /// <param name="statType">The stat type.</param>
        /// <param name="value">The value.</param>
        /// <param name="position">The position where the stat was updated.</param>
        private static void UpdateServerStat(EStatType statType, int value, GTA.Vector3 position)
        {
            string name = null;

            switch (statType)
            {
                case EStatType.AccidentalKills:
                    name = "accidents";
                    break;

                case EStatType.Arrests:
                    name = "arrests";
                    break;

                case EStatType.BackupCalled:
                    name = "backup";
                    break;

                case EStatType.Chases:
                    name = "chases";
                    break;

                case EStatType.Citations:
                    name = "tickets";
                    break;

                case EStatType.HelicopterBackupCalled:
                    name = "helis";
                    break;

                case EStatType.NooseBackupCalled:
                    name = "noose";
                    break;

                case EStatType.OfficersKilled:
                    name = "officersdown";
                    break;
            }

            // Update server
            if (!string.IsNullOrEmpty(name))
            {
                string append = null;
                if (position != GTA.Vector3.Zero)
                {
                    string area = Engine.Scripting.AreaHelper.GetAreaNameMeaningful(position);
                    append = "&x=" + position.X.ToString() + "&y=" + position.Y.ToString() + "&z=" + position.Z.ToString() + "&area=" + area;
                }

                Engine.Main.LCPDFRServer.UpdateServerValueAsync(name, "add", value.ToString(), append);
            }
        }

        /// <summary>
        /// Gets information about the system running to gather exact usage information.
        /// </summary>
        private static void CollectSystemInformation()
        {
            // Wait for the server to become available.
            while (!Engine.Main.LCPDFRServer.IsServerAvailable)
            {
                Thread.Sleep(5000);
            }

            lock (fileLock)
            {
                // Read data
                if (File.Exists("LCPDFR_Usage.dat"))
                {
                    // Open file and decrypt
                    byte[] data = File.ReadAllBytes("LCPDFR_Usage.dat");

                    // Decrypt data first stage
                    try
                    {
                        byte[] decryptedData = Encryption.Decrypt(data, "ion98z234rn9i8hf47hnhn)(H/()hg/(GH78g6");
                        Engine.Main.LCPDFRServer.UploadStatisticsFile(decryptedData);
                        Log.Info("CollectSystemInformation: Server stats updated", "Stats");
                    }
                    catch (Exception exception)
                    {
                        Log.Warning("CollectSystemInformation: Failed to upload stats: " + exception.Message, "Stats");
                    }
                }
            }
        }

        /// <summary>
        /// Gets a friendly string representation of the current OS.
        /// </summary>
        /// <returns>The name.</returns>
        private static string GetOSFriendlyName()
        {
            string result = string.Empty;
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem");
            foreach (ManagementObject os in searcher.Get())
            {
                result = os["Caption"].ToString();
                break;
            }
            return result;
        }

        /// <summary>
        /// Writes an update to the local stats file.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="amount">The amount.</param>
        private static void WriteToStatsFile(EStatType type, int amount)
        {
            lock (fileLock)
            {
                byte[] data;
                byte[] decryptedData = new byte[0];

                if (!File.Exists("LCPDFR_Usage.dat"))
                {
                    File.Create("LCPDFR_Usage.dat").Close();
                }
                else
                {
                    // Open file and decrypt
                    data = File.ReadAllBytes("LCPDFR_Usage.dat");

                    // Decrypt data first stage
                    try
                    {
                        decryptedData = Encryption.Decrypt(data, "ion98z234rn9i8hf47hnhn)(H/()hg/(GH78g6");
                    }
                    catch (Exception exception)
                    {
                        Log.Error("Failed to decrypt stats file: " + exception.Message, "Stats");
                    }
                    finally
                    {
                        File.Delete("LCPDFR_Usage.dat");
                    }
                }

                // Write to temp file
                File.WriteAllBytes("LCPDFR_Usage.tmp", decryptedData);

                // Get OS / Version
                string name = GetOSFriendlyName();
                string version = Main.Version;

                // Open file
                SettingsFile settingsFile = SettingsFile.Open("LCPDFR_Usage.tmp");
                settingsFile.Load();
                int value = settingsFile.GetValueInteger(type.ToString(), "Usage", 0);
                settingsFile.SetValue(type.ToString(), "Usage", value + amount);
                settingsFile.SetValue("API", "Main", Settings.NetworkAPIKey);
                settingsFile.SetValue("HardwareID", "Main", Authentication.GetHardwareID());
                settingsFile.SetValue("OS", "Main", name);
                settingsFile.SetValue("Version", "Main", version);
                settingsFile.Save();

                data = File.ReadAllBytes("LCPDFR_Usage.tmp");
                File.Delete("LCPDFR_Usage.tmp");
                byte[] encryptedData = Encryption.Encrypt(data, "ion98z234rn9i8hf47hnhn)(H/()hg/(GH78g6");

                File.WriteAllBytes("LCPDFR_Usage.dat", encryptedData);
            }
        }
    }
}