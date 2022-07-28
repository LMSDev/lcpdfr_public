namespace LCPD_First_Response.Engine.Scripting.Events
{
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Tasks;

    /// <summary>
    /// Fired when a cop has regained visual on a criminal.
    /// </summary>
    internal class EventCriminalSpotted : Event
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventCriminalSpotted"/> class.
        /// </summary>
        /// <param name="cop">The cop.</param>
        /// <param name="criminal">The criminal.</param>
        public EventCriminalSpotted(CPed cop, CPed criminal)
        {
            this.Cop = cop;
            this.Ped = criminal;

            if (EventRaised != null)
            {
                EventRaised(this);
            }

            this.Ped.Wanted.ClearPlacesToSearch();
        }

        /// <summary>
        /// The event handler.
        /// </summary>
        /// <param name="event">The event.</param>
        public delegate void EventRaisedEventHandler(EventCriminalSpotted @event);

        /// <summary>
        /// The event.
        /// </summary>
        public static event EventRaisedEventHandler EventRaised;

        /// <summary>
        /// Gets the cop.
        /// </summary>
        public CPed Cop { get; private set; }

        /// <summary>
        /// Gets the ped.
        /// </summary>
        public CPed Ped { get; private set; }
    }
}