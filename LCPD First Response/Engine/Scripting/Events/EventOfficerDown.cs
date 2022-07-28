namespace LCPD_First_Response.Engine.Scripting.Events
{
    using LCPD_First_Response.Engine.Scripting.Entities;

    class EventOfficerDown : Event
    {
        public delegate void EventRaisedEventHandler(EventOfficerDown @event);
        public static event EventRaisedEventHandler EventRaised;

        public CPed Ped { get; private set; }

        public EventOfficerDown(CPed ped)
        {
            this.Ped = ped;

            if (EventRaised != null)
            {
                EventRaised(this);
            }
        }
    }
}
