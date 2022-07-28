namespace LCPD_First_Response.Engine.Scripting.Events
{
    using LCPD_First_Response.Engine.Scripting.Entities;

    /// <summary>
    /// Fired when a suspect goes over 50 speed
    /// </summary>
    internal class EventCriminalSpeeding : Event
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventCriminalSpeeding"/> class.
        /// </summary>
        /// <param name="criminal">The criminal.</param>
        public EventCriminalSpeeding(CPed criminal)
        {
            this.Ped = criminal;
            this.Ped.PedData.Speeding = true;

            if (EventRaised != null)
            {
                EventRaised(this);
            }
        }

        /// <summary>
        /// The event handler.
        /// </summary>
        /// <param name="event">The event.</param>
        public delegate void EventRaisedEventHandler(EventCriminalSpeeding @event);

        /// <summary>
        /// The event.
        /// </summary>
        public static event EventRaisedEventHandler EventRaised;

        /// <summary>
        /// Gets the ped.
        /// </summary>
        public CPed Ped { get; private set; }
    }
}