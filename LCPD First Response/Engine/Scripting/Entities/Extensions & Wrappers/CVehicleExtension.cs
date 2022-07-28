namespace LCPD_First_Response.Engine.Scripting.Entities
{
    using AdvancedHookManaged;
    using LCPD_First_Response.Engine.Scripting.Native;

    static class CVehicleExtension
    {
        // Extension methods for CVehicle
        public static bool IsStuck(this CVehicle vehicle, int stuckSinceMs, int unknown = 1, int unknown2 = 0, int unknown3 = 0)
        {
            return Natives.IsVehStuck(vehicle, stuckSinceMs, unknown, unknown2, unknown3);
        }

        public static void SetHeliBladesFullSpeed(this CVehicle vehicle)
        {
            Natives.SetHeliBladesFullSpeed(vehicle);
        }


        // Extension methods for pool

        /// <summary>
        /// Returns the vehicle for <paramref name="handle"/>.
        /// </summary>
        /// <param name="pool">The pool.</param>
        /// <param name="handle">The handle.</param>
        /// <returns>The vehicle.</returns>
        public static CVehicle AtVehicleHandle(this Pool<CVehicle> pool, int handle)
        {
            foreach (CEntity entity in pool.GetAll())
            {
                if (entity.Handle == handle)
                {
                    return (CVehicle)entity;
                }
            }

            return null;
        }

        public static CVehicle GetVehicleFromPool(this Pool<CVehicle> pool, AVehicle vehicle)
        {
            foreach (CVehicle cVehicle in pool.GetAll())
            {
                if (cVehicle.Handle == vehicle.Get())
                {
                    return cVehicle;
                }
            }
            return null;
        }

        public static CVehicle GetVehicleFromPool(this Pool<CVehicle> pool, GTA.Vehicle vehicle)
        {
            foreach (CVehicle cVehicle in pool.GetAll())
            {
                if (cVehicle.Handle == vehicle.pHandle)
                {
                    return cVehicle;
                }
            }
            return null;
        }
    }
}
