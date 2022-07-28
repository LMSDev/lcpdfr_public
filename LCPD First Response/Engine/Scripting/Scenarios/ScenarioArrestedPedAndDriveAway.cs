namespace LCPD_First_Response.Engine.Scripting.Scenarios
{
    using GTA;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Events;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;

    internal class ScenarioArrestedPedAndDriveAway : Scenario, ICanOwnEntities, IPedController
    {
        private const float VehicleScanRange = 50;

        private CPed arrestedPed;
        private CPed arrestingPed;
        private CPed driver;
        private bool finished;
        private bool requested;
        private bool vehicleFound;
        private CVehicle vehicle;

        public ScenarioArrestedPedAndDriveAway(CPed arrestingPed, CPed arrestedPed)
        {
            this.arrestedPed = arrestedPed;
            this.arrestingPed = arrestingPed;
        }

        public ScenarioArrestedPedAndDriveAway(CPed arrestingPed, CPed arrestedPed, CVehicle vehicleToUse)
        {
            this.arrestedPed = arrestedPed;
            this.arrestingPed = arrestingPed;
            this.vehicleFound = true;
            this.vehicle = vehicleToUse;
        }

        public override void Initialize()
        {
            if (this.arrestingPed != null && this.arrestingPed.Exists())
            {            
                // Set driver to the ped arresting only if he's available
                if (this.arrestingPed.GetPedData<PedDataCop>().IsFreeForAction(ECopState.SuspectTransporter, this))
                {
                    this.driver = this.arrestingPed;
                    this.SetupDriver();
                }
            }
        }

        public override void Process()
        {
            if (this.finished) return;

            if (!this.arrestedPed.Exists())
            {
                this.MakeAbortable();
                return;
            }

            if (!this.vehicleFound)
            {
                // Look for close police vehicles. The vehicle musn't be in use by another chase than our current. Only search if not already requested one
                if (!this.requested)
                {
                    // Check if a driver is required
                    EVehicleSearchCriteria vehicleSearchCriteria = EVehicleSearchCriteria.CopOnly | EVehicleSearchCriteria.FreeRearSeatOnly | EVehicleSearchCriteria.NoPlayersLastVehicle;
                    if (this.arrestedPed.PedData.DontAllowEmptyVehiclesAsTransporter)
                    {
                        vehicleSearchCriteria = vehicleSearchCriteria | EVehicleSearchCriteria.DriverOnly;
                    }

                    CVehicle[] vehicles = this.arrestedPed.Intelligence.GetVehiclesAround(VehicleScanRange, vehicleSearchCriteria);
                    foreach (CVehicle veh in vehicles)
                    {
                        if (veh != null && veh.Exists() && !veh.HasOwner)
                        {
                            if (this.arrestedPed.CanEnterVehicle(veh, VehicleSeat.LeftRear) || this.arrestedPed.CanEnterVehicle(veh, VehicleSeat.RightRear))
                            {
                                if (veh.HasDriver)
                                {
                                    if (!veh.Driver.GetPedData<PedDataCop>().IsFreeForAction(ECopState.SuspectTransporter,  this))
                                    {
                                        continue;
                                    }
                                }

                                Log.Debug("Process: Found close transporter", this);

                                this.vehicle = veh;
                                this.vehicleFound = true;

                                // We now own this vehicle
                                this.vehicle.RequestOwnership(this);

                                // If vehicle has driver, make him wait until ped is in vehicle
                                if (this.vehicle.HasDriver)
                                {
                                    Log.Debug("Process: Transporter has driver", this);
                                    this.driver = this.vehicle.Driver;
                                    this.SetupDriver();
                                }
                                else
                                {
                                    Log.Debug("Process: Transporter has no driver", this);
                                }

                                break;
                            }
                        }
                    }

                    if (this.vehicle != null && this.vehicle.Exists())
                    {
                    }
                    // No close vehicle, dispatch
                    else
                    {
                        Main.CopManager.RequestDispatch(this.arrestedPed.Position, true, true, EModelFlags.IsNormalUnit | EModelFlags.IsPolice, EModelFlags.IsNormalUnit | EModelFlags.IsPolice, 2, false, this.CopRequestedDispatchedCallback);
                        this.requested = true;
                    }
                }
            }
            else
            {
                if (this.arrestedPed.Intelligence.TaskManager.IsTaskActive(ETaskID.BeingBusted))
                {
                    TaskBeingBusted taskBeingBusted = this.arrestedPed.Intelligence.TaskManager.FindTaskWithID(ETaskID.BeingBusted) as TaskBeingBusted;
                    if (!taskBeingBusted.HasVehicle)
                    {
                        // DEBUG
                        //this.vehicle.AttachBlip().Friendly = true;

                        // Vehicle was found, let ped know
                        taskBeingBusted.SetVehicleToUse(this.vehicle);

                        // HACK: Don't mark vehicle as cop car, so other cops won't enter it and only our driver will
                        this.vehicle.AVehicle.SetAsPoliceVehicle(false);
                    }
                }
            }

            // If ped has been arrested, find a driver
            if (this.arrestedPed.Wanted.HasBeenArrested)
            {
                if (!this.arrestedPed.Intelligence.TaskManager.IsTaskActive(ETaskID.PlayAnimationAndRepeat))
                {
                    TaskPlayAnimationAndRepeat taskPlayAnimationAndRepeat = new TaskPlayAnimationAndRepeat("idle", "move_m@h_cuffed", 4.0f, AnimationFlags.Unknown12 | AnimationFlags.Unknown06 | AnimationFlags.Unknown11 | AnimationFlags.Unknown09);
                    taskPlayAnimationAndRepeat.AssignTo(this.arrestedPed, ETaskPriority.MainTask);
                }

                // Check if there's already a driver selected
                if (this.driver != null && this.driver.Exists() && this.driver.IsAliveAndWell)
                {
                    // Ensure driver is suspect transporter
                    if (this.driver.GetPedData<PedDataCop>().CopState != ECopState.SuspectTransporter)
                    {
                        this.SetupDriver();
                    }

                    // Check if our driver is in the vehicle
                    if (!this.driver.IsInVehicle(this.vehicle) && !this.driver.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexNewGetInVehicle))
                    {
                        this.driver.Task.EnterVehicle(this.vehicle, VehicleSeat.Driver);
                    }

                    // There is a chance another cop will get into the vehicle as driver, if so, abort
                    bool anotherDriver = this.arrestedPed.IsInVehicle() && this.arrestedPed.CurrentVehicle.HasDriver && this.arrestedPed.CurrentVehicle.Driver.IsAliveAndWell &&
                        !this.driver.IsInVehicle(this.vehicle);

                    if (this.driver.IsInVehicle(this.vehicle) || anotherDriver)
                    {
                        if (anotherDriver)
                        {
                            // If the other driver is the player, finish here
                            if (this.arrestedPed.CurrentVehicle.Driver.PedGroup == EPedGroup.Player)
                            {
                                this.driver = this.arrestedPed.CurrentVehicle.Driver;
                                Log.Debug("Process: Already has player as driver", this);

                                // Has driver, finish scenario
                                this.finished = true;
                                DelayedCaller.Call(this.TaskFinishedCallback, 4000);
                                DelayedCaller.Call(
                                    delegate { new EventArrestedPedSittingInPlayerVehicle(this.arrestedPed); }, 4500);
                                return;
                            }
                            else
                            {
                                CPed tempDriver = this.arrestedPed.CurrentVehicle.Driver;
                                if (tempDriver.PedGroup != EPedGroup.Cop || tempDriver.HasOwner
                                    || !tempDriver.GetPedData<PedDataCop>().IsFreeForAction(ECopState.SuspectTransporter, this))
                                {
                                    Log.Warning("Found bad driver", this);
                                    if (!tempDriver.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexNewExitVehicle))
                                    {
                                        tempDriver.Task.LeaveVehicle();
                                    }
                                    return;
                                }
                                else
                                {
                                    this.SetupDriver();
                                }
                            }
                        }

                        this.driver.CurrentVehicle.SirenActive = false;
                        this.driver.Task.AlwaysKeepTask = true;

                        // If not set, cop would leave for fighting criminals. While this might be wanted behavior, it becomes annoying as soon as cop
                        // starts shooting at peds you want to arrest because the chase class can't own him due to his Transporter state and thus
                        // is not affected by the chase AI logic
                        this.driver.BlockPermanentEvents = true;
                        this.driver.Task.CruiseWithVehicle(this.vehicle, 10f, true);

                        // Has driver, finish scenario
                        this.finished = true;
                        DelayedCaller.Call(this.TaskFinishedCallback, 4000);
                    }
                    return;
                }

                // Check if the vehicle already has a suitable driver
                if (this.arrestedPed.IsInVehicle && this.arrestedPed.CurrentVehicle.HasDriver && this.arrestedPed.CurrentVehicle.Driver.IsAliveAndWell)
                {
                    CPed tempDriver = this.arrestedPed.CurrentVehicle.Driver;
                    if (tempDriver.PedGroup == EPedGroup.Cop)
                    {
                        // Musnt't have an owner or must be owned by this script
                        if (!tempDriver.HasOwner || tempDriver.Owner == this)
                        {
                            // This action is never reset because a suspect transporter cop musn't leave his car!
                            if (tempDriver.GetPedData<PedDataCop>().RequestPedAction(ECopState.SuspectTransporter, this))
                            {
                                this.driver = tempDriver;
                                this.SetupDriver();
                                return;
                            }
                        }
                    }
                    // Driver didn't meet the requirements, make him exit
                    if (!tempDriver.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexNewExitVehicle))
                    {
                        tempDriver.Task.LeaveVehicle();
                    }
                }
                else
                {
                    // Get a new driver. The cop must be free. If there's no free cop, the suspect will wait in the vehicle until there is
                    CPed[] cops = arrestedPed.Intelligence.GetPedsAround(50, EPedSearchCriteria.CopsOnly);
                    foreach (CPed cPed in cops)
                    {
                        if (cPed != null & cPed.Exists())
                        {
                            if (cPed.IsAliveAndWell)
                            {
                                // This action is never reset because a suspect transporter cop musn't leave his car!
                                if (cPed.GetPedData<PedDataCop>().RequestPedAction(ECopState.SuspectTransporter, this))
                                {
                                    this.driver = cPed;
                                    this.SetupDriver();
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void CopRequestedDispatchedCallback(CopRequest request)
        {
            // Just a test
            if (request.Vehicle != null && request.Vehicle.Exists())
            {
                this.vehicleFound = true;
                this.vehicle = request.Vehicle;

                // We now own this vehicle
                vehicle.RequestOwnership(this);

                // If vehicle has driver, make him wait until ped is in vehicle
                if (this.vehicle.HasDriver)
                {
                    this.driver = this.vehicle.Driver;
                    this.SetupDriver();

                    // Drive towards ped, but carefully
                    this.driver.Task.DriveTo(this.arrestedPed, 10f, false, true);

                    //var taskWaitUntilPedIsInVehicle = new TaskWaitUntilPedIsInVehicle(this.arrestedPed, this.vehicle);
                    //taskWaitUntilPedIsInVehicle.AssignTo(this.driver, ETaskPriority.MainTask);
                }
                // Also own every passenger cop, because we request them and thus they are marked as required for mission, but they're never cleaned up
                foreach (CPed cop in vehicle.GetAllPedsInVehicle())
                {
                    if (cop != this.driver)
                    {
                        cop.GetPedData<PedDataCop>().RequestPedAction(ECopState.SuspectTransporter, this);
                        cop.RequestOwnership(this);

                        // To prevent the cop from leaving the vehicle
                        cop.Task.AlwaysKeepTask = true;
                        cop.Task.Wait(int.MaxValue);
                    }
                }
            }
        }

        /// <summary>
        /// Setups the driver ped.
        /// </summary>
        private void SetupDriver()
        {
            // Ensure state is set
            if (this.driver.GetPedData<PedDataCop>().CopState != ECopState.SuspectTransporter)
            {
                this.driver.GetPedData<PedDataCop>().RequestPedAction(ECopState.SuspectTransporter, this);
            }

            Log.Debug("Driver State: " + this.driver.GetPedData<PedDataCop>().CopState, this);

            // Own entity
            this.driver.RequestOwnership(this);
            this.driver.Task.ClearAll();

            // If driver is in a vehicle already, wait for ped
            if (this.driver.IsInVehicle && !this.requested)
            {
                var taskWaitUntilPedIsInVehicle = new TaskWaitUntilPedIsInVehicle(this.arrestedPed, this.vehicle);
                taskWaitUntilPedIsInVehicle.AssignTo(this.driver, ETaskPriority.MainTask);
            }
        }

        /// <summary>
        /// Called a minute after the scenario has finished
        /// </summary>
        private void TaskFinishedCallback(object data)
        {
            if (!this.driver.Exists())
            {
                Log.Warning("TaskFinishedCallback: Driver disposed for unknown reason", this);
            }
            else
            {
                if (this.driver.IsInVehicle)
                {
                    this.driver.CurrentVehicle.SirenActive = false;
                }
            }

            this.MakeAbortable();
        }

        public void PedHasLeft(CPed ped)
        {
            if (ped != null && ped.Exists())
            {
                ped.NoLongerNeeded();
            }
        }

        public override string ComponentName
        {
            get { return "ScenarioArrestedPedAndDriveAway"; }
        }
    }
}
