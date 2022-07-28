namespace LCPD_First_Response.Engine.Scripting.Events
{
    using LCPD_First_Response.Engine.Scripting.Entities;

    class EventVisualLost : Event
    {
        public delegate void EventRaisedEventHandler(EventVisualLost @event);
        public static event EventRaisedEventHandler EventRaised;

        public CPed Ped { get; private set; }

        public EventVisualLost(CPed ped)
        {
            this.Ped = ped;

            if (EventRaised != null)
            {
                EventRaised(this);
            }
        }
    }
}
