namespace LCPD_First_Response.Engine.Scripting.Events
{
    using LCPD_First_Response.Engine.Scripting.Entities;

    class EventFleeingCriminal : Event
    {
        public delegate void EventRaisedEventHandler(EventFleeingCriminal @event);
        public static event EventRaisedEventHandler EventRaised;

        public CPed Criminal { get; private set; }

        public EventFleeingCriminal(CPed criminal)
        {
            this.Criminal = criminal;

            if (EventRaised != null)
            {
                EventRaised(this);
            }
        }
    }
}
