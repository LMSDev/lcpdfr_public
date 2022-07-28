namespace LCPD_First_Response.LCPDFR.Scripts.Events
{
    using LCPD_First_Response.Engine.Scripting.Entities;

    /// <summary>
    /// Event fired when the player has started a pullover.
    /// </summary>
    internal class EventPlayerStartedPullover
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventPlayerStartedPullover"/> class.
        /// </summary>
        /// <param name="vehicle">
        /// The vehicle.
        /// </param>
        /// <param name="pullover">
        /// The pullover instance.
        /// </param>
        public EventPlayerStartedPullover(CVehicle vehicle, Pullover pullover)
        {
            this.Vehicle = vehicle;
            this.Pullover = pullover;

            if (EventRaised != null)
            {
                EventRaised(this);
            }
        }

        /// <summary>
        /// The delegate used.
        /// </summary>
        /// <param name="event">The event.</param>
        public delegate void EventRaisedEventHandler(EventPlayerStartedPullover @event);

        /// <summary>
        /// Invoked when the event has been raised.
        /// </summary>
        public static event EventRaisedEventHandler EventRaised;

        /// <summary>
        /// Gets the pullover.
        /// </summary>
        public Pullover Pullover { get; private set; }

        /// <summary>
        /// Gets the vehicle.
        /// </summary>
        public CVehicle Vehicle { get; private set; }
    }
}
