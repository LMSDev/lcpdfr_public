namespace LCPD_First_Response.Engine.Scripting.Entities
{
    using GTA;

    /// <summary>
    /// Pickup types.
    /// </summary>
    internal enum EPickupType : uint
    {
        /// <summary>
        /// A basic shotgun.
        /// </summary>
        BasicShotgun = 1846597315,

        /// <summary>
        /// A baretta shotgun.
        /// </summary>
        BarettaShotgun = 3719476653,

        /// <summary>
        /// A bulletproof vest.
        /// </summary>
        BulletProofVest = 2536352272,

        /// <summary>
        /// A first aid box.
        /// </summary>
        FirstAidBox = 1069950328,

        /// <summary>
        /// A glock.
        /// </summary>
        Glock = 4098655133,

        /// <summary>
        /// A grenade.
        /// </summary>
        Grenade = 993473937,

        /// <summary>
        /// A M4.
        /// </summary>
        M4 = 897930585,

        /// <summary>
        /// A pigeon.
        /// </summary>
        Pigeon = 2559912683,
    }

    /// <summary>
    /// Pickup that can be collected in-game.
    /// </summary>
    internal class CPickup : PickupBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CPickup"/> class.
        /// </summary>
        /// <param name="pickupType">
        /// The pickup type.
        /// </param>
        /// <param name="position">
        /// The position.
        /// </param>
        /// <param name="rotation">
        /// The rotation.
        /// </param>
        public CPickup(EPickupType pickupType, GTA.Vector3 position, GTA.Vector3 rotation)
        {
            this.Pickup = Pickup.CreatePickup(position, (uint)pickupType, PickupType.Weapon, rotation);
        }

        /// <summary>
        /// Gets the blip of the pickup.
        /// </summary>
        public Blip Blip { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the pickup has a blip.
        /// </summary>
        public bool HasBlip
        {
            get { return this.Blip != null && this.Blip.Exists(); }
        }

        /// <summary>
        /// Attaches a blip.
        /// </summary>
        /// <returns>The blip.</returns>
        public new Blip AttachBlip()
        {
            if (this.HasBlip)
            {
                return this.Blip;
            }

            this.Blip = base.AttachBlip();
            return this.Blip;
        }
    }
}
