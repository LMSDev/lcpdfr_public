namespace LCPD_First_Response.LCPDFR.Scripts.Events
{
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Events;

    /// <summary>
    /// Event fired when the player has started an arrest.
    /// </summary>
    internal class EventPlayerStartedArrest : Event
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventPlayerStartedArrest"/> class.
        /// </summary>
        /// <param name="pedBeingArrested">
        /// The ped being arrested.
        /// </param>
        /// <param name="arrest">
        /// The arrest instance.
        /// </param>
        public EventPlayerStartedArrest(CPed pedBeingArrested, Arrest arrest)
        {
            this.PedBeingArrested = pedBeingArrested;
            this.Arrest = arrest;

            if (EventRaised != null)
            {
                EventRaised(this);
            }
        }

        /// <summary>
        /// The delegate used.
        /// </summary>
        /// <param name="event">The event.</param>
        public delegate void EventRaisedEventHandler(EventPlayerStartedArrest @event);

        /// <summary>
        /// Invoked when the event has been raised.
        /// </summary>
        public static event EventRaisedEventHandler EventRaised;

        /// <summary>
        /// Gets the arrest instance.
        /// </summary>
        public Arrest Arrest { get; private set; }

        /// <summary>
        /// Gets the ped being arrested.
        /// </summary>
        public CPed PedBeingArrested { get; private set; }
    }
}
