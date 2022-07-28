namespace LCPD_First_Response.Engine.Scripting.Events
{
    using LCPD_First_Response.Engine.Scripting.Entities;

    class EventAmbientFootChase : Event
    {
        public delegate void EventRaisedEventHandler(EventAmbientFootChase @event);
        public static event EventRaisedEventHandler EventRaised;

        public CPed Cop { get; private set; }
        public CPed Suspect { get; private set; }
        public bool ForceReport { get; private set; }

        /// <summary>
        /// Fires a new event for an ambient foot chase
        /// </summary>
        /// <param name="cop">The officer</param>
        /// <param name="suspect">The suspect</param>
        public EventAmbientFootChase(CPed cop, CPed suspect)
        {
            this.Cop = cop;
            this.Suspect = suspect;

            if (EventRaised != null)
            {
                EventRaised(this);
            }
        }
    }
}
