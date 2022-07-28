namespace LCPD_First_Response.Engine.Scripting.Events
{
    using LCPD_First_Response.Engine.Scripting.Entities;

    /// <summary>
    /// Fired when a ped has been arrested (by AI) and was placed in a vehicle, but before the AI driver got into the car, the player already entered the driver seat, thus the AI driver will
    /// drop the task.
    /// </summary>
    internal class EventArrestedPedSittingInPlayerVehicle : Event
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventArrestedPedSittingInPlayerVehicle"/> class.
        /// </summary>
        /// <param name="ped">
        /// The ped.
        /// </param>
        public EventArrestedPedSittingInPlayerVehicle(CPed ped)
        {
            this.Ped = ped;

            if (EventRaised != null)
            {
                EventRaised(this);
            }

            Log.Debug("EventArrestedPedSittingInPlayerVehicle: Fired", "EventArrestedPedSittingInPlayerVehicle");
        }

        /// <summary>
        /// The event handler.
        /// </summary>
        /// <param name="event">The event.</param>
        public delegate void EventRaisedEventHandler(EventArrestedPedSittingInPlayerVehicle @event);

        /// <summary>
        /// The event.
        /// </summary>
        public static event EventRaisedEventHandler EventRaised;

        /// <summary>
        /// Gets the arrested ped.
        /// </summary>
        public CPed Ped { get; private set; }
    }
}