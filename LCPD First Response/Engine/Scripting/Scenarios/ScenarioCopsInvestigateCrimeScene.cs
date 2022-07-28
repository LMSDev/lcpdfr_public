namespace LCPD_First_Response.Engine.Scripting.Scenarios
{
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Native;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;
    using TaskSequence = LCPD_First_Response.Engine.Scripting.Tasks.TaskSequence;

    /// <summary>
    /// Used for ambient cops that have been dispatched, this makes them investigate their area and then frees them. When getting in combat while this scenario is running or 
    /// when reveing criminal events, this scenario will abort and free the cops immediately to make them respond to the aggressor
    /// </summary>
    internal class ScenarioCopsInvestigateCrimeScene : Scenario, IPedController
    {
        /// <summary>
        /// The cops.
        /// </summary>
        private CPed[] cops;

        /// <summary>
        /// If true, cops will not walk around the scene, but will try to get to the position before.
        /// </summary>
        private bool investigateDeadBody;

        /// <summary>
        /// If we should use the water based variant of this scenario
        /// </summary>
        private bool waterScenario;

        /// <summary>
        /// 
        /// </summary>
        private bool waterArrived;

        /// <summary>
        /// Flag for speech when the cops exit their vehicles, used to prevent them saying over and over
        /// </summary>
        private bool leaveVehicleSpeechesDone;

        /// <summary>
        /// The position.
        /// </summary>
        private GTA.Vector3 position;

        /// <summary>
        /// How far away from the position should the unit start slowing down
        /// </summary>
        private float distanceToStopAt;

        /// <summary>
        /// If the unit has finished driving to the position
        /// </summary>
        private bool finishedDrive;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScenarioCopsInvestigateCrimeScene"/> class.
        /// </summary>
        /// <param name="cops">
        /// The cops.
        /// </param>
        /// <param name="position">
        /// The position.
        /// </param>
        /// <param name="investigateDeadBody">
        /// If true, cops will not walk around the scene, but will try to get to the position before.
        /// </param>
        public ScenarioCopsInvestigateCrimeScene(CPed[] cops, GTA.Vector3 position, bool investigateDeadBody, bool waterScenario = false)
        {
            Log.Debug("ScenarioCopsInvestigateCrimeScene: Created", this);

            this.cops = cops;
            this.investigateDeadBody = investigateDeadBody;
            this.position = position;
            this.waterScenario = waterScenario;
            this.finishedDrive = false;
            this.waterArrived = false;

            this.distanceToStopAt = Common.GetRandomValue(20, 30);

            if (this.waterScenario)
            {
                this.distanceToStopAt = 35;
            }
            
        }

        /// <summary>
        /// By default frees all owned entities and deletes the base component reference
        /// </summary>
        public override void MakeAbortable()
        {
            // Deactivate siren of cops
            foreach (CPed cop in this.cops)
            {
                if (cop.Exists())
                {
                    // If still assigned to this scenario, deactive siren and make sure ped will be freed
                    if (cop.Intelligence.IsStillAssignedToController(this))
                    {
                        if (cop.IsInVehicle)
                        {
                            cop.CurrentVehicle.SirenActive = false;
                        }

                        // Clear tasks
                        cop.Intelligence.TaskManager.ClearTasks();

                        // Reset flag, so this scenario can be used again
                        cop.PedData.AlreadyInvestigated = false;
                        cop.NoLongerNeeded();
                        cop.GetPedData<PedDataCop>().ResetPedAction(this);
                    }
                }
            }

            base.MakeAbortable();
        }

        public override void Initialize()
        {
            foreach (CPed cop in cops)
            {
                if (cop.Exists())
                {
                    PedDataCop dataCop = cop.PedData as PedDataCop;
                    if (!dataCop.RequestPedAction(ECopState.Investigating, this)) continue;
                    if (cop.IsInVehicle)
                    {
                        if (cop.IsDriver)
                        {
                            // TODO: Fix driver ai, find a better task here. What does PersueInCarUse?
                            // cop.Task.DriveTo(this.position, 20, false, true);
                            Natives.TaskCarMission(cop, cop.CurrentVehicle, this.position, 4, 30.0f, 2, 10, 10);

                            // Ensure siren is active
                            cop.CurrentVehicle.SirenActive = true;
                        }
                    }
                    else
                    {
                        // Run there
                        cop.Task.RunTo(this.position);
                    }
                }
            }
        }

        public override void Process()
        {
            int useableCops = 0;

            foreach (CPed cop in cops)
            {
                if (cop.Exists())
                {
                    useableCops++;

                    // Check if we are still allowed to access the cop
                    if (!cop.GetPedData<PedDataCop>().IsPedStillUseable(this))
                    {
                        useableCops--;
                        continue;
                    }

                    if (!cop.IsAliveAndWell)
                    {
                        useableCops--;
                        continue;
                    }

                    if (cop.IsInVehicle && !waterArrived)
                    {
                        // If vehicle is stuck, make cop run
                        if (cop.CurrentVehicle.IsStuck(10000))
                        {
                            // Run there
                            cop.Task.RunTo(this.position);
                        }

                        // If close
                        if (cop.Position.DistanceTo2D(this.position) < this.distanceToStopAt || this.finishedDrive)
                        {
                            // If stopped
                            if (cop.CurrentVehicle.IsStopped || (cop.CurrentVehicle.IsInWater && cop.CurrentVehicle.Speed < 2.5f))
                            {
                                this.waterArrived = true;

                                {
                                    if (this.investigateDeadBody)
                                    {
                                        // Get close to the position
                                        cop.Task.RunTo(this.position);
                                    }
                                    else
                                    {
                                        // Speech for leaving vehicle
                                        if (!leaveVehicleSpeechesDone)
                                        {
                                            if (!cop.IsAmbientSpeechPlaying && cop.IsGettingOutOfAVehicle)
                                            {
                                                if (cop.PedSubGroup == EPedSubGroup.Noose)
                                                {
                                                    int speechRandom = Common.GetRandomValue(0, 2);

                                                    if (speechRandom == 0)
                                                    {
                                                        cop.SayAmbientSpeech("ROPE");
                                                    }
                                                    else if (speechRandom == 1)
                                                    {
                                                        cop.SayAmbientSpeech("VAN");
                                                    }
                                                }
                                                else
                                                {
                                                    if (cop.IsDriver)
                                                    {
                                                        cop.SayAmbientSpeech("SPLIT_UP_AND_SEARCH");
                                                    }
                                                }

                                                leaveVehicleSpeechesDone = true;
                                            }
                                        }

                                        // Exit vehicle
                                        cop.Task.LeaveVehicle();
                                        cop.Intelligence.SetDrawTextAbovePedsHead("LEAVE VEHICLE");

                                        // Put rifle in NOOSE hand
                                        if (cop.PedSubGroup == EPedSubGroup.Noose)
                                        {
                                            cop.SetWeapon(GTA.Weapon.Rifle_M4);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (!finishedDrive)
                                {
                                    if (Common.GetRandomBool(0, 3, 1))
                                    {
                                        cop.Task.CarTempAction(ECarTempActionType.SlowDownHard2, 5000);
                                    }
                                    else
                                    if (!cop.Intelligence.TaskManager.IsTaskActive(ETaskID.ParkVehicle))
                                    {
                                        TaskParkVehicle tPV = new TaskParkVehicle(cop.CurrentVehicle, EVehicleParkingStyle.RightSideOfRoad);
                                        tPV.AssignTo(cop, ETaskPriority.MainTask);
                                    }
                                }
                                this.finishedDrive = true;
                            }
                        }
                    }
                    else
                    {
                        if (cop.PedData.AlreadyInvestigated)
                        {
                            if (!cop.Intelligence.TaskManager.IsTaskActive(ETaskID.Sequence) && !cop.Intelligence.TaskManager.IsTaskActive(ETaskID.Investigate))
                            {
                                // Free cop
                                cop.Task.Wait(5000);
                                cop.NoLongerNeeded();
                                cop.Task.Wait(5000);
                                cop.GetPedData<PedDataCop>().ResetPedAction(this);

                                if (cop.PedSubGroup == EPedSubGroup.Noose)
                                {
                                    cop.SetWeapon(GTA.Weapon.Rifle_M4);
                                }


                                // Reset flag, so this scenario can be used again
                                cop.PedData.AlreadyInvestigated = false;
                            }
                        }
                        else
                        {
                            // Check if there is a combat in the area
                            if (CPed.IsPedInCombatInArea(cop.Position, 50))
                            {
                                cop.GetPedData<PedDataCop>().ResetPedAction(this);
                                cop.PedData.AlreadyInvestigated = false;
                                cop.Task.RunTo(this.position);
                                Log.Debug("Process: Combat in area, aborting.", this);
                                continue;
                            }

                            if (this.investigateDeadBody)
                            {
                                // If really close
                                if (cop.Position.DistanceTo2D(this.position) < 4)
                                {
                                    TaskSequence taskSequence = new TaskSequence();
                                    TaskLookAtPosition taskLookAtPosition = new TaskLookAtPosition(this.position, 5000);
                                    TaskInvestigate taskInvestigate = new TaskInvestigate(18000);
                                    taskSequence.AddTask(taskLookAtPosition);
                                    taskSequence.AddTask(taskInvestigate);
                                    taskSequence.AssignTo(cop);

                                    // TODO: Speech
                                    cop.PedData.AlreadyInvestigated = true;
                                }
                                else
                                {
                                    if (cop.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleStandStill))
                                    {
                                        cop.Task.RunTo(this.position);
                                    }
                                }
                            }
                            else
                            {
                                if (this.waterScenario)
                                {
                                    DelayedCaller.Call(delegate { this.MakeAbortable(); }, 10000);
                                    cop.PedData.AlreadyInvestigated = true;
                                }
                                else
                                {
                                    // If close
                                    if (cop.Position.DistanceTo2D(this.position) < 20)
                                    {
                                        // TODO: Speech
                                        TaskInvestigate taskInvestigate = new TaskInvestigate(10000);
                                        taskInvestigate.AssignTo(cop, ETaskPriority.MainTask);
                                        cop.PedData.AlreadyInvestigated = true;
                                    }
                                    else
                                    {
                                        if (cop.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleStandStill))
                                        {
                                            cop.Task.RunTo(this.position);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // If useable cops is zero, quit
            if (useableCops == 0)
            {
                //Log.Info("Process: No more cops to process", this);
                this.MakeAbortable();
            }
        }

        public void PedHasLeft(CPed ped)
        {
            ped.NoLongerNeeded();
        }

        public override string ComponentName
        {
            get { return "ScenarioCopsInvestigateCrimeScene"; }
        }
    }
}
