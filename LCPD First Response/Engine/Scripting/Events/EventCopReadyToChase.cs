namespace LCPD_First_Response.Engine.Scripting.Events
{
    using LCPD_First_Response.Engine.Scripting.Entities;

    class EventCopReadyToChase : Event
    {
        public delegate void EventRaisedEventHandler(EventCopReadyToChase @event);
        public static event EventRaisedEventHandler EventRaised;

        public CPed Cop { get; private set; }
        public CPed Criminal { get; private set; }

        public EventCopReadyToChase(CPed cop, CPed criminal)
        {
            this.Cop = cop;
            this.Criminal = criminal;

            if (EventRaised != null)
            {
                EventRaised(this);
            }
        }
    }
}
