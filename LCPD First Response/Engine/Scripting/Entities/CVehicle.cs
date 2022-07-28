// TODO: Add CVehicle.Available property to know if it's in use?

namespace LCPD_First_Response.Engine.Scripting.Entities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using AdvancedHookManaged;

    using global::LCPDFR.Networking;

    using GTA;

    using LCPD_First_Response.Engine.IO;
    using LCPD_First_Response.Engine.Networking;
    using LCPD_First_Response.Engine.Scripting.Native;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;

    using Lidgren.Network;

    /// <summary>
    /// Describes special flags of the vehicle.
    /// </summary>
    [Flags]
    internal enum EVehicleFlags
    {
        /// <summary>
        /// No flags.
        /// </summary>
        None = 0x0,


        /// <summary>
        /// Whether the player gave the vehicle a ticket already.
        /// </summary>
        GotTicket = 0x1,

        /// <summary>
        /// Whether the trunk has been checked already.
        /// </summary>
        TrunkChecked = 0x2,

        /// <summary>
        /// Whether the vehicle was scanned by ANPR.
        /// </summary>
        WasScanned = 0x4,

        /// <summary>
        /// Whether the vehicle is allowed to bypass a checkpoint.
        /// </summary>
        CanBypassCheckpoint = 0x8,

        /// <summary>
        /// Whether the vehicle is ordered to stop by the player.
        /// </summary>
        ForcedToStop = 0x10,

        /// <summary>
        /// Whether pullover is disabled on this vehicle.
        /// </summary>
        DisablePullover = 0x20,
    }

    /// <summary>
    /// The network messages for vehicles.
    /// </summary>
    internal enum EVehicleNetworkMessages
    {
        AttachBlip,
        RemoveBlip,
        SetBlipMode,
    }

    internal class CVehicle : VehicleBase
    {
        /// <summary>
        /// The license number.
        /// </summary>
        private LicenseNumber licenseNumber;

        public AVehicle AVehicle { get; private set; }
        public GTA.Blip Blip { get; private set; }
        public bool CanGoAgainstTraffic
        {
            set
            {
                Natives.SetCarCanGoAgainstTraffic(this, value);
            }
        }

        /// <summary>
        /// Sets a value indicating whether the vehicle has collisions.
        /// </summary>
        public bool Collision
        {
            set
            {
                Natives.SetCarCollision(this, value);
            }
        }

        public CPed Driver
        {
            get
            {
                return GetPedOnSeat(GTA.VehicleSeat.Driver);
            }
        }

        /// <summary>
        /// Gets or sets the vehicle flags.
        /// </summary>
        public EVehicleFlags Flags { get; set; }

        public bool HasBlip
        {
            get { return this.Blip != null && this.Blip.Exists(); }
        }
        public bool HasDriver
        {
            get
            {
                bool isSeatFree = this.IsSeatFree(VehicleSeat.Driver);
                if (!isSeatFree)
                {
                    CPed ped = this.GetPedOnSeat(VehicleSeat.Driver);
                    if (ped != null && ped.Exists())
                    {
                        return true;
                    }
                    else
                    {
                        Log.Warning("HasDriver: Driver seat is occupied, but ped is invalid" , this);
                        return false;
                    }
                }

                return false;
            }
        }
        //public GTA.Vehicle InternalVehicle { get; private set; }

        public bool HasRoof
        {
            get
            {
                return Natives.DoesCarHaveRoof(this);
            }
        }

        /// <summary>
        /// Sets a value indicating whether the helicopter searchlight is on.
        /// </summary>
        public bool HeliSearchlightOn
        {   
            set
            {
                this.AVehicle.HeliSearchlightOn = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the vehicle is big.
        /// </summary>
        public bool IsBig
        {
            get
            {
                return Natives.IsBigVehicle(this);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the vehicle is in water.
        /// </summary>
        public bool IsInWater
        {
            get
            {
                return Natives.IsCarInWater(this);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the vehicle is stopped.
        /// </summary>
        public bool IsStopped 
        { 
            get { return Natives.IsCarStopped(this); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the vehicle is a suspect transporter.
        /// </summary>
        public bool IsSuspectTransporter { get; set; }

        /// <summary>
        /// Gets the license number of the vehicle.
        /// </summary>
        public LicenseNumber LicenseNumber
        {
            get
            {
                if (this.licenseNumber == null)
                {
                    this.licenseNumber = new LicenseNumber();
                }

                return this.licenseNumber;
            }
        }

        public int NetworkID
        {
            get { return Natives.GetNetworkIDFromVehicle(this); }
        }

        public Scripting.Entities.SirenManager SirenManager { get; private set; }

        public EVehicleGroup VehicleGroup { get; private set; }

        public CVehicle(CModel model, GTA.Vector3 position, EVehicleGroup type)
        {
            base.vehicle = GTA.World.CreateVehicle(model, position);
            if (Exists())
            {
                this.HasBeenCreatedByUs = true;
                this.VehicleGroup = type;

                this.Initialize();
            }
        }

        public CVehicle(int handle)
        {
            base.vehicle = new GTA.Vehicle(handle); 
            if (Exists())
            {
                // The creation doesn't add the vehicle to shdn's contentcache which makes the vehicle not available for all functions that retun a vehicle such as GTA.Ped.CurrentVehicle
                GTA.ContentCache.AddVehicle(base.vehicle);

                this.VehicleGroup = EVehicleGroup.Unknown;
                Initialize();

                // The vehicle may already contain a ped (peds created inside vehicles in the street aren't created using the normal created ped function, that's why our hook doesn't get them)
                // If so, add them to the ped pool
                if (this.HasDriver)
                {
                    CPed[] peds = GetAllPedsInVehicle(true);
                }
            }
        }

        private void Initialize()
        {
            this.SetHandle(base.vehicle.pHandle);
            this.AVehicle = new AVehicle((uint)this.Handle); // TODO: Make AdvancedHook accept int32 here
            
            if (this.VehicleGroup == EVehicleGroup.Unknown)
            {
                // TODO: Use AdvancedHook to check if vehicle is police vehicle (0x20 police flag, see ProcessCopCarBlips)
                //if ((GTA.Model)base.Model == "POLICE" || (GTA.Model)base.Model == "POLICE2" || (GTA.Model)base.Model == "POLICE3" || (GTA.Model) base.Model == "FBI")
                //{
                    if (this.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsCopCar))
                    {
                        this.VehicleGroup = EVehicleGroup.Police;
                    }

                    if (this.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsCopBoat))
                    {
                        this.VehicleGroup = EVehicleGroup.Police;
                    }
                //}
            }
            if (this.VehicleGroup == EVehicleGroup.Police)
            {
                this.AddSirenManager();
                if (this.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsCopBoat))
                {
                    this.SirenManager.AllowSirenForNonSirenModel = true;
                }
                // TODO: Register to cop manager?
            }

            Pools.VehiclePool.Add(this);

            if (Main.NetworkManager.IsNetworkSession && this.HasBeenCreatedByUs)
            {
                int networkID = this.NetworkID;
                Natives.SetNetworkIDCanMigrate(networkID, false);
                Natives.SetNetworkIDExistsOnAllMachines(networkID, true);

                if (Main.NetworkManager.CanSendData)
                {
                    Main.NetworkManager.SendMessageWithNetworkID("Networking", EGenericNetworkMessages.NewNetworkEntityIDUsed, this.NetworkID);
                    Log.Debug("CVehicle: Synced", this);
                }
            }

            // Set a default content manager for entities created by us
            if (this.HasBeenCreatedByUs)
            {
                Plugins.ContentManager.DefaultContentManager.AddVehicle(this);
            }

            new Events.EventNewVehicleCreated(this);
        }

        /// <summary>
        /// Adds a siren manager instance to the vehicle instance.
        /// </summary>
        public void AddSirenManager()
        {
            this.SirenManager = new SirenManager(this);
        }

        public GTA.Blip AttachBlip(bool sync = true, bool forceBlip = false)
        {
            if (this.HasBlip && !forceBlip) return this.Blip;
            this.Blip = base.AttachBlip();
            if (sync)
            {
                // If host, sync with all clients
                if (Main.NetworkManager.IsNetworkSession && Main.NetworkManager.IsHost && Main.NetworkManager.CanSendData)
                {
                    Main.NetworkManager.SendMessageWithNetworkID("CVehicle", EVehicleNetworkMessages.AttachBlip, this.NetworkID);
                }
            }
            return this.Blip;
        }

        public new void Delete()
        {
            base.Delete();
            Pools.VehiclePool.Remove(this);
        }

        public void DeleteBlip(bool sync = true)
        {
            if (this.HasBlip)
            {
                this.Blip.Delete();

                if (sync)
                {
                    // If host, sync with all clients
                    if (Main.NetworkManager.IsNetworkSession && Main.NetworkManager.IsHost && Main.NetworkManager.CanSendData)
                    {
                        Main.NetworkManager.SendMessageWithNetworkID("CVehicle", EVehicleNetworkMessages.RemoveBlip, this.NetworkID);
                    }
                }
            }
        }

        public new bool Exists()
        {
            return base.vehicle != null && base.vehicle.Exists();
        }

        /// <summary>
        /// Flashes the blip.
        /// </summary>
        /// <param name="value">
        /// Whether the blip should flash or not.
        /// </param>
        public void FlashBlip(bool value)
        {
            Natives.FlashBlip(this.Blip, value);
        }

        private CPed[] GetAllPedsInVehicle(bool vehicleWasJustCreated)
        {
            List<CPed> peds = new List<CPed>();
            foreach (GTA.VehicleSeat seat in Enum.GetValues(typeof(GTA.VehicleSeat)))
            {
                if (seat == GTA.VehicleSeat.None || seat == GTA.VehicleSeat.AnyPassengerSeat)
                {
                    continue;
                }

                // WARNING: isSeatFree returns true for vehicles that have only 2 seats (e.g. feltzer) for left and right rear, I don't know why
                // To prevent this we use the number of maximum seats. Driver = -1, Right front = 0, Left rear = 1, Right rear = 2.
                if (this.PassengerSeats - 1 >= ((int)seat))
                {
                    if (!this.IsSeatFree(seat))
                    {
                        if (vehicleWasJustCreated)
                        {
                            // Get gta ped here, because there is no CPed instance of this ped yet
                            GTA.Ped ped = this.vehicle.GetPedOnSeat(seat);
                            if (ped != null && ped.Exists())
                            {
                                if (ped.pHandle == GTA.Game.LocalPlayer.Character.pHandle)
                                {
                                    continue;
                                }

                                // Ensure handle is only used once. TODO: Performance checks
                                if (Pools.PedPool.AtPedHandle(ped.pHandle) == null)
                                {
                                    peds.Add(new CPed(ped.pHandle));
                                }
                            }
                        }
                        else
                        {
                            CPed ped = this.GetPedOnSeat(seat);
                            if (ped != null && ped.Exists())
                            {
                                peds.Add(ped);
                            }
                        }
                    }
                }
            }
            return peds.ToArray();
        }

        public CPed[] GetAllPedsInVehicle()
        {
            return GetAllPedsInVehicle(false);
        }

        public VehicleSeat GetSeatFromDoor(VehicleDoor door)
        {
            if (door == VehicleDoor.LeftFront)
            {
                return VehicleSeat.Driver;
            }
            else if (door == VehicleDoor.LeftRear)
            {
                return VehicleSeat.LeftRear;
            }
            else if (door == VehicleDoor.RightFront)
            {
                return VehicleSeat.RightFront;
            }

            return VehicleSeat.RightRear;
        }

        public VehicleDoor GetDoorFromSeat(VehicleSeat seat)
        {
            if (seat == VehicleSeat.Driver)
            {
                return VehicleDoor.LeftFront;
            }
            else if (seat == VehicleSeat.LeftRear)
            {
                return VehicleDoor.LeftRear;
            }
            else if (seat == VehicleSeat.RightFront)
            {
                return VehicleDoor.RightFront;
            }

            return VehicleDoor.RightRear;
        }

        /// <summary>
        /// Gets the side of the street the vehicle is located at. Relative to the vehicle's heading.
        /// </summary>
        /// <returns>The side of the street.</returns>
        public EStreetSide GetSideOfStreetVehicleIsAt()
        {
            // Get middle of the street
            Vector3 position = Vector3.Zero;
            float heading = 0f;
            GetClosestCarNodeWithHeading(this.vehicle.Position, ref position, ref heading);

            // Make vehicle a line
            Vector3 point0 = this.vehicle.GetOffsetPosition(new Vector3(0, 5, 0));
            Vector3 point1 = this.vehicle.Position;
            float value = (point1.X - point0.X) * (position.Y - point0.Y) - (point1.Y - point0.Y) * (position.X - point0.X);

            if (value < 0)
            {
                return EStreetSide.Right;
            }
            else
            {
                return EStreetSide.Left;
            }
        }

        /// <summary>
        /// Stabilizes the vehicle, if helicopter.
        /// </summary>
        public void Stabilize()
        {
            Natives.SetHeliStabiliser((GTA.Vehicle)this);
        }

        public void RemoveWindow(VehicleWindow vehicleWindow)
        {
            Natives.RemoveCarWindow((GTA.Vehicle)this, vehicleWindow);
        }

        public override string ComponentName
        {
            get { return "CVehicle"; }
        }

        // Operators
        public static implicit operator GTA.Vector3(CVehicle vehicle)
        {
            return vehicle.Position;
        }

        // Statics
        public static CVehicle CreateBoatAroundPosition(CModel model, EVehicleGroup vehicleGroup, Vector3 position, float minimumDistance, Vector3 posToDistanceTo, bool forceStaticLocations)
        {
            Vector3 startPos = position;
            Vector3 spawnPos = Vector3.Zero;
            CVehicle vehicle = null;
            int iterations = 0;
            float minDistance = minimumDistance;
            while (iterations < 100 && !forceStaticLocations)
            {
                Vector3 pos = startPos.Around(Common.GetRandomValue(0, 80));
                GTA.Native.Pointer ptr = typeof(float);
                GTA.Native.Function.Call("GET_WATER_HEIGHT", pos.X, pos.Y, pos.Z, ptr);
                float height = (float)ptr.Value;
                Vector3 gh = World.GetGroundPosition(pos, GroundType.Lowest);
                if (height != 0.0f && gh.Z == 0.0f)
                {
                    if (pos.DistanceTo(posToDistanceTo) > minDistance
                        || (!GUI.Gui.IsPositionOnScreen(pos) && pos.DistanceTo(posToDistanceTo) > minDistance / 2))
                    {
                        spawnPos = pos;
                        break;
                    }
                }

                iterations++;
            }

            // If we failed to dynamically find a spawn point, most likely because the game hasn't loaded the further away areas so we can't check,
            // we fall back to static data.
            while (spawnPos == Vector3.Zero)
            {
                Log.Debug("CreateBoatAroundPosition: Using static data", "CVehicle");

                foreach (Vector3 waterPosition in FindClosestsWaterPositionStatic(position, 0))
                {
                    if (waterPosition.DistanceTo(posToDistanceTo) > minimumDistance)
                    {
                        spawnPos = waterPosition;
                        break;
                    }
                }
            }

            if (spawnPos != Vector3.Zero)
            {
                vehicle = new CVehicle(model, spawnPos, vehicleGroup);
                if (vehicle.Exists())
                {
                    vehicle.NoLongerNeeded();
                    vehicle.Visible = false;

                    TimerLoop timer = new TimerLoop(
                        20,
                        delegate(TimerLoop loop, object[] parameter)
                        {
                            if (vehicle.Exists())
                            {
                                if (vehicle.IsInWater)
                                {
                                    DelayedCaller.Call(
                                        delegate(object[] objects)
                                        {
                                            if (!vehicle.Exists()) return;
                                       
                                            vehicle.Visible = true;
                                            GTA.Native.Function.Call("SET_VEHICLE_ALPHA", (GTA.Vehicle)vehicle, 0);
                                            foreach (CPed ped in vehicle.GetAllPedsInVehicle())
                                            {
                                                if (ped.Exists())
                                                {
                                                    ped.Visible = true;
                                                    GTA.Native.Function.Call("SET_PED_ALPHA", (GTA.Ped)ped, 0);
                                                }
                                            }
                                        },
                                        250);
                                    loop.Stop();
                                }
                            }
                        });

                    timer.Start();
                }
            }

            return vehicle;
        }

        public static IEnumerable<Vector3> EnumerateStaticWaterPositions(GTA.Vector3 startPosition, float minimumDistance)
        {
            string content = Properties.Resources.boatpos;
            Legacy.DataFile dataFile = new Legacy.DataFile(content);
            Legacy.DataSet dataSet = dataFile.DataSets[0];

            foreach (Legacy.Tag tag in dataSet.Tags)
            {
                if (tag.Name != "BOAT")
                {
                    continue;
                }

                Vector3 carPos = Legacy.FileParser.ParseVector3(tag.GetAttributeByName("CARPOS").Value);
                if (carPos.DistanceTo(startPosition) > minimumDistance)
                {
                    yield return carPos;
                }
            }
        }

        public static CVehicle FromNetworkID(int networkID)
        {
            GTA.Vehicle vehicle = null;
            Natives.GetVehicleFromNetworkID(networkID, ref vehicle);
            if (vehicle != null && vehicle.Exists())
            {
                return Pools.VehiclePool.GetVehicleFromPool(vehicle);
            }
            return null;
        }

        /// <summary>
        /// Gets the closest vehicle using the <paramref name="vehicleSearchCriteria"/>.
        /// </summary>
        /// <param name="vehicleSearchCriteria">The search criteria.</param>
        /// <param name="distance">The distance.</param>
        /// <param name="position">The position.</param>
        /// <returns>The closest vehicle.</returns>
        public static CVehicle GetClosestVehicle(EVehicleSearchCriteria vehicleSearchCriteria, float distance, Vector3 position)
        {
            CVehicle closestVehicle = null;
            float closestDistance = float.MaxValue;
            foreach (CVehicle vehicle in GetVehiclesAround(distance, vehicleSearchCriteria, position))
            {
                if (!vehicle.Exists())
                {
                    continue;
                }

                float distanceTo = vehicle.Position.DistanceTo(position);
                if (distanceTo < closestDistance)
                {
                    closestVehicle = vehicle;
                    closestDistance = distanceTo;
                }
            }

            return closestVehicle;
        }

        /// <summary>
        /// Gets the closest vehicle using the <paramref name="vehicleSearchCriteria"/>.
        /// </summary>
        /// <param name="vehicleSearchCriteria">The search criteria.</param>
        /// <param name="distance">The distance.</param>
        /// <param name="position">The position.</param>
        /// <param name="blacklistedVehicles">The blacklisted vehicles.</param>
        /// <returns>The closest vehicle.</returns>
        public static CVehicle GetClosestVehicle(EVehicleSearchCriteria vehicleSearchCriteria, float distance, Vector3 position, IEnumerable<CVehicle> blacklistedVehicles)
        {
            CVehicle closestVehicle = null;
            float closestDistance = float.MaxValue;
            foreach (CVehicle vehicle in GetVehiclesAround(distance, vehicleSearchCriteria, position))
            {
                if (!vehicle.Exists())
                {
                    continue;
                }

                if (blacklistedVehicles.Contains(vehicle))
                {
                    continue;
                }

                float distanceTo = vehicle.Position.DistanceTo(position);
                if (distanceTo < closestDistance)
                {
                    closestVehicle = vehicle;
                    closestDistance = distanceTo;
                }
            }

            return closestVehicle;
        }

        /// <summary>
        /// Gets all vehicles around <paramref name="position"/> using the <paramref name="vehicleSearchCriteria"/>.
        /// </summary>
        /// <param name="distance">The distance.</param>
        /// <param name="vehicleSearchCriteria">The search criteria.</param>
        /// <param name="position">The position.</param>
        /// <returns>Vehicles array.</returns>
        public static CVehicle[] GetVehiclesAround(float distance, EVehicleSearchCriteria vehicleSearchCriteria, Vector3 position)
        {
            List<CVehicle> vehicles = new List<CVehicle>();
            foreach (CVehicle vehicle in Pools.VehiclePool.GetAll())
            {
                if (vehicle.Exists())
                {
                    if (vehicle.Position.DistanceTo(position) < distance)
                    {
                        // TODO: Available flag?
                        if (vehicle.IsSuspectTransporter) continue;
                        if (vehicle.HasOwner) continue;

                        // If vehicle is not alive or driveable and broken flag is not set, skip
                        if (!vehicle.IsAlive || !vehicle.IsDriveable)
                        {
                            if (!vehicleSearchCriteria.HasFlag(EVehicleSearchCriteria.Broken))
                            {
                                continue;
                            }
                        }

                        // Better only use vehicles on all wheels
                        if (!vehicle.IsOnAllWheels) continue;

                        if (vehicleSearchCriteria.HasFlag(EVehicleSearchCriteria.CopOnly)) if (vehicle.VehicleGroup != EVehicleGroup.Police) continue;
                        if (vehicleSearchCriteria.HasFlag(EVehicleSearchCriteria.NoCop)) if (vehicle.VehicleGroup == EVehicleGroup.Police) continue; ;
                        if (vehicleSearchCriteria.HasFlag(EVehicleSearchCriteria.DriverOnly)) if (!vehicle.HasDriver) continue;
                        if (vehicleSearchCriteria.HasFlag(EVehicleSearchCriteria.NoCarsWithCopDriver))
                        {
                            if (vehicle.HasDriver)
                            {
                                if (vehicle.Driver.PedGroup == EPedGroup.Cop)
                                {
                                    continue;
                                }
                            }
                        }
                        if (vehicleSearchCriteria.HasFlag(EVehicleSearchCriteria.FreeRearSeatOnly)) if (!vehicle.IsSeatFree(VehicleSeat.LeftRear) && !vehicle.IsSeatFree(VehicleSeat.RightRear)) continue;
                        if (vehicleSearchCriteria.HasFlag(EVehicleSearchCriteria.NoDriverOnly)) if (vehicle.HasDriver) continue;
                        if (vehicleSearchCriteria.HasFlag(EVehicleSearchCriteria.StoppedOnly)) if (!vehicle.IsStopped) continue;
                        if (vehicleSearchCriteria.HasFlag(EVehicleSearchCriteria.NoPlayersLastVehicle))
                        {
                            if (vehicle == CPlayer.LocalPlayer.Ped.LastVehicle)
                            {
                                continue;
                            }
                        }

                        vehicles.Add(vehicle);
                    }
                }
            }
            var lengths = from element in vehicles
                          orderby element.Position.DistanceTo2D(position)
                          select element;
            return lengths.ToArray();
        }

        /// <summary>
        /// Returns the closest car node with heading close to <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="closestNode">Vector to store closest node.</param>
        /// <param name="closestHeading">Floating point value to store closest node's heading.</param>
        /// <returns>True on success, otherwise false.</returns>
        public static bool GetClosestCarNodeWithHeading(GTA.Vector3 position, ref GTA.Vector3 closestNode, ref float closestHeading)
        {
            return Natives.GetClosestCarNodeWithHeading(position, ref closestNode, ref closestHeading);
        }

        public static Vector3 FindWaterPosition(GTA.Vector3 startPosition, float minimumDistance)
        {
            Vector3 spawnPosition = startPosition;
            float distance = 0;
            Game.LoadAllPathNodes = true;
            List<Vector3> positions = new List<Vector3>();

            while (true)
            {
                distance += 5;

                // Twice per distance.
                for (int i = 0; i < 2; i++)
                {
                    Vector3 tempPos = spawnPosition.Around(distance);
                    Vector3 nextRoad = World.GetNextPositionOnStreet(tempPos);
                    if (nextRoad != Vector3.Zero)
                    {
                        float d = nextRoad.DistanceTo2D(tempPos);

                        if (d > 180)
                        {
                            if (World.GetZoneName(tempPos) == "LIBERTY")
                            {
                                positions.Add(tempPos);
                            }
                        }
                    }
                }

                if (distance > 800)
                {
                    break;
                }
            }

            if (positions.Count > 0)
            {
                // Get closest
                var dict = from entry in positions
                           orderby entry.DistanceTo(startPosition) ascending
                           select entry;
                foreach (Vector3 vector3 in dict)
                {
                    if (vector3.DistanceTo(startPosition) > minimumDistance)
                    {
                        spawnPosition = vector3;
                        break;
                    }
                }

            }
            else
            {
                Log.Warning("Failed to find position in water", "CVehicle");
                spawnPosition = new Vector3(431, 509, 0);
            }

            return spawnPosition;
        }

        public static Vector3 FindClosestWaterPositionStatic(GTA.Vector3 startPosition, float minimumDistance)
        {
            Vector3 closestPos = Vector3.Zero;
            float closestDist = float.MaxValue;

            foreach (Vector3 waterPosition in EnumerateStaticWaterPositions(startPosition, minimumDistance))
            {
                if (waterPosition.DistanceTo(startPosition) < closestDist)
                {
                    closestDist = waterPosition.DistanceTo(startPosition);
                    closestPos = waterPosition;
                }
            }

            return closestPos;
        }

        public static Vector3[] FindClosestsWaterPositionStatic(GTA.Vector3 startPosition, float minimumDistance)
        {
            Vector3 closestPos = Vector3.Zero;
            float closestDist = float.MaxValue;
            List<Vector3> list = EnumerateStaticWaterPositions(startPosition, minimumDistance).ToList();
            var lengths = from element in list
                          orderby element.DistanceTo2D(startPosition)
                          select element;
            return lengths.ToArray();
        }

        /// <summary>
        /// Sets the target of the heli searchlight.
        /// </summary>
        /// <param name="ped">The ped. Use null to clear.</param>
        public static void SetHeliSearchlightTarget(CPed ped)
        {
            if (ped == null)
            {
                AVehicle.ClearHeliSearchlightTargetPed();
                return;
            }

            AVehicle.SetHeliSearchlightTargetPed(ped.APed);
        }
    }

    /// <summary>
    /// The side of the street the vehicle is located at (relative to the vehicle's heading).
    /// </summary>
    internal enum EStreetSide
    {
        /// <summary>
        /// No side.
        /// </summary>
        None,

        /// <summary>
        /// Left side.
        /// </summary>
        Left, 

        /// <summary>
        /// Right side.
        /// </summary>
        Right,
    }

    enum EVehicleGroup
    {
        Unknown,
        Normal,
        Police,
    }
}
