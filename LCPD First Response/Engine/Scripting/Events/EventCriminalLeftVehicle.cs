namespace LCPD_First_Response.Engine.Scripting.Events
{
    using LCPD_First_Response.Engine.Scripting.Entities;

    /// <summary>
    /// Fired when a criminal has left a vehicle during a chase.
    /// </summary>
    internal class EventCriminalLeftVehicle : Event
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventCriminalLeftVehicle"/> class.
        /// </summary>
        /// <param name="ped">
        /// The ped.
        /// </param>
        public EventCriminalLeftVehicle(CPed ped)
        {
            this.Ped = ped;

            if (EventRaised != null)
            {
                EventRaised(this);
            }

            Log.Debug("EventCriminalLeftVehicle: Fired", "EventCriminalLeftVehicle");
        }

        /// <summary>
        /// The event handler.
        /// </summary>
        /// <param name="event">The event.</param>
        public delegate void EventRaisedEventHandler(EventCriminalLeftVehicle @event);

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