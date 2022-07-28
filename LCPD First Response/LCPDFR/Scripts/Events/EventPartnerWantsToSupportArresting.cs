namespace LCPD_First_Response.LCPDFR.Scripts.Events
{
    using LCPD_First_Response.Engine.Scripting.Events;
    using LCPD_First_Response.LCPDFR.Scripts.Partners;

    class EventPartnerWantsToSupportArresting : Event
    {
        public EventPartnerWantsToSupportArresting(Partner partner, Arrest arrest)
        {
            this.Partner = partner;
            this.Arrest = arrest;
            this.Result = true;

            if (EventRaised != null)
            {
                foreach (EventRaisedEventHandler @delegate in EventRaised.GetInvocationList())
                {
                    if (!@delegate.Invoke(this))
                    {
                        this.Result = false;
                    }
                }
            }
        }

        public Partner Partner { get; set; }

        public Arrest Arrest { get; set; }

        public bool Result { get; private set; }

        /// <summary>
        /// The delegate used.
        /// </summary>
        /// <param name="event">The event.</param>
        public delegate bool EventRaisedEventHandler(EventPartnerWantsToSupportArresting @event);

        /// <summary>
        /// Invoked when the event has been raised.
        /// </summary>
        public static event EventRaisedEventHandler EventRaised;
    }
}