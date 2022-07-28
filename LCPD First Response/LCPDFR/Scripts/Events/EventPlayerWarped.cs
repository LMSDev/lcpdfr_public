namespace LCPD_First_Response.LCPDFR.Scripts.Events
{
    using GTA;

    using LCPD_First_Response.Engine.Scripting.Events;

    class EventPlayerWarped : Event
    {
        private readonly Vector3 position;

        public EventPlayerWarped(Vector3 position)
        {
            this.position = position;

            if (EventRaised != null)
            {
                EventRaised(this);
            }
        }

        /// <summary>
        /// The delegate used.
        /// </summary>
        /// <param name="event">The event.</param>
        public delegate void EventRaisedEventHandler(EventPlayerWarped @event);

        /// <summary>
        /// Invoked when the event has been raised.
        /// </summary>
        public static event EventRaisedEventHandler EventRaised;

        public Vector3 Position
        {
            get
            {
                return this.position;
            }
        }
    }
}