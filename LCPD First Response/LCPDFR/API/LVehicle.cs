namespace LCPD_First_Response.LCPDFR.API
{
    using System;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting.Entities;

    /// <summary>
    /// The LCPDFRVehicle, which provides access to LCPDFR specific functions.
    /// </summary>
    public class LVehicle : LVehicleBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LVehicle"/> class.
        /// </summary>
        /// <param name="position">
        /// The position.
        /// </param>
        /// <param name="model">
        /// The model.
        /// </param>
        public LVehicle(GTA.Vector3 position, string model)
        {
            this.Vehicle = new CVehicle(model, position, EVehicleGroup.Unknown);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LVehicle"/> class.
        /// </summary>
        /// <param name="vehicle">
        /// The vehicle.
        /// </param>
        public LVehicle(LVehicle vehicle)
        {
            CVehicle customVehicle = Pools.VehiclePool.GetVehicleFromPool(vehicle);
            this.Vehicle = customVehicle;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LVehicle"/> class.
        /// </summary>
        /// <param name="vehicle">
        /// The vehicle.
        /// </param>
        internal LVehicle(CVehicle vehicle)
        {
            this.Vehicle = vehicle;
        }

        /// <summary>
        /// Gets the blip assigned to the vehicle.
        /// </summary>
        public GTA.Blip Blip
        {
            get
            {
                return this.Vehicle.Blip;
            }
        }

        public bool DisablePullover
        {
            get
            {
                return this.Vehicle.Flags.HasFlag(EVehicleFlags.DisablePullover);
            }

            set
            {
                if (value)
                {
                    this.Vehicle.Flags |= EVehicleFlags.DisablePullover;
                }
                else
                {
                    this.Vehicle.Flags &= ~EVehicleFlags.DisablePullover;
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="GTA.Vehicle"/> representation.
        /// </summary>
        public GTA.Vehicle GVehicle
        {
            get
            {
                return (GTA.Vehicle)this.Vehicle;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the vehicle has a driver.
        /// </summary>
        public bool HasDriver
        {
            get
            {
                return this.Vehicle.HasDriver;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the siren is muted (so lights only).
        /// </summary>
        public bool SirenMuted
        {
            get
            {
                if (this.Vehicle.SirenManager == null)
                {
                    return false;
                }

                return this.Vehicle.SirenManager.IsSirenMuted;
            }

            set
            {
                if (this.Vehicle.SirenManager == null)
                {
                    this.Vehicle.AddSirenManager();
                }

                if (value)
                {
                    this.Vehicle.SirenManager.MuteSiren();
                }
                else
                {
                    this.Vehicle.SirenManager.UnmuteSiren();
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="CVehicle"/> representation.
        /// </summary>
        internal CVehicle CVehicle
        {
            get
            {
                return this.Vehicle;
            }
        }

        public void AttachBlip(bool sync)
        {
            this.AttachBlip(sync);
        }

        /// <summary>
        /// Deletes the blip of the vehicle, if any.
        /// </summary>
        public void DeleteBlip()
        {
            this.Vehicle.DeleteBlip();
        }

        /// <summary>
        /// Creates a new instance of <see cref="LVehicle"/> from <paramref name="vehicle"/>.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        /// <returns>Converted vehicle instance.</returns>
        public static LVehicle FromGTAVehicle(GTA.Vehicle vehicle)
        {
            CVehicle customVehicle = Pools.VehiclePool.GetVehicleFromPool(vehicle);
            return new LVehicle(customVehicle);
        }
    }
}