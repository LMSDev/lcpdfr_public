namespace LCPD_First_Response.Engine.Scripting.Scenarios
{
    using System;

    using GTA;

    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;

    /// <summary>
    /// Scenario that manages cops in a vehicle taking care of an already arrested ped and transporting it.
    /// </summary>
    internal class ScenarioSuspectTransport : Scenario, ICanOwnEntities, IPedController
    {
        /// <summary>
        /// The cops.
        /// </summary>
        private CPed[] cops;

        /// <summary>
        /// Whether the suspect has been successfully put into the vehicle.
        /// </summary>
        private bool inCustody;

        /// <summary>
        /// Timer to prevent the movement task of the suspect to be applied every tick.
        /// </summary>
        private NonAutomaticTimer movementTimer;

        /// <summary>
        /// The suspect.
        /// </summary>
        private CPed suspect;

        /// <summary>
        /// The vehicle.
        /// </summary>
        private CVehicle vehicle;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScenarioSuspectTransport"/> class.
        /// </summary>
        /// <param name="suspect">The suspect.</param>
        /// <param name="cops">The cops, have to be in a vehicle.</param>
        public ScenarioSuspectTransport(CPed suspect, CPed[] cops)
        {
            this.suspect = suspect;
            this.cops = cops;
            this.movementTimer = new NonAutomaticTimer(5000);
        }

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get
            {
                return "ScenarioSuspectTransport";
            }
        }

        /// <summary>
        /// Gets a value indicating whether the suspect is in custody.
        /// </summary>
        public bool IsSuspectInCustody
        {
            get
            {
                return this.inCustody;
            }
        }

        /// <summary>
        /// Initializes the scenario.
        /// </summary>
        public override void Initialize()
        {
            // Own suspect
            this.suspect.RequestOwnership(this);

            // Own cops
            foreach (CPed cop in this.cops)
            {
                if (cop.GetPedData<PedDataCop>().RequestPedAction(ECopState.SuspectTransporter, this))
                {
                    cop.RequestOwnership(this);

                    if (cop.IsInVehicle)
                    {
                        if (this.vehicle == null)
                        {
                            this.vehicle = cop.CurrentVehicle;
                        }

                        if (cop.IsDriver)
                        {
                            this.vehicle = cop.CurrentVehicle;

                            // Ensure siren is active
                            cop.CurrentVehicle.SirenActive = true;
                            cop.APed.TaskCombatPersueInCarSubtask(this.suspect.APed);
                        }
                    }
                }
                else
                {
                    Log.Error("Initialize: Cop not available", this);
                    throw new ArgumentException("Cop not available");
                }
            }

            if (this.vehicle == null)
            {
                Log.Warning("Initialize: Failed to find a vehicle", this);
                return;
            }

            this.vehicle.RequestOwnership(this);
        }

        /// <summary>
        /// By default frees all owned entities and deletes the base component reference
        /// </summary>
        public override void MakeAbortable()
        {
            // Only keep the SuspectTransporter states of the cop if they really have the suspect in custody
            foreach (CPed cop in this.cops)
            {
                if (!this.inCustody)
                {
                    cop.GetPedData<PedDataCop>().ResetPedAction(this);
                }
            }

            // Delete vehicle blip, if any
            if (this.vehicle != null && this.vehicle.Exists() && this.vehicle.Owner == this)
            {
                this.vehicle.DeleteBlip();
            }

            base.MakeAbortable();
        }

        /// <summary>
        /// Processes the scenario logic.
        /// </summary>
        public override void Process()
        {
            if (!this.suspect.Exists() || !this.suspect.IsAliveAndWell)
            {
                this.MakeAbortable();
                return;
            }

            if (!this.vehicle.Exists())
            {
                Log.Warning("Process: Vehicle no longer exists while still in use. Was it deleted?", this); 
                this.MakeAbortable();
                return;
            }

            // If close to ped
            foreach (CPed cop in this.cops)
            {
                if (cop.Exists() && cop.IsAliveAndWell)
                {
                    // If cop is still in vehicle but suspect is not
                    if (cop.IsInVehicle && !this.suspect.IsInVehicle)
                    {
                        // If close to suspect or vehicle is stuck, leave
                        if (cop.Position.DistanceTo(this.suspect.Position) < 10 || cop.CurrentVehicle.IsStuck(10000))
                        {
                            if (!cop.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexNewExitVehicle))
                            {
                                cop.Task.LeaveVehicle();
                            }
                        }
                    }
                    else
                    {
                        // If suspect is not in the police vehicle
                        if (!this.suspect.IsInVehicle(this.vehicle) && !this.suspect.IsGettingIntoAVehicle)
                        {
                            // Go to suspect aiming
                            if (!cop.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSeekEntityAiming))
                            {
                                cop.EnsurePedHasWeapon();
                                cop.Task.GoToCharAiming(this.suspect, 5f, 10f);
                            }

                            if (this.suspect.Position.DistanceTo(this.vehicle.Position) > 5)
                            {
                                // Only move when close to cop
                                if (this.suspect.Position.DistanceTo(cop.Position) < 7)
                                {
                                    if (this.movementTimer.CanExecute())
                                    {
                                        this.suspect.Task.RunTo(this.vehicle.Position);
                                    }
                                }
                            }
                            else
                            {
                                // If suspect is not getting into a vehicle and close to it, get into police vehicle
                                if (!this.suspect.Intelligence.TaskManager.IsTaskActive(ETaskID.GetInVehicle))
                                {
                                    TaskGetInVehicle taskGetInVehicle = new TaskGetInVehicle(this.vehicle, VehicleSeat.RightRear, VehicleSeat.LeftRear, true, true);
                                    taskGetInVehicle.AssignTo(this.suspect, ETaskPriority.MainTask);
                                }
                            }
                        }

                        // If suspect is in police vehicle, make cops enter too
                        if (this.suspect.IsSittingInVehicle(this.vehicle))
                        {
                            this.vehicle.IsSuspectTransporter = true;
                            this.suspect.Wanted.HasBeenArrested = true;
                            this.suspect.BlockPermanentEvents = true;
                            this.suspect.Task.Wait(int.MaxValue);

                            // If flag is set, remove blip
                            if (!this.suspect.DontRemoveBlipWhenBusted)
                            {
                                this.suspect.DeleteBlip();
                            }

                            this.inCustody = true;
                            this.MakeAbortable();
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Called when a ped has 'left' from the current controller, e.g. due to a more important event. Use this to clean up things.
        /// </summary>
        /// <param name="ped">
        /// The ped.
        /// </param>
        public void PedHasLeft(CPed ped)
        {

        }
    }
}
