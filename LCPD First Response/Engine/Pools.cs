namespace LCPD_First_Response.Engine
{
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Timers;

    /// <summary>
    /// Singleton class that holds references to all pools
    /// </summary>
    internal static class Pools
    {
        /// <summary>
        /// Initializes static members of the <see cref="Pools"/> class.
        /// </summary>
        static Pools()
        {
            Pools.CoreTicks = new Pool<ICoreTickable>();
            Pools.Ticks = new Pool<ITickable>();

            Pools.PedPool = new Pool<CPed>();
            Pools.VehiclePool = new Pool<CVehicle>();
        }

        /// <summary>
        /// Gets the pool of core ticks
        /// </summary>
        public static Pool<ICoreTickable> CoreTicks { get; private set; }

        /// <summary>
        /// Gets the pool of normal ticks
        /// </summary>
        public static Pool<ITickable> Ticks { get; private set; }

        /// <summary>
        /// Gets the ped pool
        /// </summary>
        public static Pool<CPed> PedPool { get; private set; }

        /// <summary>
        /// Gets the vehicle pool
        /// </summary>
        public static Pool<CVehicle> VehiclePool { get; private set; }

    }
}