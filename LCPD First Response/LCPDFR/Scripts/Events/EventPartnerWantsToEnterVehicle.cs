namespace LCPD_First_Response.LCPDFR.Scripts.Events
{
    using System;

    using GTA;

    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Events;
    using LCPD_First_Response.LCPDFR.Scripts.Partners;

    class EventPartnerWantsToEnterVehicle : Event
    {
        public EventPartnerWantsToEnterVehicle(Partner partner, CVehicle vehicle, GTA.VehicleSeat seat)
        {
            this.Partner = partner;
            this.Vehicle = vehicle;
            this.Seat = seat;
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

        public Partner Partner { get; private set; }

        public CVehicle Vehicle { get; private set; }

        public VehicleSeat Seat { get; private set; }

        public bool Result { get; private set; }


        /// <summary>
        /// The delegate used.
        /// </summary>
        /// <param name="event">The event.</param>
        public delegate bool EventRaisedEventHandler(EventPartnerWantsToEnterVehicle @event);

        /// <summary>
        /// Invoked when the event has been raised.
        /// </summary>
        public static event EventRaisedEventHandler EventRaised;
    }
}