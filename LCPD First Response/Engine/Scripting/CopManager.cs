namespace LCPD_First_Response.Engine.Scripting
{
    using System.Collections.Generic;
    using GTA;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Events;
    using LCPD_First_Response.Engine.Scripting.Native;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;

    // TODO: A LOT visual feedback, like animated bars for pending backup requests, notification when request was registered and when unit was dispatched,
    // Overview about active units
    // Blips for roadblocks

    /// <summary>
    /// The cop manager, resposible for keeping the cop task alive, managing backup, roadblocks etc.
    /// </summary>
    internal class CopManager : BaseComponent, ITickable
    {
        /// <summary>
        /// Pending cop requests.
        /// </summary>
        private List<CopRequest> copRequests;

        /// <summary>
        /// Initializes a new instance of the <see cref="CopManager"/> class.
        /// </summary>
        public CopManager()
        {
            this.Cops = new Pool<CPed>();
            this.copRequests = new List<CopRequest>();

            // Listen to cop created event
            EventCopCreated.EventRaised += new EventCopCreated.EventRaisedEventHandler(this.EventCopCreated_EventRaised);
        }

        /// <summary>
        /// Gets the number of active requests
        /// </summary>
        public int ActiveRequests { get; private set; }

        /// <summary>
        /// Gets a value indicating whether dispatching is allowed or not.
        /// </summary>
        public bool AllowDispatching { get; private set; }

        /// <summary>
        /// Gets all cops.
        /// </summary>
        public Pool<CPed> Cops { get; private set; }

        /// <summary>
        /// Gets the roadblock manager.
        /// </summary>
        public RoadblockManager RoadblockManager { get; private set; }

        private void EventCopCreated_EventRaised(EventCopCreated @event)
        {          
            // Add to local ist
            this.Cops.Add(@event.Cop);

            TaskCop taskCop = new TaskCop();
            taskCop.AssignTo(@event.Cop, ETaskPriority.Permanent);

            //Log.Debug("EventCopCreated_EventRaised: Done", this);
        }

        public void Process()
        {
            // Ensure cop task is running
            foreach (CPed cop in this.Cops.GetAll())
            {
                if (cop.Exists())
                {
                    if (!cop.Intelligence.TaskManager.IsTaskActive(ETaskID.Cop))
                    {
                        TaskCop taskCop = new TaskCop();
                        taskCop.AssignTo(cop, ETaskPriority.Permanent);
                    }
                }
            }

            for (int i = 0; i < this.copRequests.Count; i++)
            {
                CopRequest request = this.copRequests[i];
                request.Process();

                // If unit was dispatched, remove
                if (request.Dispatched)
                {
                    this.copRequests.Remove(request);
                }

                if (request.Discard)
                {
                    Log.Warning("Process: A request has been discarded and all resources were freed", this);
                    this.copRequests.Remove(request);
                }
            }
        }

        public CopRequest RequestDispatch(GTA.Vector3 position, bool siren, bool dispatchImmediately, EUnitType pedType, bool ambient, CopRequest.CopCreatedEventHandler callback, params object[] parameter)
        {
            // Dispatches an officer on foot using a random model based on pedType
            CModel pedModel = CModel.GetRandomCopModel(pedType);
            return this.RequestDispatch(position, siren, dispatchImmediately, pedModel, ambient, callback, parameter);
        }

        public CopRequest RequestDispatch(GTA.Vector3 position, bool siren, bool dispatchImmediately, CModel pedModel, bool ambient, CopRequest.CopCreatedEventHandler callback, params object[] parameter)
        {
            // Dispatches an officer on foot using the given model
            CopRequest request = new CopRequest(position, siren, dispatchImmediately, pedModel, ambient, callback, parameter);
            this.copRequests.Add(request);
            return request;
        }

        public CopRequest RequestDispatch(GTA.Vector3 position, bool siren, bool dispatchImmediately, EUnitType unitType, int numberOfCops, bool ambient, CopRequest.CopCreatedEventHandler callback, params object[] parameter)
        {
            // Dispatches a unit (vehicle + peds) based on unitType
            CModel pedModel = CModel.GetRandomCopModel(unitType);
            CModel vehicleModel = CModel.GetRandomCopCarModel(unitType);
            return this.RequestDispatch(position, siren, dispatchImmediately, vehicleModel, pedModel, numberOfCops, ambient, callback, parameter);
        }

        /// <summary>
        /// Requests a dispatch to <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="siren">Whether siren should be on.</param>
        /// <param name="dispatchImmediately">Whether there should be no delay.</param>
        /// <param name="policePedModelFlags">The ped model flags. <see cref="EModelFlags.IsCop"/> is added automatically.</param>
        /// <param name="policeVehicleModelFlags">The vehicle model flags. <see cref="EModelFlags.IsCopCar"/> is added automatically.</param>
        /// <param name="numberOfCops">The number of cops.</param>
        /// <param name="ambient">Whether cops should be ambient, that is doing nothing.</param>
        /// <param name="callback">The callback.</param>
        /// <param name="parameter">The parameter for the callback.</param>
        public CopRequest RequestDispatch(GTA.Vector3 position, bool siren, bool dispatchImmediately, EModelFlags policePedModelFlags, EModelFlags policeVehicleModelFlags, int numberOfCops, bool ambient, CopRequest.CopCreatedEventHandler callback, params object[] parameter)
        {
            // Dispatches a unit (vehicle + peds) based on unitType

            if (CModel.IsCurrentCopModelAlderneyModel)
            {
                policePedModelFlags = policePedModelFlags | EModelFlags.IsAlderneyModel;
            }
            else
            {
                policePedModelFlags = policePedModelFlags | EModelFlags.IsLibertyModel;
            }

            EModelFlags pedFlags = policePedModelFlags | EModelFlags.IsCop;
            EModelFlags vehicleFlags = policeVehicleModelFlags | EModelFlags.IsCopCar;

            CModel pedModel = CModel.GetRandomModel(pedFlags);
            CModel vehicleModel = CModel.GetRandomModel(vehicleFlags);
            return this.RequestDispatch(position, siren, dispatchImmediately, vehicleModel, pedModel, numberOfCops, ambient, callback, parameter);
        }

        public CopRequest RequestDispatch(GTA.Vector3 position, bool siren, bool dispatchImmediately, CModel vehicleModel, CModel pedModel, int numberOfCops, bool ambient, CopRequest.CopCreatedEventHandler callback, params object[] parameter)
        {
            // Dispatches a unit (vehicle + peds) using vehicleModel and pedModel
            CopRequest request = new CopRequest(position, siren, dispatchImmediately, vehicleModel, pedModel, numberOfCops, ambient, callback, parameter);
            this.copRequests.Add(request);
            return request;
        }

        /// <summary>
        /// Returns all available units (PedData.Available has to be true aswell as the ped musn't have an owner)
        /// </summary>
        /// <param name="forceAllUnits">If true, all units even if unavailable are returned</param>
        /// <returns></returns>
        public CPed[] RequestAllAvailableUnits(bool forceAllUnits)
        {
            if (forceAllUnits) return this.Cops.GetAll();

            List<CPed> availableUnits = new List<CPed>();
            foreach (CPed cop in Cops.GetAll())
            {
                if (!cop.Exists()) continue;
                if (cop.HasOwner) continue;
                if (!cop.IsAliveAndWell) continue;
                PedDataCop pedDataCop = cop.PedData as PedDataCop;
                if (pedDataCop.Available)
                {
                    availableUnits.Add(cop);
                }
            }
            return availableUnits.ToArray();
        }

        /// <summary>
        /// Returns a close unit. Returns null if no unit is around.
        /// </summary>
        public CPed RequestUnit(Vector3 position, float maxRange = float.MaxValue)
        {
            foreach (CPed cop in CPed.SortByDistanceToPosition(this.Cops.GetAll(), position))
            {
                if (!cop.Exists())
                {
                    continue;
                }

                if (!cop.IsAliveAndWell)
                {
                    continue;
                }

                if (cop.Position.DistanceTo2D(position) > maxRange)
                {
                    break;
                }

                PedDataCop pedDataCop = cop.PedData as PedDataCop;
                if (pedDataCop.Available)
                {
                    return cop;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns cops in a close police car. Returns null if no is around.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="maxRange">The maximum range.</param>
        /// <returns>Cops in closest vehicle.</returns>
        public CPed[] RequestUnitInVehicle(Vector3 position, float maxRange = float.MaxValue)
        {
            return this.RequestUnitInVehicle(position, EModelFlags.None, maxRange);
        }

        /// <summary>
        /// Returns cops in a close police car. Returns null if no is around.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="policeModelFlags">The police model flags.</param>
        /// <param name="maxRange">The maximum range.</param>
        /// <returns>Cops in closest vehicle.</returns>
        public CPed[] RequestUnitInVehicle(Vector3 position, EModelFlags policeModelFlags, float maxRange = float.MaxValue)
        {
            foreach (CPed cop in CPed.SortByDistanceToPosition(this.Cops.GetAll(), position))
            {
                if (!cop.Exists())
                {
                    continue;
                }

                if (!cop.IsAliveAndWell)
                {
                    continue;
                }

                if (!cop.IsInVehicle || !cop.IsSittingInVehicle())
                {
                    continue;
                }

                if (cop.Position.DistanceTo2D(position) > maxRange)
                {
                    break;
                }

                if (!cop.GetPedData<PedDataCop>().Available || cop.GetPedData<PedDataCop>().CopState != ECopState.Idle)
                {
                    continue;
                }

                if (!cop.CurrentVehicle.Model.ModelInfo.ModelFlags.HasFlag(policeModelFlags))
                {
                    continue;
                }

                if (cop.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexNewExitVehicle))
                {
                    continue;
                }

                // Get all passengers as well
                List<CPed> cops = new List<CPed>();
                foreach (var passenger in cop.CurrentVehicle.GetAllPedsInVehicle())
                {
                    if (passenger.Exists() && passenger.IsAliveAndWell && passenger.PedGroup == EPedGroup.Cop && passenger.GetPedData<PedDataCop>().Available)
                    {
                        cops.Add(passenger);
                    }
                }

                if (cops.Count == 0)
                {
                    Log.Warning("RequestUnitInVehicle: Found cop in vehicle, array is zero though. Passenger count: " + cop.CurrentVehicle.GetAllPedsInVehicle().Length, this);
                    return null;
                }

                if (cops.Count != cop.CurrentVehicle.GetAllPedsInVehicle().Length)
                {
                    Log.Warning("RequestUnitInVehicle: At least one ped in vehicle is not idle", this);
                    return null;
                }

                return cops.ToArray();
            }

            return null;
        }

        /// <summary>
        /// Clears all backup requests.
        /// </summary>
        public void ClearRequests()
        {
            // Clear pending requests
            this.copRequests.Clear();
        }

        public override string ComponentName
        {
            get { return "CopManager"; }
        }
    }

    enum ECopType
    {
        Cop,
        Fbi,
        Noose,
    }
}
