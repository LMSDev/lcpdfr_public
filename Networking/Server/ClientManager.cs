namespace LCPDFR.Networking.Server
{
    using System.Collections.Generic;

    using LCPDFR.Networking.User;

    using Lidgren.Network;

    /// <summary>
    /// Manages the clients connected to a server.
    /// </summary>
    internal class ClientManager
    {
        /// <summary>
        /// The clients registered.
        /// </summary>
        private Dictionary<long, NetworkClient> clients;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientManager"/> class.
        /// </summary>
        public ClientManager()
        {
            this.clients = new Dictionary<long, NetworkClient>();
        }

        public void AddClient(long identifier, NetConnection netConnection)
        {
            this.clients.Add(identifier, new NetworkClient(NetUtility.ToHexString(identifier), netConnection));
        }

        public NetworkClient GetClient(long identifier)
        {
            return this.clients[identifier];
        }

        public bool HasClient(long identifier)
        {
            return this.clients.ContainsKey(identifier);
        }
    }
}