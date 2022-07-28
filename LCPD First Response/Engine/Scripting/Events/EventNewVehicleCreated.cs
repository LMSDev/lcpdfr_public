namespace LCPD_First_Response.Engine.Scripting.Events
{
    using LCPD_First_Response.Engine.Scripting.Entities;

    /// <summary>
    /// Fired when a new vehicle has been created.
    /// </summary>
    internal class EventNewVehicleCreated : Event
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventNewVehicleCreated"/> class.
        /// </summary>
        /// <param name="vehicle">
        /// The vehicle.
        /// </param>
        public EventNewVehicleCreated(CVehicle vehicle)
        {
            this.Vehicle = vehicle;

            if (EventRaised != null)
            {
                EventRaised(this);
            }
        }

        /// <summary>
        /// The event handler.
        /// </summary>
        /// <param name="event">The event.</param>
        public delegate void EventRaisedEventHandler(EventNewVehicleCreated @event);

        /// <summary>
        /// Event raised.
        /// </summary>
        public static event EventRaisedEventHandler EventRaised;

        /// <summary>
        /// Gets the vehicle.
        /// </summary>
        public CVehicle Vehicle { get; private set; }
    }
}