namespace LCPD_First_Response.Engine.Networking
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Net;
    using System.Reflection;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;

    using LCPD_First_Response.Engine.Networking.QueueMessageHandler;
    using LCPD_First_Response.Engine.Scripting.Events;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR;

    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Describes the connection state.
    /// </summary>
    internal enum ENetworkConnectionState
    {
        /// <summary>
        /// No state.
        /// </summary>
        None,

        /// <summary>
        /// Connection in progress.
        /// </summary>
        Pending,

        /// <summary>
        /// Connected to the internet.
        /// </summary>
        Connected,

        /// <summary>
        /// Failed to connect.
        /// </summary>
        Failed,
    }

    /// <summary>
    /// Class responsible for communicating with the LCPDFR server to e.g. authenticate users or share IPs.
    /// Some documentation on the JSON returned (including server runtime statistics) may be available here: http://v2.lcpdfr.onlineservices.g17media.com/
    /// </summary>
    internal class ServerCommunication : BaseComponent
    {
        /// <summary>
        /// The API version. Currently this is expected to always be 1 -- although this will allow for future expansion without radically changing server-side code
        /// </summary>
        public const int ApiVersion = 1;

        /// <summary>
        /// The web endpoint for the webserver.
        /// </summary>
        private string webEndpoint;

        /// <summary>
        /// The web endpoint for the update server.
        /// </summary>
        private string updateEndpoint;

        /// <summary>
        /// The hashing nonce used for sending statistics. Is only good for one stat send, after that a new nonce will be issued.
        /// </summary>
        private string sessionNonce;

        /// <summary>
        /// Lock object to make sure only one statistic update is sent at once (nonce requirement)
        /// </summary>
        private object nonceLock = new object();

        /// <summary>
        /// Timer for session renewal
        /// </summary>
        private System.Timers.Timer renewalTimer;

        /// <summary>
        /// Is a renewal in progress?
        /// </summary>
        private bool renewalInProgress;

        /// <summary>
        /// How many times has a renewal been attempted?
        /// </summary>
        private int renewalAttempts = 0;

        /// <summary>
        /// The session id lock.
        /// </summary>
        private object sessionIDLock;

        /// <summary>
        /// List of message handlers for queue events
        /// </summary>
        private List<IQueueMessageHandler> queueMessageHandlers;

        /// <summary>
        /// The session ID:
        /// </summary>
        private string sessionID;

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get { return "ServerCommunication"; }
        }

        /// <summary>
        /// Gets the connection state.
        /// </summary>
        public ENetworkConnectionState ConnectionState { get; private set; }

        /// <summary>
        /// Gets the IP address.
        /// </summary>
        public IPAddress IPAddress { get; private set; }

        /// <summary>
        /// Gets a value indicating whether an internet connection is available at all.
        /// </summary>
        public bool IsConnectedToTheInternet { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the connection to the server is available.
        /// </summary>
        public bool IsServerAvailable { get; private set; }

        /// <summary>
        /// Gets the session ID of the current client.
        /// </summary>
        public string SessionID
        {
            get
            {
                lock (this.sessionIDLock)
                {
                    return this.sessionID;
                }
            }

            private set
            {
                lock (this.sessionIDLock)
                {
                    this.sessionID = value;
                }
            }
        }

        /// <summary>
        /// Initializes the connection to the server.
        /// </summary>
        public void InitializeConnection()
        {
            this.sessionIDLock = new object();

            // On Success
            try
            {
                Log.Debug("InitializeConnection: Initializing...", this);
                this.ConnectionState = ENetworkConnectionState.Pending;

                // Check internet connection
                this.IsConnectedToTheInternet = CheckInternetConnection();
                if (this.IsConnectedToTheInternet)
                {
                    Log.Info("InitializeConnection: Internet connection available", this);

                    // Get public IP
                    if (this.GetPublicIP())
                    {
                        Log.Info("InitializeConnection: Public IP Address is: " + this.IPAddress, this);
                    }
                    else
                    {
                        this.IPAddress = IPAddress.None;
                        Log.Warning("InitializeConnection: Failed to get public IP Address", this);
                    }

                    // Connect to our server
                    try
                    {
                        // Get configuration for the masterserver from LCPDFR.com (allows us to change the location or for clustering in the future)
                        string configurationJsonString = GetWebResponse("https://www.lcpdfr.com/onlineservices/configuration.json");
                        JArray configurations = JArray.Parse(configurationJsonString);
                        foreach (JToken configuration in configurations)
                        {
                            if ((string)configuration["name"] == Settings.WebServicesConfiguration)
                            {
                                this.webEndpoint = (string)configuration["endpoint"];
                                this.updateEndpoint = (string)configuration["updateEndpoint"];
                                Log.Info(
                                    string.Format("InitializeConnection: Found configuration for the masterserver, going to use API server {0}", this.webEndpoint),
                                    this);
                                this.IsServerAvailable = true;
                            }
                        }

                        // Initalize queue message handlers.
                        this.queueMessageHandlers = new List<IQueueMessageHandler>();
                        this.queueMessageHandlers.Add(new WallMessage());
                        this.queueMessageHandlers.Add(new ReportYourLocation());

                        if (!this.IsServerAvailable)
                        {
                            Log.Warning("InitializeConnection: Unable to find a suitable configuration", this);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("InitializeConnection: Error while connecting to master server: " + ex.Message + ex.StackTrace, this);
                    }

                    if (this.IsServerAvailable)
                    {
                        // Get a session and make sure the RenewalWatchdog is started.
                        Log.Info("InitializeConnection: Establishing session with the LCPDFR server...", this);
                        Random rand = new Random();
                        int renewal = rand.Next(5000, 25000);
                        this.renewalTimer = new System.Timers.Timer(renewal);
                        this.renewalTimer.Elapsed += this.RenewalWatchdog;
                        this.GetSession(Settings.NetworkAPIKey);
                    }
                    else
                    {
                        Log.Warning("InitializeConnection: Couldn't reach server", this);
                        throw new WebException("No server connection");
                    }
                }
                else
                {
                    Log.Warning("InitializeConnection: No internet connection available", this);
                    throw new WebException("No internet connection"); 
                }
            }
            catch (Exception e)
            { 
                this.IsServerAvailable = false;
                this.ConnectionState = ENetworkConnectionState.Failed;
                Log.Warning("InitializeConnection: Failed to connect to the LCPDFR server (" + e.Message + "). Multiplayer is deactivated", this);

                // Ensure event is fired in the right thread
                DelayedCaller.Call(delegate { new EventNetworkConnectionFailed(); }, this, 1);
            }
        }

        /// <summary>
        /// Called every few seconds to renew the session to keep it alive.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void RenewalWatchdog(object sender, System.Timers.ElapsedEventArgs e)
        {
            if ((this.ConnectionState == ENetworkConnectionState.Connected || this.ConnectionState == ENetworkConnectionState.Pending)
                && !this.renewalInProgress)
            {
                this.renewalInProgress = true;

                // 3 times? Try to create a new session instead...
                if (this.renewalAttempts >= 3)
                {
                    this.ConnectionState = ENetworkConnectionState.Pending;
                    try
                    {
                        this.GetSession(Settings.NetworkAPIKey, true);
                        Log.Info("RenewalTimer: Restored connectivity with a new session", this);
                        this.ConnectionState = ENetworkConnectionState.Connected;
                        this.renewalInProgress = false;
                        this.renewalAttempts = 0;
                        return;
                    }
                    catch (Exception)
                    {
                        this.renewalInProgress = false;
                        return;
                    }
                }

                this.renewalAttempts++;

                // Try to renew session.
                bool wasSuccessful = this.RenewSession();
                if (wasSuccessful)
                {
                    // Session renewed :)
                    Log.Debug("RenewalTimer: Session was renewed.", this);
                    this.renewalInProgress = false;
                    this.renewalAttempts = 0;
                }
                else
                {
                    // Note that we failed and try later..
                    Log.Info("RenewalTimer: Failed to renew session. Will try again soon.", this);
                    this.renewalInProgress = false;
                }
            }
        }

        /// <summary>
        /// Gets a session (which is required to use any other LCPDFR masterserver capability)
        /// </summary>
        /// <param name="apikey">An optional API key, for the session to be tied to an account</param>
        /// <param name="noExtra">Specifies whether this should also set up network connected logic.</param>
        private void GetSession(string apikey, bool noExtra = false)
        {
            // Try to 
            string response =
                GetWebResponse(string.Format(
                        "http://{0}/api/getSession?apiVersion={1}&apikey={2}",
                        this.webEndpoint,
                        ApiVersion,
                        !string.IsNullOrWhiteSpace(apikey) ? apikey : string.Empty));

            JObject sessionResult = JObject.Parse(response);
            if (sessionResult["error"] != null)
            {
                throw new WebException((string)sessionResult["error"]);
            }

            // Show a warning (if any). For example, if the API key is invalid, they will be issued an anonymous session and this warning will mention that.
            if (sessionResult["warning"] != null)
            {
                string sessionWarning = (string)sessionResult["warning"];
                Log.Warning("GetSession: " + sessionWarning, this);
            }

            // Store session information.
            this.SessionID = (string)sessionResult["sessionId"];
            this.sessionNonce = (string)sessionResult["session"]["nonce"];
            Log.Info(string.Format("GetSession: Got session ID of {0} from masterserver.", this.SessionID), this);

            if (!noExtra)
            {
                this.ConnectionState = ENetworkConnectionState.Connected;
                this.renewalTimer.Start();
                DelayedCaller.Call(delegate { new EventNetworkConnectionEstablished(); }, this, 1);
            }
        }
        
        /// <summary>
        /// Processes the session queue and calls the relevant IQueueMessageHandler to deal with it.
        /// </summary>
        /// <param name="queueEvents">The JSON array of events</param>
        private void ProcessSessionQueue(IEnumerable<JToken> queueEvents)
        {
            try
            {
                foreach (JObject queueEvent in queueEvents)
                {
                    string queueEventType = (string)queueEvent["type"];
                    IQueueMessageHandler handler = null;
                    foreach (IQueueMessageHandler discoveredHandler in this.queueMessageHandlers)
                    {
                        if (discoveredHandler.GetName() == queueEventType)
                        {
                            handler = discoveredHandler;
                        }
                    }

                    if (handler != null)
                    {
                        handler.Handle(queueEvent);
                    }
                    else
                    {
                        Log.Warning("ProcessSessionQueue: No handler for event '" + queueEventType + "'", this);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning("ProcessSessionQueue: Error processing session queue (" + e.Message + ")!", true);
            }
        }

        /// <summary>
        /// Sends a queue item to a session.
        /// </summary>
        /// <param name="sessionId">
        /// The session ID to send the queue item to (can be our own)
        /// </param>
        /// <param name="queueEvent">
        /// The raw queue item (use an IQueueMessageHandler to create this)
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool SendQueueItemToSession(string sessionId, JObject queueEvent)
        {
            try
            {
                string response = GetWebResponse(
                    String.Format("http://{0}/api/sendQueueItemToSession?apiVersion={1}&session={2}&queueItem={3}",
                        webEndpoint,
                        ApiVersion,
                        sessionId,
                        WebUtility.UrlEncode(queueEvent.ToString())
                    )
                 );
                JObject queueItemResult = JObject.Parse(response);

                // If an error occured, throw an exception.
                if (queueItemResult["success"] == null || queueItemResult["error"] != null)
                {
                    throw new Exception("An error occured.");
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Sends a queue item to all sessions.
        /// </summary>
        /// <param name="queueEvent">The raw queue item (use an IQueueMessageHandler to create this)</param>
        public bool SendQueueItemToAll(JObject queueEvent)
        {
            try
            {
                string response = GetWebResponse(
                    String.Format("http://{0}/api/sendQueueItemToAll?apiVersion={1}&queueItem={2}",
                        webEndpoint,
                        ApiVersion,
                        WebUtility.UrlEncode(queueEvent.ToString())
                    )
                 );
                JObject queueItemResult = JObject.Parse(response);

                // If an error occured, throw an exception.
                if (queueItemResult["success"] == null || queueItemResult["error"] != null)
                {
                    throw new Exception("An error occured.");
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Renews the current session held by the client - and checks for any queue events and executes them.
        /// This call will clear the list of queue events stored by the server (unless excludeQueue is specified, in which case queues will not be touched)
        /// </summary>
        /// <param name="excludeQueue">
        /// The exclude queue.
        /// </param>
        /// <returns>
        /// Whether renewing was successful.
        /// </returns>
        private bool RenewSession(bool excludeQueue = false)
        {
            try
            {
                string response = GetWebResponse(
                    String.Format("http://{0}/api/renewSession?apiVersion={1}&session={2}&excludeQueue={3}", webEndpoint, ApiVersion, SessionID, excludeQueue ? "true" : "")
                );
                JObject renewResult = JObject.Parse(response);

                // If an error occured, throw an exception.
                if (renewResult["error"] != null)
                {
                    throw new Exception("An error occured. Possible session expired already?");
                }

                // If there are entries in the session queue to process them, process them.
                if(renewResult["queue"] != null)
                {
                    ProcessSessionQueue((JArray)renewResult["queue"]);
                }
            }
            catch (Exception)
            {
                Log.Error("RenewSession: Failed to renew session", this);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Sets a session variable. The variable can then be used to filter through current sessions and to provide information to other LCPDFR instances.
        /// Mainly for automatic network connection setups.
        /// </summary>
        /// <param name="variableName">The variable name to set</param>
        /// <param name="variableValue">The variable's value</param>
        /// <returns></returns>
        public bool SetSessionVariable(string variableName, string variableValue)
        {
            try
            {
                string response = GetWebResponse(
                    string.Format("http://{0}/api/setSessionVariable?apiVersion={1}&variableName={2}&variableValue={3}&session={4}", 
                    webEndpoint, ApiVersion, variableName, variableValue, SessionID));

                JObject setResult = JObject.Parse(response);
                if (setResult["success"] != null && (bool)setResult["success"] != true)
                {
                    throw new Exception("Didn't succeed");
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error("SetSessionVariable: Failed to set session variable: " + ex, this);
            }

            return false;
        }

        /// <summary>
        /// Allows you to find sessions by a variable's value.
        /// For example, we could pass 'xlivename' as variable name and 'samspam' as variable value to find sessions by that user.
        /// </summary>
        /// <param name="variableName">The variable name to set</param>
        /// <param name="variableValue">The variable's value</param>
        /// <returns></returns>
        public JArray FindSessionsByVariableSearch(string variableName, string variableValue)
        {
            try
            {
                string response = GetWebResponse(
                    String.Format("http://{0}/api/findBySessionVariable?apiVersion={1}&variableName={2}&variableValue={3}", webEndpoint, ApiVersion, variableName, variableValue)
                );
                JObject findResult = JObject.Parse(response);
                if (findResult["success"] != null && (bool)findResult["success"] != true)
                   {
                    throw new Exception("Not successful");
                }
                return (JArray)findResult["results"];
            }
            catch(Exception)
            {
                Log.Error("FindSessionsByVariableSearch: Failed to search sessions", this);
            }

            return null;
        }

        /// <summary>
        /// Gets all information about the user
        /// </summary>
        /// <returns>The information.</returns>
        public JObject GetUserInformationJObject()
        {
            try
            {
                string response = GetWebResponse(
                    String.Format("http://{0}/api/getUserData?apiVersion={1}&session={2}", webEndpoint, ApiVersion, SessionID)
                );
                JObject userInfoResult = JObject.Parse(response);
                return (JObject)userInfoResult["userdata"];
            }
            catch (Exception)
            {
                Log.Error("GetUserInformationString: Failed to query data", this);
            }

            return new JObject();
        }

        /// <summary>
        /// Get the latest version string from the server, dependant on our current build.
        /// </summary>
        /// <param name="currentVersion">the current version string</param>
        /// <param name="isBeta">is a beta version?</param>
        /// <param name="isDev">is a development version?</param>
        /// <returns></returns>
        public string GetLatestVersionString(string currentVersion, bool isBeta, bool isDev)
        {
            try
            {
                string beta = isBeta ? "yes": "no";
                string dev = isDev ? "yes" : "no";
                string response = GetWebResponse("http://" + updateEndpoint + "/check.php?version=" + currentVersion + "&beta=" + beta + "&dev=" + dev);
                return response;
            }
            catch (Exception)
            {
                Log.Error("GetLatestVersionString: Failed to query data", this);
            }
            return string.Empty;
        }

        /// <summary>
        /// Updates a variable called <paramref name="variableName"/> with <paramref name="value"/> while performing <paramref name="action"/>. The web request is made in a new thread.
        /// </summary>
        /// <param name="variableName">The name of the variable to be updated.</param>
        /// <param name="action">The action, such as "add" or "check".</param>
        /// <param name="value">The value, such as 1.</param>
        public void UpdateServerValueAsync(string variableName, string action, string value)
        {
            this.UpdateServerValueAsync(variableName, action, value, null);
        }

        /// <summary>
        /// Updates a variable called <paramref name="variableName"/> with <paramref name="value"/> while performing <paramref name="action"/>. The web request is made in a new thread.
        /// </summary>
        /// <param name="variableName">The name of the variable to be updated.</param>
        /// <param name="action">The action, such as "add" or "check".</param>
        /// <param name="value">The value, such as 1.</param>
        /// <param name="appendData">Data that should be appended to the request.</param>
        public void UpdateServerValueAsync(string variableName, string action, string value, string appendData)
        {
            // Delegate to fetch all arguments and call internal function
            Action threadDelegate = delegate
            {
                lock (this.nonceLock)
                {
                    this.UpdateServerValue(variableName, action, value, appendData);
                }
            };

            // Spawn thread
            Thread thread = new Thread(new ThreadStart(threadDelegate));
            thread.IsBackground = true;
            thread.Start();
        }

        /// <summary>
        /// Checks the internet connection by loading www.google.com.
        /// </summary>
        /// <returns>True if connected to the internet, false if not.</returns>
        private static bool CheckInternetConnection()
        {
            try
            {
                using (var client = new WebClient())
                using (var stream = client.OpenRead("http://www.google.com"))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Makes a HTTP web request and returns the response.
        /// </summary>
        /// <param name="url">The url.</param>
        /// <returns>The response.</returns>
        private static string GetWebResponse(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(url));
            request.Proxy = null; // <-- this is the good stuff
            request.Headers.Add("X-LCPDFR-Request", "True");
            request.Headers.Add("X-LCPDFR-HWAuth", Authentication.GetHardwareID());
            request.Headers.Add("X-LCPDFR-APIKey", Settings.NetworkAPIKey);
            Version v = Assembly.GetExecutingAssembly().GetName().Version;
            request.Headers.Add("X-LCPDFR-Version", String.Format("{0}.{1}.{2}.{3}", v.Major, v.Minor, v.Build, v.Revision));

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream responseStream = response.GetResponseStream();

            StreamReader streamReader = new StreamReader(responseStream);
            string responseData = streamReader.ReadToEnd();
            return responseData;
        }

        /// <summary>
        /// Gets the public IP of the computer.
        /// </summary>
        /// <returns>True if got the IP, false if not.</returns>
        private bool GetPublicIP()
        {
            try
            {
                //try
                //{
                //    if (UPnP.Discover())
                //    {
                //        Log.Debug("GetPublicIP: Found UPnP device", this);
                //        IPAddress address = UPnP.GetExternalIP();
                //        if (!address.Equals(IPAddress.Any))
                //        {
                //            this.IPAddress = address;
                //            return true;
                //        }
                //    }
                //}
                //catch (Exception exception)
                //{
                //    Log.Error("GetPublicIP: Error when search for UPnP devices: " + exception.Message, this);
                //}

                Log.Debug("GetPublicIP: Failed to find UPnP device providing IP", this);

                // Lennart Jul-25-2022: http://bot.whatismyipaddress.com no longer works, replaced with custom endpoint.
                string url = "https://ipintel.g17media.com/ipv4/me?textOnly=true";
                WebClient webClient = new WebClient();
                string response = Encoding.UTF8.GetString(webClient.DownloadData(url));
                IPAddress ip = IPAddress.Parse(response);
                this.IPAddress = ip;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Updates a variable called <paramref name="variableName"/> with <paramref name="value"/> while performing <paramref name="action"/>.
        /// </summary>
        /// <param name="variableName">The name of the variable to be updated.</param>
        /// <param name="action">The action, such as "add" or "check".</param>
        /// <param name="value">The value, such as 1.</param>
        /// <param name="appendData">Data that should be appended to the request.</param>
        private void UpdateServerValue(string variableName, string action, string value, string appendData)
        {
            // Generate validation response based on this session's nonce
            string validationResponse = GetValidationResponse();

            // Prepare webrequest
            string requestString = String.Format(
                "http://{0}/api/manipulateStat?apiVersion={1}&session={2}&variable={3}&count={4}&validationResponse={5}&",
                webEndpoint,
                ApiVersion,
                SessionID,
                variableName,
                value,
                validationResponse
            );

            // Append data if there is any
            if (appendData != null)
            {
                requestString += appendData;
            }

            // Connect to server
            try
            {
                // Get JSON text
                string content = GetWebResponse(requestString);

                // Convert to Object
                JObject updateServerValueResult = JObject.Parse(content);

                // Did it fail server-side?
                if (updateServerValueResult["success"] == null || (bool)updateServerValueResult["success"] != true)
                    throw new Exception("Server-side problem");

                // Set the new hashing nonce
                this.sessionNonce = (string)updateServerValueResult["newSessionNonce"];

                Log.Info("UpdateServerValue: Updated server stat: " + variableName, this);
            }
            catch (Exception)
            {
                Log.Warning("UpdateServerValue: Failed to update server stat: " + variableName, this);
            }
        }

        public void UploadStatisticsFile(byte[] fileBytes)
        {
            System.Net.ServicePointManager.Expect100Continue = false;
            NameValueCollection inputs = new NameValueCollection();
            string value = System.Text.Encoding.Default.GetString(fileBytes);
            inputs.Add("data", value);
            WebClient webClient = new WebClient();
            webClient.Proxy = null;
            webClient.Headers.Add("X-LCPDFR-Request", "True");
            webClient.Headers.Add("X-LCPDFR-HWAuth", Authentication.GetHardwareID());
            webClient.Headers.Add("X-LCPDFR-APIKey", Settings.NetworkAPIKey);
            Version v = Assembly.GetExecutingAssembly().GetName().Version;
            webClient.Headers.Add("X-LCPDFR-Version", String.Format("{0}.{1}.{2}.{3}", v.Major, v.Minor, v.Build, v.Revision));
            webClient.UploadValues(new Uri(String.Format("http://{0}/api/statisticFileUpload?apiVersion={1}", this.webEndpoint, ApiVersion)), inputs);
        }
        
        private string GetValidationResponse()
        {
            // Lennart Jul-25-2022: Removed for now, can pass in as env variable later for distribution.
            return GetMd5Hash(String.Format("<snip>", this.sessionNonce));
        }

        /// <summary>
        /// Hashes <paramref name="strToHash"/> using MD5.
        /// </summary>
        /// <param name="strToHash">The string to hash.</param>
        /// <returns>The hash.</returns>
        private string GetMd5Hash(string strToHash)
        {
            MD5CryptoServiceProvider cryptoServiceProvider = new MD5CryptoServiceProvider();
            byte[] bytesToHash = Encoding.ASCII.GetBytes(strToHash);
            bytesToHash = cryptoServiceProvider.ComputeHash(bytesToHash);

            string strResult = string.Empty;
            foreach (byte b in bytesToHash)
            {
                strResult += b.ToString("x2");
            }
            return strResult;
        }
    }
}