namespace LCPD_First_Response.Engine.Scripting.Events
{
    /// <summary>
    /// Event fired when either internet or the server connection failed to establish.
    /// </summary>
    internal class EventNetworkConnectionFailed : Event
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventNetworkConnectionFailed"/> class.
        /// </summary>
        public EventNetworkConnectionFailed()
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
        public delegate void EventRaisedEventHandler(EventNetworkConnectionFailed @event);

        /// <summary>
        /// The event.
        /// </summary>
        public static event EventRaisedEventHandler EventRaised;
    }
}