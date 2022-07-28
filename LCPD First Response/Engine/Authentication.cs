namespace LCPD_First_Response.Engine
{
    using System;
    using System.IO;
    using System.Management;
    using System.Net.NetworkInformation;
    using System.Text;
    using System.Threading;

    using LCPD_First_Response.Engine.IO;
    using LCPD_First_Response.Engine.Scripting.Events;
    using LCPD_First_Response.Engine.Timers;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Authentication class which handles authentication for tester build and maybe someday special donator features.
    /// </summary>
    internal class Authentication
    {
        /// <summary>
        /// Value indicating whether a tester build or not.
        /// </summary>
        internal const bool IsTesterBuild = false;

        /// <summary>
        /// Value indicating whether strong protection (in doubt don't trust user) schemes are used. Only enable for highly confidential stuff as it may yield many false-positives.
        /// </summary>
        internal const bool StrongProtection = false;

        /// <summary>
        /// Value indicating whether sensitive data will be logged. ONLY USE THIS FOR PRIVATE BUILDS!
        /// </summary>
        internal const bool SensitiveLogging = false;

        /// <summary>
        /// The path where the offline user information data is stored that is used to authenticate the user when there's no internet connection.
        /// </summary>
        internal const string OfflineUserInformationPath = "LCPDFRLease";

        /// <summary>
        /// Thread used for asynchronous connecting.
        /// </summary>
        private Thread connectionThread;

        /// <summary>
        /// Whether offline mode is used.
        /// </summary>
        private bool offlineMode;

        /// <summary>
        /// Initializes a new instance of the <see cref="Authentication"/> class.
        /// </summary>
        public Authentication()
        {
            // Initialize default user data
            this.Userdata = UserData.Default;

            this.CanStart = true;

            // Initialize network connection without blocking the current thread
            this.InitializeConnection(true);

            EventNetworkConnectionEstablished.EventRaised += new EventNetworkConnectionEstablished.EventRaisedEventHandler(this.EventNetworkConnectionEstablished_EventRaised);
            EventNetworkConnectionFailed.EventRaised += this.EventNetworkConnectionFailed_EventRaised;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Authentication"/> class. Empty constructor for unit testing.
        /// </summary>
        /// <param name="doNothing">
        /// Not used.
        /// </param>
        public Authentication(bool doNothing)
        {      
        }

        /// <summary>
        /// Gets the API key of the user.
        /// </summary>
        public string APIKey { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the user is authenticated.
        /// </summary>
        public bool Authenticated { get; private set; }

        /// <summary>
        /// Gets a value indicating whether LCPDFR is allowed to start.
        /// </summary>
        public bool CanStart { get; private set; }

        /// <summary>
        /// Gets the user data.
        /// </summary>
        public UserData Userdata { get; private set; }

        /// <summary>
        /// Gets the hardware ID.
        /// </summary>
        private static string hardwareID;

        /// <summary>
        /// Gets the hardware ID.
        /// </summary>
        /// <returns>The hardware ID.</returns>
        public static string GetHardwareID()
        {
            if (hardwareID == null)
            {
                hardwareID = GenerateHardwareIDHash();
            }

            return hardwareID;
        }

        /// <summary>
        /// Generates the local hardware ID hash.
        /// </summary>
        /// <returns>The hardware ID hash.</returns>
        private static string GenerateHardwareIDHash()
        {
            // Get CPU ID
            string cpuInfo = string.Empty;
            ManagementClass mc = new ManagementClass("win32_processor");
            ManagementObjectCollection moc = mc.GetInstances();
            foreach (ManagementObject mo in moc)
            {
                if (cpuInfo == string.Empty)
                {
                    // Get only the first CPU's ID
                    cpuInfo = mo.Properties["processorID"].Value.ToString();
                    break;
                }
            }

            LogSensitive("GenerateHardwareIDHash: CPU ID: " + cpuInfo);

            // Get harddisk ID
            string drive = Path.GetPathRoot(Environment.SystemDirectory).Substring(0, 1);
            ManagementObject dsk = new ManagementObject(@"win32_logicaldisk.deviceid=""" + drive + @":""");
            dsk.Get();
            string volumeSerial = dsk["VolumeSerialNumber"].ToString();
            LogSensitive("GenerateHardwareIDHash: HDD ID: " + volumeSerial);

            // Get motherboard id
            string uuid = string.Empty;
            mc = new ManagementClass("Win32_ComputerSystemProduct");
            moc = mc.GetInstances();
            foreach (ManagementObject mo in moc)
            {
                uuid = mo.Properties["UUID"].Value.ToString();
                break;
            }

            LogSensitive("GenerateHardwareIDHash: Motherboard ID: " + uuid);

            string fullString = cpuInfo + volumeSerial + uuid;
            LogSensitive("GenerateHardwareIDHash: Hash string: " + fullString);

            byte[] buffer = Encoding.UTF8.GetBytes(fullString);
            string hash = BitConverter.ToString(Encryption.HashDataSHA1(buffer));
            hash = hash.Replace("-", string.Empty);
            LogSensitive("GenerateHardwareIDHash: Hash: " + hash);
            return hash;
        }

        /// <summary>
        /// Saves the <paramref name="userInformation"/> retrieved from the server.
        /// </summary>
        /// <param name="userInformation">The user information.</param>
        /// <returns>True on success, false if not.</returns>
        private static bool SaveUserInformationOffline(string userInformation)
        {
            // Add some data to the user information
            long timeStamp = DateTime.Now.ToFileTimeUtc();
            byte[] timeStampBytes = BitConverter.GetBytes(timeStamp);

            // Encrypt user information
            byte[] data = Encoding.UTF8.GetBytes(userInformation);
            byte[] encryptedData = Encryption.Encrypt(data, "DataPassword1PleaseChange");

            // Hash encrypted data
            byte[] userInformationHash = Encryption.HashDataSHA1(encryptedData);

            // Store data. Layout:
            // Length of timestamp
            // Length of hash
            // Length of user information
            // timestamp
            // hash
            // user information

            // Get lengths
            byte[] lengthTimeStamp = BitConverter.GetBytes(timeStampBytes.Length);
            byte[] lengthHash = BitConverter.GetBytes(userInformationHash.Length);
            byte[] lengthData = BitConverter.GetBytes(encryptedData.Length);
            byte[] lengthArray = new byte[lengthTimeStamp.Length + lengthHash.Length + lengthData.Length];
            Buffer.BlockCopy(lengthTimeStamp, 0, lengthArray, 0, lengthTimeStamp.Length);
            Buffer.BlockCopy(lengthHash, 0, lengthArray, lengthTimeStamp.Length, lengthHash.Length);
            Buffer.BlockCopy(lengthData, 0, lengthArray, lengthTimeStamp.Length + lengthHash.Length, lengthData.Length);

            // Merge all data
            byte[] mergedBytes = new byte[lengthArray.Length + timeStampBytes.Length + userInformationHash.Length + encryptedData.Length];
            Buffer.BlockCopy(lengthArray, 0, mergedBytes, 0, lengthArray.Length);
            Buffer.BlockCopy(timeStampBytes, 0, mergedBytes, lengthArray.Length, timeStampBytes.Length);
            Buffer.BlockCopy(userInformationHash, 0, mergedBytes, lengthArray.Length + timeStampBytes.Length, userInformationHash.Length);
            Buffer.BlockCopy(encryptedData, 0, mergedBytes, lengthArray.Length + timeStampBytes.Length + userInformationHash.Length, encryptedData.Length);

            // Encrypt data again
            encryptedData = Encryption.Encrypt(mergedBytes, "DataPassword2PleaseChange");

            // Save data
            using (FileStream fs = File.Create(OfflineUserInformationPath, encryptedData.Length))
            {
                fs.Write(encryptedData, 0, encryptedData.Length);
            }

            // Now that the file has been saved, hash the file and write the hash to the registry to validate it later
            byte[] encryptedDataHash = Encryption.HashDataSHA1(encryptedData);

            return true;
        }

        /// <summary>
        /// Loads the user information that have been saved before.
        /// </summary>
        /// <returns>The user information.</returns>
        private static string LoadUserInformationOffline()
        {
            // Read data
            byte[] data = File.ReadAllBytes(OfflineUserInformationPath);

            // Decrypt data first stage
            byte[] decryptedData = Encryption.Decrypt(data, "DataPassword2PleaseChange");

            // Read data. Layout:
            // Length of timestamp
            // Length of hash
            // Length of user information
            // timestamp
            // hash
            // user information
            byte[] lengthTimeStampBytes = ReadBytesFromArray(decryptedData, 0, 4);
            byte[] lengthHashBytes = ReadBytesFromArray(decryptedData, 4, 4);
            byte[] lengthDataBytes = ReadBytesFromArray(decryptedData, 8, 4);

            int lengthTimeStamp = BitConverter.ToInt32(lengthTimeStampBytes, 0);
            int lengthHash = BitConverter.ToInt32(lengthHashBytes, 0);
            int lengthData = BitConverter.ToInt32(lengthDataBytes, 0);

            // Read bytes
            byte[] timeStampBytes = ReadBytesFromArray(decryptedData, 12, lengthTimeStamp);
            byte[] hashBytes = ReadBytesFromArray(decryptedData, 12 + lengthTimeStamp, lengthHash);
            byte[] dataBytes = ReadBytesFromArray(decryptedData, 12 + lengthTimeStamp + lengthHash, lengthData);

            // Convert data
            long timeStamp = BitConverter.ToInt64(timeStampBytes, 0);
            string userInformationHash = BitConverter.ToString(hashBytes);

            // Hash the data and compare the hashes to check whether data has been modified
            byte[] dataHash = Encryption.HashDataSHA1(dataBytes);
            if (BitConverter.ToString(dataHash) == BitConverter.ToString(hashBytes))
            {
                LogSensitive("LoadUserInformationOffline: Hashes equal");

                // Decrypt data
                decryptedData = Encryption.Decrypt(dataBytes, "DataPassword1PleaseChange");
                return Encoding.UTF8.GetString(decryptedData);
            }
            else
            {
                // Hashes are not equal
                Log.Error("Authentication: Error 0x002", "Authentication");
            }

            return string.Empty;
        }

        /// <summary>
        /// Reads <paramref name="count"/> bytes from <paramref name="array"/> starting at <paramref name="offset"/>.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The length.</param>
        /// <returns>The bytes read.</returns>
        private static byte[] ReadBytesFromArray(Array array, int offset, int count)
        {
            byte[] tempArray = new byte[count];
            Buffer.BlockCopy(array, offset, tempArray, 0, count);
            return tempArray;
        }

        /// <summary>
        /// Checks whether there is a chance a web emulator is running.
        /// </summary>
        /// <returns>True if there is, false if not.</returns>
        private static bool IsWebEmulatorRunning()
        {
            int port = 80;
            bool isAvailable = true;

            try
            {
                // Evaluate current system tcp connections. This is the same information provided
                // by the netstat command line application, just in .Net strongly-typed object
                // form.  We will look through the list, and if our port we would like to use
                // in our TcpClient is occupied, we will set isAvailable to false.
                IPGlobalProperties globalProperties = IPGlobalProperties.GetIPGlobalProperties();
                TcpConnectionInformation[] tcpConnInfoArray = globalProperties.GetActiveTcpConnections();

                foreach (TcpConnectionInformation tcpi in tcpConnInfoArray)
                {
                    if (tcpi.LocalEndPoint.Port == port)
                    {
                        isAvailable = false;
                        break;
                    }
                }
            }
            catch (NetworkInformationException networkInformationException)
            {
                Log.Error("Network error 0x0011: " + networkInformationException.Message + "(" + networkInformationException.ErrorCode + ")", "Authentication");
                if (StrongProtection)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return !isAvailable;
        }

        /// <summary>
        /// Logs <paramref name="text"/>.
        /// </summary>
        /// <param name="text">The text.</param>
        private static void LogSensitive(string text)
        {
            if (SensitiveLogging)
            {
                Log.Debug(text, "Authentication");
            }
        }

        /// <summary>
        /// Fired when a connection to the LCPDFR server has been established.
        /// </summary>
        /// <param name="event">The event.</param>
        private void EventNetworkConnectionEstablished_EventRaised(EventNetworkConnectionEstablished @event)
        {
            // Peform authentication in a thread
            Action performAuth = new Action(
                delegate
                    {
                        if (this.PerformAuthentication())
                        {
                            Log.Info("Authentication: Logged in as \"" + this.Userdata.Username + "\"", "Authentication");
                        }
                        else
                        {
                            Log.Info("Authentication: Failed to authenticate", "Authentication");
                        }
                    });

            Thread t = new Thread(new ThreadStart(performAuth));
            t.IsBackground = true;
            t.Start();
        }

        /// <summary>
        /// Fired when no connection to the LCPDFR server could be established.
        /// </summary>
        /// <param name="event">The event.</param>
        private void EventNetworkConnectionFailed_EventRaised(EventNetworkConnectionFailed @event)
        {
            Action performAuth = new Action(
                delegate
                    {
                        this.Authenticated = this.PerformAuthentication();
                        if (this.Authenticated)
                        {
                            Log.Info("Authentication: Logged in as \"" + this.Userdata.Username + "\"", "Authentication");
                        }

                        if (this.offlineMode)
                        {
                            Log.Info("Authentication: User is in offline mode", "Authentication");
                        }
                    });

            Thread t = new Thread(new ThreadStart(performAuth));
            t.IsBackground = true;
            t.Start();
        }

        /// <summary>
        /// Gets the userdata from the raw data. Note: When hash checks or data checks fail, returns null.
        /// </summary>
        /// <param name="rawData">The raw data.</param>
        /// <returns>The user data.</returns>
        private UserData GetUserData(JObject rawData)
        {
            // Get local hardware hash
            string hash = GenerateHardwareIDHash();

            // Local vars
            bool isTester = false;
            string name = string.Empty;
            bool allowOfflineAuth = false;
            int maximumSessions = 0;
            int numberOfSessions = 0;

            if (rawData["members_display_name"] != null)
            {
                // Get the name the server sent
                string nameServer = (string)rawData["members_display_name"];
                name = nameServer;
            }
            else
            {
                // Data is corrupted, name missing
                Log.Error("Authentication: Error 0x320", "Authentication");
                return null;
            }

            return new UserData(allowOfflineAuth, false, isTester, maximumSessions, numberOfSessions, name);
        }

        /// <summary>
        /// Initializes the network connection. Can be done asynchronous to prevent the thread from being blocked.
        /// </summary>
        /// <param name="async">True if asynchronous, false if not.</param>
        private void InitializeConnection(bool async)
        {
            Action connectionCode = () => Main.LCPDFRServer.InitializeConnection();

            if (async)
            {
                this.connectionThread = new Thread(new ThreadStart(connectionCode));
                this.connectionThread.IsBackground = true;
                this.connectionThread.Start();
            }
            else
            {
                connectionCode();
            }
        }

        /// <summary>
        /// Authenticates the user.
        /// </summary>
        /// <returns>
        /// True on success, false otherwise.
        /// </returns>
        private bool PerformAuthentication()
        {
            // If server is available, authenticate via internet
            if (Main.LCPDFRServer.IsServerAvailable)
            {
                Log.Info("Authentication: Server available, using online authentication", "Authentication");
                if (this.PerformOnlineAuthentication())
                {
                    Log.Info("Authentication: Authentication successful", "Authentication");
                    return true;
                }
                else
                {
                    Log.Error("Authentication: Authentication failed", "Authentication");
                }
            }
            else
            {
                this.offlineMode = true;
                Log.Info("Authentication: Server not available, trying to authenticate offline", "Authentication");
                if (this.PerformOfflineAuthentication())
                {
                    Log.Info("Authentication: Authentication successful", "Authentication");
                    return true;
                }
                else
                {
                    Log.Error("Authentication: Authentication failed. Couldn't reach server or no valid lease available", "Authentication");
                }
            }

            return false;
        }

        /// <summary>
        /// Performs the online authentication.
        /// </summary>
        /// <returns>
        /// True if successful, false if not.
        /// </returns>
        private bool PerformOnlineAuthentication()
        {
            // Get APIKey
            string path = LCPDFR.Settings.SettingsFilePath;
            SettingsFile settingsFile = new SettingsFile(path);
            if (!settingsFile.Exists())
            {
                Log.Error("PerformOnlineAuthentication: Failed to retrieve API key. LCPDFR.ini not found", "Authentication");
                return false;
            }

            settingsFile.Read();
            string key = settingsFile.GetValue<string>("Networking", "APIKey");
            if (key == null)
            {
                Log.Error("PerformOnlineAuthentication: Failed to retrieve API key. Key not found.", "Authentication");
                return false;
            }

            // Ask server for information about this user by sending the APIKey
            this.APIKey = key;
            JObject userInformation = Main.LCPDFRServer.GetUserInformationJObject();

            // Parse data
            UserData userdata = this.GetUserData(userInformation);

            // Will only return not-null if data is fine and hashes match
            if (userdata != null)
            {
                this.Userdata = userdata;

                // Save user information so it can be used later when no internet connection is available
                if (SaveUserInformationOffline(userInformation.ToString()))
                {
                    Log.Info("PerformOnlineAuthentication: Lease renewed", "Authentication");
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Peforms the offline authentication.
        /// </summary>
        /// <returns>
        /// True if successful, false if not.
        /// </returns>
        private bool PerformOfflineAuthentication()
        {
            // Check for lease file
            if (File.Exists(OfflineUserInformationPath))
            {
                JObject userDataArray = new JObject();

                try
                {
                    // Load data in JSON format
                    string data = LoadUserInformationOffline();

                    // Convert to JSON object
                    userDataArray = JObject.Parse(data);
                }

                catch (Exception)
                {
                    Log.Info("PerformOfflineAuthentication: Failed to parse data", "Authentication");
                }

                // Parse data
                UserData userdata = this.GetUserData(userDataArray);

                // Will only return not-null if data is fine and hashes match
                if (userdata != null)
                {
                    // Set userdata
                    this.Userdata = userdata;
                    LogSensitive("PerformOfflineAuthentication: Sessions already played: " + this.Userdata.NumberOfOfflineSessionsPlayed);
                    LogSensitive("PerformOfflineAuthentication: Max sessions: " + this.Userdata.MaximumNumberOfOfflineSessions);

                    // Increase sessions played count
                    userDataArray["offline_sessions_played"] = (this.Userdata.NumberOfOfflineSessionsPlayed + 1).ToString();

                    // Save data
                    SaveUserInformationOffline(userDataArray.ToString());
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Stores the data of the user.
        /// </summary>
        internal class UserData
        {
            /// <summary>
            /// A default user data instance.
            /// </summary>
            public static UserData Default = new UserData(false, false, false, 0, 0, string.Empty);

            /// <summary>
            /// Initializes a new instance of the <see cref="UserData"/> class.
            /// </summary>
            /// <param name="allowOfflineAuth">
            /// Whether user can be authenticated offline.
            /// </param>
            /// <param name="isSupporter">
            /// Whether user is supporter.
            /// </param>
            /// <param name="isTester">
            /// Whether user is tester.
            /// </param>
            /// <param name="maxNumberOfSessions">
            /// The maximum number of sessions.
            /// </param>
            /// <param name="numberOfSessions">
            /// The number of sessions.
            /// </param>
            /// <param name="username">
            /// The username.
            /// </param>
            public UserData(bool allowOfflineAuth, bool isSupporter, bool isTester, int maxNumberOfSessions, int numberOfSessions, string username)
            {
                this.AllowOfflineAuthentication = allowOfflineAuth;
                this.IsSupporter = isSupporter;
                this.IsTester = isTester;
                this.MaximumNumberOfOfflineSessions = maxNumberOfSessions;
                this.NumberOfOfflineSessionsPlayed = numberOfSessions;
                this.Username = username;
            }

            /// <summary>
            /// Gets a value indicating whether this user can be authenticated offline.
            /// </summary>
            public bool AllowOfflineAuthentication { get; private set; }

            /// <summary>
            /// Gets a value indicating whether the user is a supporter.
            /// </summary>
            public bool IsSupporter { get; private set; }

            /// <summary>
            /// Gets a value indicating whether the user is a tester.
            /// </summary>
            public bool IsTester { get; private set; }

            /// <summary>
            /// Gets the maximum number of sessions a user can run offline.
            /// </summary>
            public int MaximumNumberOfOfflineSessions { get; private set; }

            /// <summary>
            /// Gets the number of offline sessions played by the user.
            /// </summary>
            public int NumberOfOfflineSessionsPlayed { get; private set; }

            /// <summary>
            /// Gets the username.
            /// </summary>
            public string Username { get; private set; }
        }
    }
}