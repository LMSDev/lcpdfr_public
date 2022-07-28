namespace LCPD_First_Response.Engine.Scripting.Events
{
    using LCPD_First_Response.Engine.Scripting.Entities;

    class EventArmedCriminal : Event
    {
        public delegate void EventRaisedEventHandler(EventArmedCriminal @event);
        public static event EventRaisedEventHandler EventRaised;

        public CPed Criminal { get; private set; }

        public EventArmedCriminal(CPed criminal)
        {
            this.Criminal = criminal;

            if (EventRaised != null)
            {
                EventRaised(this);
            }
        }
    }
}
