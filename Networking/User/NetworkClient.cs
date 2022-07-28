namespace LCPDFR.Networking.User
{
    using System.Net;

    using Lidgren.Network;

    /// <summary>
    /// Represents an actual client on the network and provides high level functionality.
    /// </summary>
    public class NetworkClient
    {
        /// <summary>
        /// The unique client identifier.
        /// </summary>
        private readonly string identifier;

        /// <summary>
        /// The internal <see cref="NetConnection"/> instance.
        /// </summary>
        private readonly NetConnection netConnection;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkClient"/> class.
        /// </summary>
        /// <param name="identifier">
        /// The identifier.
        /// </param>
        /// <param name="netConnection">
        /// The net connection.
        /// </param>
        internal NetworkClient(string identifier, NetConnection netConnection)
        {
            this.identifier = identifier;
            this.netConnection = netConnection;
            this.NetworkEndPoint = this.netConnection.RemoteEndPoint;
        }

        /// <summary>
        /// Gets a value indicating whether a disconnect was forced for this client.
        /// </summary>
        public bool ForcedDisconnected { get; private set; }

        /// <summary>
        /// Gets the unique client identifier.
        /// </summary>
        public string Identifier
        {
            get
            {
                return this.identifier;
            }
        }

        /// <summary>
        /// Gets the name of the client (if received).
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Gets the network end point of the client.
        /// </summary>
        public IPEndPoint NetworkEndPoint { get; private set; }

        /// <summary>
        /// Gets the name of the client. If name has not been retrieved yet, returns the unique identifier.
        /// </summary>
        public string SafeName
        {
            get
            {
                if (string.IsNullOrEmpty(this.Name))
                {
                    return this.Identifier;
                }

                return this.Name;
            }
        }

        /// <summary>
        /// Gets the version of the client (if received).
        /// </summary>
        public string Version { get; internal set; }

        /// <summary>
        /// Gets the internal <see cref="NetConnection"/> instance.
        /// </summary>
        internal NetConnection NetConnection
        {
            get
            {
                return this.netConnection;
            }
        }

        /// <summary>
        /// Forcefully disconnects the client.
        /// </summary>
        /// <param name="message">The message delivered to the client.</param>
        public void ForceDisconnect(string message)
        {
            this.NetConnection.Disconnect(message);
            this.ForcedDisconnected = true;
        }
    }
}