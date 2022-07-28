namespace LCPD_First_Response.LCPDFR.API
{
    using LCPD_First_Response.Engine.Scripting.Events;

    using global::LCPDFR.Networking;
    using global::LCPDFR.Networking.Server;

    /// <summary>
    /// The delegate for when the player has joined a network game.
    /// </summary>
    public delegate void JoinedNetworkEventHandler();

    /// <summary>
    /// Provides access to LCPDFR networking functions.
    /// </summary>
    public class Networking
    {
        /// <summary>
        /// Initializes static members of the <see cref="Networking"/> class.
        /// </summary>
        static Networking()
        {
           EventJoinedNetworkGame.EventRaised += EventJoinedNetworkGameOnEventRaised;
        }

        /// <summary>
        /// Fired when the local player has joined or created a network game. Refers to an actual network game, not a network connection established
        /// to other LCPDFR clients.
        /// </summary>
        public static event JoinedNetworkEventHandler JoinedNetworkGame;

        /// <summary>
        /// Gets a value indicating whether the player has established a connection to either a LCPDFR server or client.
        /// </summary>
        public static bool IsConnected
        {
            get
            {
                return Engine.Main.NetworkManager.CanSendData;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the player is hosting the network game.
        /// </summary>
        public static bool IsHost
        {
            get
            {
                return Engine.Main.NetworkManager.IsHost;
            }
        }

        /// <summary>
        /// Gets value indicating whether the player is in a network game.
        /// </summary>
        public static bool IsInSession
        {
            get
            {
                return Engine.Main.NetworkManager.IsNetworkSession;
            }
        }

        /// <summary>
        /// Gets the <see cref="Client"/> that is used for LCPDFR network communication. Only used when player is not the host.
        /// </summary>
        /// <returns>The client.</returns>
        public static Client GetClientInstance()
        {
            return Engine.Main.NetworkManager.Client;
        }

        /// <summary>
        /// Gets the <see cref="Server"/> that is used for LCPDFR network communication. Only used when player is the host.
        /// </summary>
        /// <returns>The server.</returns>
        public static Server GetServerInstance()
        {
            return Engine.Main.NetworkManager.Server;
        }

        /// <summary>
        /// Called when player joined a network game.
        /// </summary>
        /// <param name="event">The event.</param>
        private static void EventJoinedNetworkGameOnEventRaised(EventJoinedNetworkGame @event)
        {
            JoinedNetworkEventHandler handler = JoinedNetworkGame;
            if (handler != null)
            {
                handler();
            }
        }
    }
}