namespace LCPD_First_Response.Engine.Scripting.Events
{
    /// <summary>
    /// Event fired when player joined or created a network game.
    /// </summary>
    internal class EventJoinedNetworkGame
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventJoinedNetworkGame"/> class.
        /// </summary>
        public EventJoinedNetworkGame()
        {
            if (EventRaised != null)
            {
                EventRaised(this);
            }
        }

        /// <summary>
        /// The event handler.
        /// </summary>
        /// <param name="event">The event.</param>
        public delegate void EventRaisedEventHandler(EventJoinedNetworkGame @event);

        /// <summary>
        /// The event.
        /// </summary>
        public static event EventRaisedEventHandler EventRaised;
    }
}