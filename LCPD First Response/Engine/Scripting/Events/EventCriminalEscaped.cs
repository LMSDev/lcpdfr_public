namespace LCPD_First_Response.Engine.Scripting.Events
{
    using LCPD_First_Response.Engine.Scripting.Entities;

    /// <summary>
    /// Fired when a criminal has escaped during a chase.
    /// </summary>
    internal class EventCriminalEscaped : Event
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventCriminalEscaped"/> class.
        /// </summary>
        /// <param name="ped">
        /// The ped.
        /// </param>
        public EventCriminalEscaped(CPed ped)
        {
            this.Ped = ped;

            if (EventRaised != null)
            {
                EventRaised(this);
            }

            Log.Debug("EventCriminalEscaped: Fired", "EventCriminalEscaped");
        }

        /// <summary>
        /// The event handler.
        /// </summary>
        /// <param name="event">The event.</param>
        public delegate void EventRaisedEventHandler(EventCriminalEscaped @event);

        /// <summary>
        /// The event.
        /// </summary>
        public static event EventRaisedEventHandler EventRaised;

        /// <summary>
        /// Gets the ped. Note that the ped will shortly be deleted after the event has been fired.
        /// </summary>
        public CPed Ped { get; private set; }
    }
}