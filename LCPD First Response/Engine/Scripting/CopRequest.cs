namespace LCPD_First_Response.Engine.Scripting
{
    using System.Linq;
    using GTA;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Scenarios;
    using LCPD_First_Response.Engine.Scripting.Tasks;

    using Timer = LCPD_First_Response.Engine.Timers.NonAutomaticTimer;

    class CopRequest : BaseComponent
    {
        public delegate void CopCreatedEventHandler(CopRequest request);

        public delegate Vector3 OnBeforeCopCarCreatedEventHandler(CopRequest copRequest);

        public event OnBeforeCopCarCreatedEventHandler OnBeforeCopCarCreated;

        public CPed[] Cops { get; private set; }
        public bool Dispatched { get; private set; }

        /// <summary>
        /// Gets or sets whether position finding for boats should only use static data.
        /// </summary>
        public bool ForceStaticPositionFindingForBoats { get; set; }

        public CVehicle Vehicle { get; private set; }

        private bool siren;
        private CopCreatedEventHandler callback;
        private bool dispatchImmediately;
        private int numberOfCops;
        private CModel pedModel;
        private bool ambient;
        private GTA.Vector3 position;
        private Timer dispatchTimer;
        private CModel vehicleModel;
        private ERequestType requestType;

        /// <summary>
        /// The timer used to measure when the creation took too long.
        /// </summary>
        private Timers.Timer timeoutTimer;

        /// <summary>
        /// Creates a cop on foot near the position
        /// </summary>
        /// <param name="position"></param>
        /// <param name="siren"></param>
        /// <param name="dispatchImmediately"></param>
        /// <param name="pedModel"></param>
        /// <param name="ambient">This will start ScenarioCopsInvestigateCrimeScene and thus shouldn't be used if you want to control the unit</param>
        /// <param name="callback"></param>
        /// <param name="callbackParameter"></param>
        public CopRequest(GTA.Vector3 position, bool siren, bool dispatchImmediately, CModel pedModel, bool ambient, CopCreatedEventHandler callback, params object[] callbackParameter)
        {
            this.position = position;
            this.siren = siren;
            this.dispatchImmediately = dispatchImmediately;
            this.pedModel = pedModel;
            this.ambient = ambient;
            this.callback = callback;
            this.dispatchTimer = new Timer(2500);
            this.CallbackParameter = callbackParameter;

            this.requestType = ERequestType.OnFoot;
        }

        /// <summary>
        /// Creates a given number of cops in a vehicle near the position
        /// </summary>
        /// <param name="position"></param>
        /// <param name="siren"></param>
        /// <param name="dispatchImmediately"></param>
        /// <param name="vehicleModel"></param>
        /// <param name="pedModel"></param>
        /// <param name="numberOfCops"></param>
        /// <param name="ambient">This will start ScenarioCopsInvestigateCrimeScene and thus shouldn't be used if you want to control the unit</param>
        /// <param name="callback"></param>
        /// <param name="callbackParameter"></param>
        public CopRequest(GTA.Vector3 position, bool siren, bool dispatchImmediately, CModel vehicleModel, CModel pedModel, int numberOfCops, bool ambient, CopCreatedEventHandler callback, params object[] callbackParameter)
        {
            this.dispatchImmediately = dispatchImmediately;
            this.pedModel = pedModel;
            this.numberOfCops = numberOfCops;
            this.callback = callback;
            this.position = position;
            this.siren = siren;
            this.ambient = ambient;
            this.vehicleModel = vehicleModel;
            this.dispatchTimer = new Timer(2500);
            this.CallbackParameter = callbackParameter;

            this.requestType = ERequestType.CopsInVehicle;
        }

        /// <summary>
        /// Gets the callback parameter.
        /// </summary>
        public object[] CallbackParameter { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the request should be discarded, e.g. due to an error.
        /// </summary>
        public bool Discard { get; private set; }

        public void Process()
        {
            if (this.dispatchTimer.CanExecute(true) || this.dispatchImmediately)
            {
                if (this.timeoutTimer == null)
                {
                    this.timeoutTimer = new Timers.Timer(5000, this.DispatchTimedOut);
                    this.timeoutTimer.Start();
                }

                // If time is exceeded
                this.Dispatched = this.Dispatch();

                if (this.Dispatched)
                {
                    if (this.callback != null)
                    {
                        this.callback.Invoke(this);
                    }
                }
            }
        }

        private bool Dispatch()
        {
            // This will dispatch a police unit
            if (this.requestType == ERequestType.CopsInVehicle)
            {   
                bool car = DispatchCar();
                if (!car)
                {
                    FailedCreationCleanup();
                    return false;
                }
                this.Cops = new CPed[this.numberOfCops];

                for (int i = 0; i < this.numberOfCops; i++)
                {
                    bool officer = DispatchOfficer();
                    if (!officer)
                    {
                        FailedCreationCleanup();
                        return false;
                    }
                }

                // If ambient, start scenario
                if (this.ambient)
                {
                    var scenario = new ScenarioCopsInvestigateCrimeScene(this.Cops.ToArray(), this.position, false, this.Vehicle.Model.IsBoat);
                    var taskScenario = new TaskScenario(scenario);
                    //taskScenario.AssignTo();
                }

                this.Vehicle.SirenActive = this.siren;
            }

            return true;
        }

        private bool DispatchCar()
        {
            // If model is heli
            if (this.vehicleModel.ModelInfo != null)
            {
                if (this.vehicleModel.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsHelicopter))
                {
                    // Helicopters are manually created far away from the position
                    int yManipulation = 0;
                    if (Common.GetRandomBool(0, 2, 1))
                    {
                        yManipulation = Common.GetRandomValue(100, 200);
                    }
                    else
                    {
                        yManipulation = Common.GetRandomValue(-200, -100);
                    }

                    // Fire event to change position (useful when in chase and position should be adjusted)
                    if (this.OnBeforeCopCarCreated != null)
                    {
                        Vector3 newPos = this.OnBeforeCopCarCreated(this);
                        if (newPos != Vector3.Zero)
                        {
                            this.position = newPos;
                        }
                    }

                    this.Vehicle = new CVehicle(this.vehicleModel, new GTA.Vector3(this.position.X, this.position.Y + yManipulation, this.position.Z + 200).Around(100.0f), EVehicleGroup.Police);
                    if (this.Vehicle.Exists())
                    {
                        this.Vehicle.SetHeliBladesFullSpeed();
                        this.Vehicle.EngineRunning = true;
                        return true;
                    }
                    return false;
                }

                // Boats are spawned in water (no shit!).
                if (this.vehicleModel.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsCopBoat))
                {
                    // Fire event to change position (useful when in chase and position should be adjusted)
                    if (this.OnBeforeCopCarCreated != null)
                    {
                        Vector3 newPos = this.OnBeforeCopCarCreated(this);
                        if (newPos != Vector3.Zero)
                        {
                            this.position = newPos;
                        }
                    }

                    this.Vehicle = CVehicle.CreateBoatAroundPosition(this.vehicleModel, EVehicleGroup.Police, this.position, 160f, CPlayer.LocalPlayer.Ped.Position, this.ForceStaticPositionFindingForBoats);
                    if (this.Vehicle != null && this.Vehicle.Exists())
                    {
                        return true;
                    }
                    return false;
                }
            }
            else
            {
                // Log error and return true to avoid this from being called again and spamming the log
                Log.Warning("DispatchCar: Model has no ModelInfo reference. Missing ModelID.", this);
                return true;
            }

            CModelInfo modelInfo = this.vehicleModel.ModelInfo;
            // Request model
            this.vehicleModel.LoadIntoMemory(false);

            // Create model using model id
            Vector3 furtherAwayPosition = this.position.Around(150.0f);

            // Fire event to change position (useful when in chase or waypoint set and position should be adjusted)
            if (this.OnBeforeCopCarCreated != null)
            {
                Vector3 newPos = this.OnBeforeCopCarCreated(this);              
                if (newPos != Vector3.Zero)
                {
                    if (Game.GetWaypoint() != null && Game.GetWaypoint().Exists())
                    {
                        furtherAwayPosition = newPos;
                    }
                    else
                    {
                        this.position = newPos;
                        furtherAwayPosition = this.position;
                    }
                }
            }

            AdvancedHookManaged.AVehicle aVehicle = AdvancedHookManaged.AVehicle.CreateCarAroundPosition(furtherAwayPosition.X, furtherAwayPosition.Y, furtherAwayPosition.Z, modelInfo.ModelID, 0);
            if (aVehicle == null || aVehicle.Get() == 0)
            {
                return false;
            }
            this.Vehicle = new CVehicle((int)aVehicle.Get());
            if (this.Vehicle.Exists())
            {
                this.Vehicle.AllowSirenWithoutDriver = true;
                return true;
            }

            return false;
        }

        private bool DispatchOfficer()
        {
            // Create cops in vehicle
            if (this.requestType == ERequestType.CopsInVehicle)
            {
                CPed cop = new CPed(this.pedModel, this.position, EPedGroup.Cop);
                if (cop.Exists() && this.Vehicle.Exists())
                {
                    // Ensure there is a free seat
                    if (this.Vehicle.GetFreeSeat() != VehicleSeat.None)
                    {
                        cop.FixCopClothing();
                        cop.WarpIntoVehicle(this.Vehicle, this.Vehicle.GetFreeSeat());

                        // Don't render cop in boats yet.
                        if (this.vehicleModel.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsCopBoat))
                        {
                            cop.Visible = false;
                        }

                        // Insert into array
                        for (int i = 0; i < this.Cops.Length; i++)
                        {
                            if (this.Cops[i] == null)
                            {
                                this.Cops[i] = cop;
                                break;
                            }
                        }
                        return true;
                    }
                    else
                    {
                        Log.Warning("DispatchOfficer: No free seat in vehicle. Did you spawn too many cops per vehicle?", this);
                        int seats = this.Vehicle.PassengerSeats;
                        int validCops = 0;
                        for (int i = 0; i < this.Cops.Length; i++)
                        {
                            if (this.Cops[i] != null && this.Cops[i].Exists())
                            {
                                validCops++;
                            }
                        }

                        Log.Warning("DispatchOfficer: Error while spawning. Number of passenger seats: " + seats + " Number of cops: " + this.numberOfCops + " Created cops: " + validCops + " Vehicle model: " + this.vehicleModel.ModelInfo.Name + " Vehicle state: " + this.Vehicle.Exists(), this);
                        cop.Delete();
                        this.Discard = true;
                        return false;
                    }
                }
            }
            return false;
        }

        private void FailedCreationCleanup()
        {
            // Delete vehicle and cops
            if (this.Vehicle != null && this.Vehicle.Exists())
            {
                this.Vehicle.Delete();
            }

            if (this.Cops != null)
            {
                foreach (CPed cPed in Cops)
                {
                    if (cPed != null && cPed.Exists())
                    {
                        cPed.Delete();
                    }
                }
            }
        }

        /// <summary>
        /// Called when the dispatch timed out.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        private void DispatchTimedOut(object[] parameter)
        {
            if (this.vehicleModel.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsCopBoat))
            {
                this.Discard = true;
                this.timeoutTimer.Stop();
                return;
            }

            // We change the position to the closest loaded road
            Vector3 closestNodePosition = Vector3.Zero;
            float closestNodeHeading = 0f;
            if (CVehicle.GetClosestCarNodeWithHeading(this.position, ref closestNodePosition, ref closestNodeHeading))
            {
                Log.Debug("DispatchTimedOut: Using closest loaded road", this);
                this.position = closestNodePosition;
                this.timeoutTimer.Stop();
            }
        }

        public override string ComponentName
        {
            get { return "CopRequest"; }
        }
    }

    enum ERequestType
    {
        CopsInVehicle,
        OnFoot,
    }

    public enum EUnitType
    {
        Helicopter,
        Noose,
        Police,
        Boat,
        NooseBoat,
        Unknown,
    }
}
