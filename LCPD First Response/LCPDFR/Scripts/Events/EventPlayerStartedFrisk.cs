namespace LCPD_First_Response.LCPDFR.Scripts.Events
{
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Events;

    /// <summary>
    /// Event fired when the player has started an arrest.
    /// </summary>
    internal class EventPlayerStartedFrisk : Event
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventPlayerStartedFrisk"/> class.
        /// </summary>
        /// <param name="pedBeingFrisked">
        /// The ped being frisked.
        /// </param>
        /// <param name="frisk">
        /// The frisk instance.
        /// </param>
        public EventPlayerStartedFrisk(CPed pedBeingFrisked, Frisk frisk)
        {
            this.PedBeingFrisked = pedBeingFrisked;
            this.Frisk = frisk;

            if (EventRaised != null)
            {
                EventRaised(this);
            }
        }

        /// <summary>
        /// The delegate used.
        /// </summary>
        /// <param name="event">The event.</param>
        public delegate void EventRaisedEventHandler(EventPlayerStartedFrisk @event);

        /// <summary>
        /// Invoked when the event has been raised.
        /// </summary>
        public static event EventRaisedEventHandler EventRaised;

        /// <summary>
        /// Gets the frisk instance.
        /// </summary>
        public Frisk Frisk { get; private set; }

        /// <summary>
        /// Gets the ped being frisked.
        /// </summary>
        public CPed PedBeingFrisked { get; private set; }
    }
}