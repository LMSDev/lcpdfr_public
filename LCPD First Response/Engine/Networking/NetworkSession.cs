namespace LCPD_First_Response.Engine.Networking
{
    /// <summary>
    /// Represents a player on the network returned by the master server as a session.
    /// </summary>
    class NetworkSession
    {
        public string ServerName { get; set; }

        public string LocalPlayerName { get; set; }

        public bool IsHost { get; set; }

        public bool IsNetworkSession { get; set; }

        public string Ip { get; set; }

        public int Port { get; set; }

        public NetworkSession(
            string serverName,
            string localPlayerName,
            bool isHost,
            bool isNetworkSession,
            string ip,
            int port)
        {
            this.ServerName = serverName;
            this.LocalPlayerName = localPlayerName;
            this.IsHost = isHost;
            this.IsNetworkSession = isNetworkSession;
            this.Ip = ip;
            this.Port = port;
        }
    }
}