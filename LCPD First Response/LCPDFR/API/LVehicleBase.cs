namespace LCPD_First_Response.LCPDFR.API
{
    using LCPD_First_Response.Engine.Scripting.Entities;

    /// <summary>
    /// The LCPDFR vehicle base.
    /// </summary>
    public abstract class LVehicleBase
    {
        /// <summary>
        /// The model.
        /// </summary>
        private CModel model;

        /// <summary>
        /// The internal vehicle.
        /// </summary>
        private CVehicle vehicle;

        /// <summary>
        /// Gets or sets the vehicle.
        /// </summary>
        internal CVehicle Vehicle
        {
            get
            {
                return this.vehicle;
            }

            set
            {
                this.vehicle = value;
            }
        }

        public bool AllowSirenWithoutDriver
        {
            set { this.vehicle.AllowSirenWithoutDriver = value; }
        }

        public bool CanBeDamaged
        {
            set { this.vehicle.CanBeDamaged = value; }
        }

        public bool CanBeVisiblyDamaged
        {
            set { this.vehicle.CanBeVisiblyDamaged = value; }
        }

        public bool CanTiresBurst
        {
            set { this.vehicle.CanTiresBurst = value; }
        }

        /// <summary>
        /// Gets or sets the base color of the vehicle.
        /// </summary>
        public GTA.ColorIndex Color
        {
            get { return this.vehicle.Color; }
            set { this.vehicle.Color = value; }
        }

        public GTA.Room CurrentRoom
        {
            get { return this.vehicle.CurrentRoom; }
            set { this.vehicle.CurrentRoom = value; }
        }

        public float CurrentRPM
        {
            get { return this.vehicle.CurrentRPM; }
        }

        public GTA.Vector3 Direction
        {
            get { return this.vehicle.Direction; }
        }

        public float Dirtyness
        {
            get { return this.vehicle.Dirtyness; }
            set { this.vehicle.Dirtyness = value; }
        }

        public GTA.DoorLock DoorLock
        {
            get { return this.vehicle.DoorLock; }
            set { this.vehicle.DoorLock = value; }
        }

        /// <summary>
        /// The current health of the car's engine. 1000 is maximum, 0 is broken, can go below 0 if burning. 
        /// </summary>
        public float EngineHealth
        {
            get { return this.vehicle.EngineHealth; }
            set { this.vehicle.EngineHealth = value; }
        }

        public bool EngineRunning
        {
            get { return this.vehicle.EngineRunning; }
            set { this.vehicle.EngineRunning = value; }
        }

        /// <summary>
        /// Gets or sets the color of some extra features for the vehicle. (stripes, etc.)
        /// </summary>
        public GTA.ColorIndex FeatureColor1
        {
            get { return this.vehicle.FeatureColor1; }
            set { this.vehicle.FeatureColor1 = value; }
        }

        /// <summary>
        /// Gets or sets the color of some extra features for the vehicle. (stripes, etc.)
        /// </summary>
        public GTA.ColorIndex FeatureColor2
        {
            get { return this.vehicle.FeatureColor2; }
            set { this.vehicle.FeatureColor2 = value; }
        }

        public bool FreezePosition
        {
            set { this.vehicle.FreezePosition = value; }
        }

        public bool HazardLightsOn
        {
            set { this.vehicle.HazardLightsOn = value; }
        }

        public float Heading
        {
            get { return this.vehicle.Heading; }
            set { this.vehicle.Heading = value; }
        }

        public int Health
        {
            get { return this.vehicle.Health; }
            set { this.vehicle.Health = value; }
        }

        public bool InteriorLightOn
        {
            set { this.vehicle.InteriorLightOn = value; }
        }

        public bool IsAlive
        {
            get { return this.vehicle.IsAlive; }
        }

        public bool IsDriveable
        {
            get { return this.vehicle.IsDriveable; }
        }

        public bool IsOnAllWheels
        {
            get { return this.vehicle.IsOnAllWheels; }
        }

        public bool IsOnFire
        {
            get { return this.vehicle.IsOnFire; }
            set { this.vehicle.IsOnFire = value; }
        }

        public bool IsOnScreen
        {
            get { return this.vehicle.IsOnScreen; }
        }

        public bool IsRequiredForMission
        {
            get { return this.vehicle.IsRequiredForMission; }
            set { this.vehicle.IsRequiredForMission = value; }
        }

        public bool IsUpright
        {
            get { return this.vehicle.IsUpright; }
        }

        public bool IsUpsideDown
        {
            get { return this.vehicle.IsUpsideDown; }
        }

        public bool LightsOn
        {
            get { return this.vehicle.LightsOn; }
        }

        public CModel Model
        {
            get
            {
                // We don't want to create a new instance all the time
                if (this.model == null)
                {
                    this.model = new CModel(this.vehicle.Model);
                }

                return this.model;
            }
        }

        public string Name
        {
            get { return this.vehicle.Name; }
        }

        public bool NeedsToBeHotwired
        {
            set { this.vehicle.NeedsToBeHotwired = value; }
        }

        public int PassengerSeats
        {
            get { return this.vehicle.PassengerSeats; }
        }

        /// <summary>
        /// The current health of the car's petrol tank. 1000 is maximum, 0 is broken, can go below 0 if burning. 
        /// </summary>
        public float PetrolTankHealth
        {
            get { return this.vehicle.PetrolTankHealth; }
            set { this.vehicle.PetrolTankHealth = value; }
        }

        public bool PreviouslyOwnedByPlayer
        {
            set { this.vehicle.PreviouslyOwnedByPlayer = value; }
        }

        public GTA.Vector3 Position
        {
            get { return this.vehicle.Position; }
            set { this.vehicle.Position = value; }
        }

        public GTA.Vector3 Rotation
        {
            get { return this.vehicle.Rotation; }
            set { this.vehicle.Rotation = value; }
        }

        public GTA.Quaternion RotationQuaternion
        {
            get { return this.vehicle.RotationQuarternion; }
            set { this.vehicle.RotationQuarternion = value; }
        }

        public bool SirenActive
        {
            get { return this.vehicle.SirenActive; }
            set { this.vehicle.SirenActive = value; }
        }

        /// <summary>
        /// Gets or sets the specular color of the vehicle. (for sun reflections, etc.)
        /// </summary>
        public GTA.ColorIndex SpecularColor
        {
            get { return this.vehicle.SpecularColor; }
            set { this.vehicle.SpecularColor = value; }
        }

        public float Speed
        {
            get { return this.vehicle.Speed; }
            set { this.vehicle.Speed = value; }
        }

        public GTA.Vector3 Velocity
        {
            get { return this.vehicle.Velocity; }
            set { this.vehicle.Velocity = value; }
        }

        public bool Visible
        {
            set { this.vehicle.Visible = value; }
        }

        /// <summary>
        /// Applies a force given in world coordinates to the vehicle.
        /// </summary>
        public void ApplyForce(GTA.Vector3 direction, GTA.Vector3 rotation)
        {
            this.vehicle.ApplyForce(direction, rotation);
        }

        /// <summary>
        /// Applies a force given in world coordinates to the vehicle.
        /// </summary>
        public void ApplyForce(GTA.Vector3 direction)
        {
            this.vehicle.ApplyForce(direction);
        }

        /// <summary>
        /// Applies a force that is relative to the current orientation of the vehicle.
        /// Directions: positive X is right, positive Y is front, positive Z is Top 
        /// </summary>
        public void ApplyForceRelative(GTA.Vector3 direction, GTA.Vector3 rotation)
        {
            this.vehicle.ApplyForceRelative(direction, rotation);
        }
        /// <summary>
        /// Applies a force that is relative to the current orientation of the vehicle.
        /// Directions: positive X is right, positive Y is front, positive Z is Top 
        /// </summary>
        public void ApplyForceRelative(GTA.Vector3 direction)
        {
            this.vehicle.ApplyForceRelative(direction);
        }

        public GTA.Blip AttachBlip()
        {
            return this.vehicle.AttachBlip();
        }

        public void BurstTire(GTA.VehicleWheel wheel)
        {
            this.vehicle.BurstTire(wheel);
        }

        public void CloseAllDoors()
        {
            this.vehicle.CloseAllDoors();
        }

        public LPed CreatePedOnSeat(GTA.VehicleSeat seat)
        {
            return new LPed(this.vehicle.CreatePedOnSeat(seat));
        }

        public LPed CreatePedOnSeat(GTA.VehicleSeat seat, CModel model)
        {
            return new LPed(this.vehicle.CreatePedOnSeat(seat, model));
        }

        public LPed CreatePedOnSeat(GTA.VehicleSeat seat, CModel model, GTA.RelationshipGroup relationshipGroup)
        {
            return new LPed(this.vehicle.CreatePedOnSeat(seat, model, relationshipGroup));
        }

        public void Delete()
        {
            this.vehicle.Delete();
        }

        public GTA.value.VehicleDoor Door(GTA.VehicleDoor door)
        {
            return this.vehicle.Door(door);
        }

        public GTA.value.VehicleExtra Extras(int extraID)
        {
            return this.vehicle.Extras(extraID);
        }

        public void EveryoneLeaveVehicle()
        {
            this.vehicle.EveryoneLeaveVehicle();
        }

        public bool Exists()
        {
            return this.vehicle.Exists();
        }

        public void Explode()
        {
            this.vehicle.Explode();
        }

        public void FixTire(GTA.VehicleWheel wheel)
        {
            this.vehicle.FixTire(wheel);
        }

        public GTA.VehicleSeat GetFreeSeat()
        {
            return this.vehicle.GetFreeSeat();
        }

        public GTA.VehicleSeat GetFreePassengerSeat()
        {
            return this.vehicle.GetFreePassengerSeat();
        }

        public LPed GetPedOnSeat(GTA.VehicleSeat seat)
        {
            return new LPed(this.vehicle.GetPedOnSeat(seat));
        }

        /// <summary>
        /// Returns the given position as directional offset using the following directions:
        /// positive X is right, positive Y is in front, positive Z is on Top 
        /// </summary>
        public GTA.Vector3 GetOffset(GTA.Vector3 worldPosition)
        {
            return this.vehicle.GetOffset(worldPosition);
        }

        /// <summary>
        /// Returns the given offset in world coordinates.
        /// Directions: positive X is right, positive Y is front, positive Z is Top 
        /// </summary>
        public GTA.Vector3 GetOffsetPosition(GTA.Vector3 offset)
        {
            return this.vehicle.GetOffsetPosition(offset);
        }

        public bool IsSeatFree(GTA.VehicleSeat seat)
        {
            return this.vehicle.IsSeatFree(seat);
        }

        public bool IsTireBurst(GTA.VehicleWheel wheel)
        {
            return this.vehicle.IsTireBurst(wheel);
        }

        public bool IsTouching(LVehicle vehicle)
        {
            return this.vehicle.IsTouching(vehicle.CVehicle);
        }

        public void MakeProofTo(bool bullets, bool fire, bool explosions, bool collisions, bool meleeAttacks)
        {
            this.vehicle.MakeProofTo(bullets, fire, explosions, collisions, meleeAttacks);
        }

        public void NoLongerNeeded()
        {
            this.vehicle.NoLongerNeeded();
        }

        public void PassengersLeaveVehicle()
        {
            this.vehicle.PassengersLeaveVehicle();
        }

        public void PassengersLeaveVehicle(bool immediately)
        {
            this.vehicle.PassengersLeaveVehicle(immediately);
        }

        public void PlaceOnGroundProperly()
        {
            this.vehicle.PlaceOnGroundProperly();
        }

        public void PlaceOnNextStreetProperly()
        {
            this.vehicle.PlaceOnNextStreetProperly();
        }

        public void Repair()
        {
            this.vehicle.Repair();
        }

        public void SoundHorn(int duration)
        {
            this.vehicle.SoundHorn(duration);
        }

        public void Wash()
        {
            this.vehicle.Wash();
        }

        public static implicit operator GTA.Vehicle(LVehicleBase vehicleBase)
        {
            return (GTA.Vehicle)vehicleBase.vehicle;
        }
    }
}