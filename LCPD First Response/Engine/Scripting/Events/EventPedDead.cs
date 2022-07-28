namespace LCPD_First_Response.Engine.Scripting.Events
{
    using LCPD_First_Response.Engine.Scripting.Entities;

    class EventPedDead : Event
    {
        public delegate void EventRaisedEventHandler(EventPedDead @event);
        public static event EventRaisedEventHandler EventRaised;

        public CPed Ped { get; private set; }

        public EventPedDead(CPed ped)
        {
            this.Ped = ped;

            if (EventRaised != null)
            {
                EventRaised(this);
            }

            // If cop is dead, fire event
            if (ped.PedGroup == EPedGroup.Cop)
            {
                new EventOfficerDown(ped);
            }
        }
    }
}
