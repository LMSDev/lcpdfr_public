namespace LCPD_First_Response.LCPDFR.Scripts
{
    using System.Collections.Generic;

    using GTA;

    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Native;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.Input;

    using Timer = LCPD_First_Response.Engine.Timers.Timer;

    /// <summary>
    /// AreaBlocker class used to prevent peds to enter a specific vehicles, can be used to block the area of an arrest or pullover.
    /// </summary>
    internal static class AreaBlocker
    {
        /// <summary>
        /// The blocked areas.
        /// </summary>
        private static List<BlockedAreaInfo> blockedAreas;

        /// <summary>
        /// The timer.
        /// </summary>
        private static Timer timer;

        /// <summary>
        /// Initializes static members of the <see cref="AreaBlocker"/> class. 
        /// </summary>
        public static void Initialize()
        {
            blockedAreas = new List<BlockedAreaInfo>();
            timer = new Timer(20, Process);
            timer.Start();
        }

        /// <summary>
        /// Adds a new blocked area around <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="showBlip">Whether or not a blip should be drawn.</param>
        public static void AddBlockedArea(Vector3 position, float radius, bool showBlip)
        {
            BlockedAreaInfo blockedAreaInfo = new BlockedAreaInfo(position, radius, showBlip);
            blockedAreas.Add(blockedAreaInfo);

            Stats.UpdateStat(Stats.EStatType.AreaTrafficBlocked, 1);
        }

        /// <summary>
        /// Creates a blip for an area with the given radius. This blip looks like the police search area blip.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="radius">The radius.</param>
        /// <returns>The blip.</returns>
        public static Blip CreateAreaBlip(Vector3 position, float radius, BlipColor color = BlipColor.Yellow)
        {
            AdvancedHookManaged.ABlip blip = AdvancedHookManaged.ABlip.AddBlipWithRadius(position.X, position.Y, radius);

            if (blip != null)
            {
                Blip b = new Blip((int)blip.Get());
                b.Color = color;
                return b;
            }

            return null;
        }

        /// <summary>
        /// Deletes all blocked areas.
        /// </summary>
        public static void FlushBlockedAreas()
        {
            foreach (BlockedAreaInfo blockedAreaInfo in blockedAreas)
            {
                if (blockedAreaInfo.ShowBlip && blockedAreaInfo.Blip != null)
                {
                    blockedAreaInfo.RemoveBlip();
                }
            }

            blockedAreas.Clear();
        }

        /// <summary>
        /// Removes the blocked area at <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        public static void RemoveBlockedArea(Vector3 position)
        {
            for (int i = 0; i < blockedAreas.Count; i++)
            {
                BlockedAreaInfo blockedAreaInfo = blockedAreas[i];
                if (blockedAreaInfo.Position == position)
                {
                    if (blockedAreaInfo.ShowBlip && blockedAreaInfo.Blip != null)
                    {
                        blockedAreaInfo.RemoveBlip();
                    }

                    foreach (CVehicle vehicle in CVehicle.GetVehiclesAround(blockedAreaInfo.Radius, EVehicleSearchCriteria.DriverOnly | EVehicleSearchCriteria.NoPlayersLastVehicle, blockedAreaInfo.Position))
                    {
                        if (vehicle.Exists() && vehicle.VehicleGroup != EVehicleGroup.Police)
                        {
                            if (vehicle.Driver.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCarSetTempAction))
                            {
                                if (!vehicle.Driver.IsRequiredForMission)
                                {
                                    // Properly reset all drivers by assigning the crusing task for five seconds
                                    vehicle.Driver.Task.ClearAll();
                                    vehicle.Driver.Task.CruiseWithVehicle(vehicle, 5, true);
                                    CVehicle vehicle1 = vehicle;
                                    DelayedCaller.Call(
                                        delegate
                                            {
                                                if (vehicle1.Exists() && vehicle1.Driver != null && vehicle1.Driver.Exists())
                                                {
                                                    vehicle1.Driver.NoLongerNeeded();
                                                }
                                            }, 
                                            5000);
                                }
                            }
                        }
                    }

                    blockedAreas.Remove(blockedAreaInfo);
                    break;
                }
            }
        }

        /// <summary>
        /// Checks whether the blocked area is still effective.
        /// </summary>
        /// <param name="parameter">
        /// The parameter.
        /// </param>
        private static void Process(object parameter)
        {
            if (KeyHandler.IsKeyDown(ELCPDFRKeys.BlockArea))
            {
                // Limit to only area for now
                if (blockedAreas.Count > 0)
                {
                    FlushBlockedAreas();
                }
                else
                {
                    AddBlockedArea(CPlayer.LocalPlayer.Ped.Position, 15f, true);
                }
            }

            for (int i = 0; i < blockedAreas.Count; i++)
            {
                BlockedAreaInfo blockedAreaInfo = blockedAreas[i];

                foreach (CVehicle vehicle in CVehicle.GetVehiclesAround(blockedAreaInfo.Radius, EVehicleSearchCriteria.DriverOnly | EVehicleSearchCriteria.NoPlayersLastVehicle, blockedAreaInfo.Position))
                {
                    if (vehicle.Exists() && !vehicle.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsEmergencyServicesVehicle) && vehicle.Driver.PedGroup == EPedGroup.Pedestrian)
                    {
                        if (!vehicle.Driver.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCarSetTempAction))
                        {
                            vehicle.Driver.Task.CarTempAction(ECarTempActionType.SlowDownHard2, 5000);
                        }
                    }
                }

                if (blockedAreaInfo.Position.DistanceTo(CPlayer.LocalPlayer.Ped.Position) > blockedAreaInfo.Radius + 5)
                {
                    RemoveBlockedArea(blockedAreaInfo.Position);
                }
            }
        }

        /// <summary>
        /// Stores information about a blocked area.
        /// </summary>
        private class BlockedAreaInfo
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="BlockedAreaInfo"/> class.
            /// </summary>
            /// <param name="position">
            /// The position.
            /// </param>
            /// <param name="radius">
            /// The radius.
            /// </param>
            /// <param name="showBlip">
            /// The show blip.
            /// </param>
            public BlockedAreaInfo(Vector3 position, float radius, bool showBlip)
            {
                this.Position = position;
                this.Radius = radius;
                this.ShowBlip = showBlip;

                if (showBlip)
                {
                    this.AddBlip();
                }
            }

            /// <summary>
            /// Gets the blip.
            /// </summary>
            public Blip Blip { get; private set; }

            /// <summary>
            /// Gets the position of the area.
            /// </summary>
            public Vector3 Position { get; private set; }

            /// <summary>
            /// Gets the radius of the area.
            /// </summary>
            public float Radius { get; private set; }

            /// <summary>
            /// Gets a value indicating whether a blip should be drawn.
            /// </summary>
            public bool ShowBlip { get; private set; }

            /// <summary>
            /// Deletes the blip.
            /// </summary>
            public void RemoveBlip()
            {
                this.Blip.Delete();
            }
            
            /// <summary>
            /// Adds the  blip.
            /// </summary>
            private void AddBlip()
            {
                this.Blip = CreateAreaBlip(this.Position, this.Radius);
            }
        }
    }
}