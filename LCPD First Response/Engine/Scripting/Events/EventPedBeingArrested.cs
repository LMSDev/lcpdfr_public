namespace LCPD_First_Response.Engine.Scripting.Events
{
    using LCPD_First_Response.Engine.Scripting.Entities;

    class EventPedBeingArrested : Event
    {
        public delegate void EventRaisedEventHandler(EventPedBeingArrested @event);
        public static event EventRaisedEventHandler EventRaised;

        public CPed Ped { get; private set; }

        public EventPedBeingArrested(CPed ped)
        {
            this.Ped = ped;

            if (EventRaised != null)
            {
                EventRaised(this);
            }
        }
    }
}
