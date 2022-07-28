namespace LCPD_First_Response.Engine.Scripting.Events
{
    using LCPD_First_Response.Engine.Scripting.Entities;

    class EventCopCreated : Event
    {
        public delegate void EventRaisedEventHandler(EventCopCreated @event);
        public static event EventRaisedEventHandler EventRaised;

        public CPed Cop { get; private set; }

        public EventCopCreated(CPed cop)
        {
            this.Cop = cop;

            if (EventRaised != null)
            {
                EventRaised(this);
            }
        }
    }
}
