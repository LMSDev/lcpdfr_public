namespace LCPD_First_Response.Engine.Scripting.Events
{
    using LCPD_First_Response.Engine.Scripting.Entities;

    class EventPoliceToleranceDecrease : Event
    {
        public delegate void EventRaisedEventHandler(EventPoliceToleranceDecrease @event);
        public static event EventRaisedEventHandler EventRaised;

        public CPed Ped { get; private set; }

        public EventPoliceToleranceDecrease(CPed ped)
        {
            this.Ped = ped;

            if (EventRaised != null)
            {
                EventRaised(this);
                Log.Debug("AttackEventRaised", this);
            }
        }
    }
}
