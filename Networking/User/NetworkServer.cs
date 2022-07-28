namespace LCPDFR.Networking.User
{
    using Lidgren.Network;

    /// <summary>
    /// Represents an actual server on the network and provides high level functionality.
    /// </summary>
    public class NetworkServer : NetworkClient
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkServer"/> class.
        /// </summary>
        /// <param name="identifier">
        /// The identifier.
        /// </param>
        /// <param name="netConnection">
        /// The net connection.
        /// </param>
        internal NetworkServer(string identifier, NetConnection netConnection)
            : base(identifier, netConnection)
        {
        }
    }
}