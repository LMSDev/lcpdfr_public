namespace LCPD_First_Response.Engine.Scripting.Events
{
    using LCPD_First_Response.Engine.Scripting.Entities;

    /// <summary>
    /// Fired when a helicopter being piloted by TaskCopHelicopter dies
    /// </summary>
    internal class EventHelicopterDown : Event
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventHelicopterDown"/> class.
        /// </summary>
        /// <param name="helicopter">The helicopter.</param>
        public EventHelicopterDown(CVehicle helicopter)
        {
            this.Vehicle = helicopter;

            Log.Warning("EventHelicopterDown", "TaskCopHelicopter");

            if (EventRaised != null)
            {
                EventRaised(this);
            }
        }

        /// <summary>
        /// The event handler.
        /// </summary>
        /// <param name="event">The event.</param>
        public delegate void EventRaisedEventHandler(EventHelicopterDown @event);

        /// <summary>
        /// The event.
        /// </summary>
        public static event EventRaisedEventHandler EventRaised;

        /// <summary>
        /// Gets the ped.
        /// </summary>
        public CVehicle Vehicle { get; private set; }
    }
}