namespace LCPD_First_Response.Engine.Scripting
{
    using LCPD_First_Response.Engine.Scripting.Entities;

    /// <summary>
    /// Wrapper class for <see cref="GTA.Pickup"/>.
    /// </summary>
    internal class PickupBase
    {
        /// <summary>
        /// The internal pickup instance.
        /// </summary>
        private GTA.Pickup pickup;

        /// <summary>
        /// Sets a value indicating whether the pickup can be collected by a vehicle.
        /// </summary>
        public bool CollectableByCar
        {
            set
            {
                this.pickup.CollectableByCar = value;
            }
        }

        /// <summary>
        /// Gets or sets the current room of the pickup.
        /// </summary>
        public GTA.Room CurrentRoom
        {
            get
            {
                return this.pickup.CurrentRoom;
            }

            set
            {
                this.pickup.CurrentRoom = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the pickup has been collected.
        /// </summary>
        public bool HasBeenCollected
        {
            get
            {
                return this.pickup.HasBeenCollected;
            }
        }

        /// <summary>
        /// Gets the position of the pickup.
        /// </summary>
        public GTA.Vector3 Position
        {
            get
            {
                return this.pickup.Position;
            }
        }

        /// <summary>
        /// Gets or sets the internal <see cref="GTA.Pickup"/> instance.
        /// </summary>
        protected GTA.Pickup Pickup
        {
            get
            {
                return this.pickup;
            }

            set
            {
                this.pickup = value;
            }
        }

        /// <summary>
        /// Attaches a blip.
        /// </summary>
        /// <returns>The blip.</returns>
        public GTA.Blip AttachBlip()
        {
            return this.pickup.AttachBlip();
        }

        /// <summary>
        /// Deletes the pickup.
        /// </summary>
        public void Delete()
        {
            if (this.Exists())
            {
                this.pickup.Delete();
            }
        }

        /// <summary>
        /// Returns whether the pickup still exists in-game.
        /// </summary>
        /// <returns>True if exists, false if not.</returns>
        public bool Exists()
        {
            return this.pickup != null && this.pickup.Exists();
        }

        /// <summary>
        /// Gives the pickup to <paramref name="ped"/>.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public void GiveToPed(CPed ped)
        {
            this.pickup.GiveToPed(ped);
        }

    }
}
