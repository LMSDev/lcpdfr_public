namespace LCPD_First_Response.Engine.Scripting.Events
{
    /// <summary>
    /// Event fired when a connection to the LCPDFR server has been established.
    /// </summary>
    internal class EventNetworkConnectionEstablished : Event
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventNetworkConnectionEstablished"/> class.
        /// </summary>
        public EventNetworkConnectionEstablished()
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
        public delegate void EventRaisedEventHandler(EventNetworkConnectionEstablished @event);

        /// <summary>
        /// The event.
        /// </summary>
        public static event EventRaisedEventHandler EventRaised;
    }
}