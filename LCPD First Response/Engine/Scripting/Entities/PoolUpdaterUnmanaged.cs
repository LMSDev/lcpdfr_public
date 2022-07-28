namespace LCPD_First_Response.Engine.Scripting.Entities
{
    using AdvancedHookManaged;

    /// <summary>
    /// Responsible for updating pools by communicationg with the iv-engine (using AdvancedHook)
    /// </summary>
    class PoolUpdaterUnmanaged
    {
        private bool hooked;

        public PoolUpdaterUnmanaged()
        {
            Hook();

            // Fill pools with already created peds and vehicles
            GTA.Vehicle[] vehicles = GTA.World.GetAllVehicles();
            foreach (GTA.Vehicle vehicle in vehicles)
            {
                if (vehicle != null && vehicle.Exists())
                {
                    CVehicle cVehicle = new CVehicle(vehicle.pHandle);                    
                }
            }

            GTA.Ped[] peds = GTA.World.GetAllPeds();
            foreach (GTA.Ped ped in peds)
            {
                if (ped != null && ped.Exists())
                {
                    // HACK: No player ped
                    // TODO: Additional checks
                    if (ped.pHandle == GTA.Game.LocalPlayer.Character.pHandle) continue;

                    // Since instances of ped deads are immediately deleted, no need to add them at all
                    if (!ped.isAliveAndWell)
                    {
                        continue;
                    }

                    // Handle might be added due to the vehicle loop above (it adds all passengers), so check if ped exists already.
                    if (Pools.PedPool.AtPedHandle(ped.pHandle) == null)
                    {
                        CPed cPed = new CPed(ped.pHandle);
                    }
                }
            }
        }

        ~PoolUpdaterUnmanaged()
        {
            if (hooked)
            {
                // Unhook
            }
        }

        public void UpdatePools()
        {
            // Get latest ped handles
            uint[] handles = APed.HookCreatePedGetPedHandlesList();
            foreach (uint handle in handles)
            {
                // HACK: No player ped
                if (handle == CPlayer.LocalPlayer.Ped.Handle) continue;
                // TODO: Additional checks

                // Ensure handle is only used once. TODO: Performance checks
                if (Pools.PedPool.AtPedHandle((int)handle) == null)
                {
                    CPed ped = new CPed((int)handle);
                }
            }

            // Get latest deleted ped handles and remove them from our list
            handles = APed.HookCreatePedGetPedHandlesDeletedList();
            foreach (uint handle in handles)
            {
                foreach (CPed cPed in Pools.PedPool.GetAll())
                {
                    if (cPed.Handle == handle)
                    {
                        Pools.PedPool.Remove(cPed);
                        break;
                    }
                }
            }

            // Get the latest vehicle handles
            handles = AVehicle.HookCreateVehicleGetVehicleHandlesList();
            foreach (uint handle in handles)
            {
                // Ensure handle is only used once. TODO: Performance checks
                if (Pools.VehiclePool.AtVehicleHandle((int)handle) == null)
                {
                    CVehicle vehicle = new CVehicle((int)handle);
                }
            }

            // Get the latest deleted vehicle handles and remove from our list
            handles = AVehicle.HookCreateVehicleGetVehicleHandlesDeletedList();
            foreach (uint handle in handles)
            {
                foreach (CVehicle cVehicle in Pools.VehiclePool.GetAll())
                {
                    if (cVehicle.Handle == handle)
                    {
                        Pools.VehiclePool.Remove(cVehicle);
                        break;
                    }
                }
            }
        }

        private void Hook()
        {
            APed.HookCreatePed();
            AVehicle.HookCreateVehicle();
            hooked = true;
        }
    }
}
