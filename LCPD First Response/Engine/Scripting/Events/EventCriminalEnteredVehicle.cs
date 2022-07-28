namespace LCPD_First_Response.Engine.Scripting.Events
{
    using LCPD_First_Response.Engine.Scripting.Entities;

    /// <summary>
    /// Fired when a criminal has entered a vehicle during a chase.
    /// </summary>
    internal class EventCriminalEnteredVehicle : Event
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventCriminalEnteredVehicle"/> class.
        /// </summary>
        /// <param name="ped">
        /// The ped.
        /// </param>
        public EventCriminalEnteredVehicle(CPed ped)
        {
            this.Ped = ped;

            if (EventRaised != null)
            {
                EventRaised(this);
            }

            Log.Debug("EventCriminalEnteredVehicle: Fired", "EventCriminalEnteredVehicle");
        }

        /// <summary>
        /// The event handler.
        /// </summary>
        /// <param name="event">The event.</param>
        public delegate void EventRaisedEventHandler(EventCriminalEnteredVehicle @event);

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