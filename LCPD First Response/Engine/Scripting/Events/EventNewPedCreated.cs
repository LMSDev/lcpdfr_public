namespace LCPD_First_Response.Engine.Scripting.Events
{
    using LCPD_First_Response.Engine.Scripting.Entities;

    /// <summary>
    /// Fired when a new ped has been created.
    /// </summary>
    internal class EventNewPedCreated : Event
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventNewPedCreated"/> class.
        /// </summary>
        /// <param name="ped">
        /// The ped.
        /// </param>
        public EventNewPedCreated(CPed ped)
        {
            this.Ped = ped;

            if (EventRaised != null)
            {
                EventRaised(this);
            }
        }

        /// <summary>
        /// The event handler.
        /// </summary>
        /// <param name="event">The event.</param>
        public delegate void EventRaisedEventHandler(EventNewPedCreated @event);

        /// <summary>
        /// Event raised.
        /// </summary>
        public static event EventRaisedEventHandler EventRaised;

        /// <summary>
        /// Gets the ped.
        /// </summary>
        public CPed Ped { get; private set; }
    }
}