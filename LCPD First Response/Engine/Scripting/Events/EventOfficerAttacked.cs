namespace LCPD_First_Response.Engine.Scripting.Events
{
    using LCPD_First_Response.Engine.Scripting.Entities;

    class EventOfficerAttacked : Event
    {
        public delegate void EventRaisedEventHandler(EventOfficerAttacked @event);
        public static event EventRaisedEventHandler EventRaised;

        public CPed Ped { get; private set; }
        public CPed Attacker { get; private set; }
        public bool ForceReport { get; private set; }

        /// <summary>
        /// Fires a new event for an officer being attacked
        /// </summary>
        /// <param name="cop">The officer</param>
        /// <param name="attacker">The attacker</param>
        /// <param name="forceReport">Whether or not to force an audio report</param>
        public EventOfficerAttacked(CPed cop, CPed attacker, bool forceReport = false)
        {
            this.Ped = cop;
            this.Attacker = attacker;
            this.ForceReport = forceReport;

            if (EventRaised != null)
            {
                EventRaised(this);
            }
        }
    }
}
